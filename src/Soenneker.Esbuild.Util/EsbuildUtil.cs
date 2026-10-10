using Soenneker.Esbuild.Util.Dtos;
using Soenneker.Utils.Json;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Esbuild.Util.Abstract;
using Soenneker.Asyncs.Semaphores;
using Soenneker.Node.Util.Abstract;
using Soenneker.Utils.Process.Abstract;
using Soenneker.Utils.HttpClientCache.Abstract;

namespace Soenneker.Esbuild.Util;

public sealed class EsbuildUtil : IEsbuildUtil
{
    private readonly INodeUtil _nodeUtil;
    private readonly IProcessUtil _processUtil;
    private readonly IHttpClientCache _httpClientCache;
    private readonly string _directory;
    private readonly string? _minimumNodeVersion;
    private string? _node;
    private readonly AsyncSemaphore _installation = new(1, 1);

    public EsbuildUtil(INodeUtil nodeUtil, IProcessUtil processUtil, IHttpClientCache httpClientCache, EsbuildInstallOptions? options = null)
    {
        _nodeUtil = nodeUtil;
        _processUtil = processUtil;
        _httpClientCache = httpClientCache;
        options ??= new EsbuildInstallOptions();
        _minimumNodeVersion = options.MinimumNodeVersion;
        _directory = Path.GetFullPath(options.Directory ??
                                      Path.Combine(
                                          Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                          "Soenneker", "Esbuild"));
    }

    public async ValueTask<string> EnsureInstalled(CancellationToken cancellationToken = default)
    {
        using (SemaphoreLease lease = await _installation.Acquire(cancellationToken).NoSync())
        {
            // Resolve on every operation so a singleton does not remain on an outdated release.
            HttpClient registry = await _httpClientCache.Get("Soenneker.Esbuild.Util.Registry", cancellationToken).NoSync();
            string metadata = await registry
                                    .GetStringAsync("https://registry.npmjs.org/esbuild/latest", cancellationToken)
                                    .NoSync();
            string version = JsonUtil.Deserialize(metadata, EsbuildJsonContext.Default.JsonNode)?["version"]?.GetValue<string>() ??
                             throw new InvalidOperationException("The npm registry did not return an esbuild version.");
            if (!Regex.IsMatch(version, @"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$", RegexOptions.CultureInvariant))
                throw new InvalidOperationException("The npm registry returned an invalid esbuild version.");
            // Separate releases keep existing contexts intact while new operations use the latest release.
            string directory = Path.Combine(_directory, version);
            _node = await _nodeUtil.EnsureInstalled(_minimumNodeVersion, cancellationToken: cancellationToken)
                                   .NoSync();
            Directory.CreateDirectory(directory);
            // Coordinate installation across scopes and processes sharing the same cache.
            await using FileStream installationLock =
                await AcquireLock(directory, cancellationToken).NoSync();
            string manifest = Path.Combine(directory, "package.json");
            if (File.Exists(manifest))
            {
                JsonNode? existing =
                    JsonUtil.Deserialize(await File.ReadAllTextAsync(manifest, cancellationToken).NoSync(), EsbuildJsonContext.Default.JsonNode);
                if (existing?["name"]?.GetValue<string>() != "soenneker-esbuild-runtime" ||
                    existing?["dependencies"]?["esbuild"]?.GetValue<string>() != version)
                    throw new InvalidOperationException(
                        "The installation directory belongs to another project or esbuild version. Use a dedicated empty directory.");
            }
            else
            {
                var package = new JsonObject
                {
                    ["name"] = "soenneker-esbuild-runtime", ["private"] = true,
                    ["dependencies"] = new JsonObject { ["esbuild"] = version }
                };
                await File.WriteAllTextAsync(manifest, JsonUtil.Serialize(package, EsbuildJsonContext.Default.JsonObject), cancellationToken).NoSync();
            }

            string module = Path.Combine(directory, "node_modules", "esbuild");
            if (!File.Exists(Path.Combine(module, "bin", "esbuild")))
                await _nodeUtil.NpmInstall(directory, skipIfUpToDate: false, cancellationToken: cancellationToken)
                               .NoSync();
            try
            {
                await VerifyInstallation(module, version, directory, cancellationToken).NoSync();
            }
            catch (Exception ex) when (ex is InvalidOperationException or EsbuildException)
            {
                // npm ci repairs incomplete or corrupt platform binaries in this dedicated runtime directory.
                await _nodeUtil.NpmInstall(directory,
                    cleanInstall: File.Exists(Path.Combine(directory, "package-lock.json")), skipIfUpToDate: false,
                    cancellationToken: cancellationToken).NoSync();
                await VerifyInstallation(module, version, directory, cancellationToken).NoSync();
            }

            return Path.Combine(module, "bin", "esbuild");
        }
    }

    private async ValueTask VerifyInstallation(string module, string version, string directory,
        CancellationToken cancellationToken)
    {
        await using EsbuildSession session = await Launch(Path.Combine(module, "bin", "esbuild"), ["--version"],
            directory, null, false, null, cancellationToken).NoSync();
        EsbuildCommandResult result = await session.WaitForExit(cancellationToken).NoSync();
        ThrowIfFailed(result);
        if (result.StandardOutput.Trim() != version)
            throw new InvalidOperationException($"Expected esbuild {version}, found {result.StandardOutput.Trim()}.");
    }

    private async ValueTask<FileStream> AcquireLock(string directory, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(Path.Combine(directory, ".install.lock"), FileMode.OpenOrCreate,
                    FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException ex) when ((ex.HResult & 0xFFFF) is 11 or 32 or 33)
            {
                await Task.Delay(100, cancellationToken).NoSync();
            }
        }
    }

    private async ValueTask<EsbuildSession> Launch(string cli, IReadOnlyList<string> arguments, string? directory,
        string? input, bool keepInputOpen, IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken)
    {
        string executable = cli;
        // npm uses a Node CLI launcher on Windows and may replace it with the native binary on Unix.
        using (FileStream file = File.OpenRead(cli))
        {
            if (file.ReadByte() == '#' && file.ReadByte() == '!')
            {
                executable = _node!;
                arguments = new[] { cli }.Concat(arguments).ToArray();
            }
        }

        return await EsbuildSession.Start(_processUtil, executable, arguments, directory, input, keepInputOpen,
            environment, cancellationToken).NoSync();
    }

    private async ValueTask<EsbuildSession> StartCommand(IReadOnlyList<string> arguments, string? directory,
        string? input, bool keepInputOpen, IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        string cli = await EnsureInstalled(cancellationToken).NoSync();
        return await Launch(cli, arguments, directory, input, keepInputOpen, environment, cancellationToken)
            .NoSync();
    }

    private static void ThrowIfFailed(EsbuildCommandResult result)
    {
        if (result.ExitCode != 0)
            throw new EsbuildException(result);
    }

    public async ValueTask<EsbuildCommandResult> Run(IReadOnlyList<string> arguments, string? workingDirectory = null,
        string? input = null, CancellationToken cancellationToken = default)
    {
        await using EsbuildSession session =
            await StartCommand(arguments, workingDirectory, input, false, null, cancellationToken)
                .NoSync();
        EsbuildCommandResult result = await session.WaitForExit(cancellationToken).NoSync();
        ThrowIfFailed(result);
        return result;
    }

    public async ValueTask<IEsbuildSession> Start(IReadOnlyList<string> arguments, string? workingDirectory = null,
        string? input = null, CancellationToken cancellationToken = default) =>
        await StartCommand(arguments, workingDirectory, input, input == null, null, cancellationToken)
            .NoSync();

    public async ValueTask<string> GetVersion(CancellationToken cancellationToken = default) =>
        (await Run(["--version"], cancellationToken: cancellationToken).NoSync()).StandardOutput.Trim();

    private async ValueTask<EsbuildSession> StartBuild(EsbuildBuildOptions options, List<string> arguments,
        bool keepInputOpen, CancellationToken cancellationToken)
    {
        Dictionary<string, string>? environment = options.NodePaths == null
            ? null
            : new Dictionary<string, string>
            {
                ["NODE_PATH"] = string.Join(Path.PathSeparator, options.NodePaths)
            };
        string? directory = options.Stdin?.ResolveDir ?? options.AbsWorkingDir;
        if (directory != null)
            directory = Path.GetFullPath(directory);
        return await StartCommand(arguments, directory, options.Stdin?.Contents, keepInputOpen, environment,
            cancellationToken).NoSync();
    }

    private async ValueTask<EsbuildCommandResult> RunBuild(EsbuildBuildOptions options, List<string> arguments,
        CancellationToken cancellationToken)
    {
        await using EsbuildSession session =
            await StartBuild(options, arguments, false, cancellationToken).NoSync();
        EsbuildCommandResult result = await session.WaitForExit(cancellationToken).NoSync();
        ThrowIfFailed(result);
        return result;
    }

    public ValueTask<EsbuildCommandResult> Build(EsbuildBuildOptions options,
        CancellationToken cancellationToken = default) =>
        RunBuild(options, EsbuildArguments.Build(options), cancellationToken);

    public ValueTask<EsbuildCommandResult> Transform(string input, EsbuildTransformOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Run(EsbuildArguments.Transform(options ?? new EsbuildTransformOptions()), input: input, cancellationToken: cancellationToken);
    }

    public ValueTask<EsbuildCommandResult> Analyze(EsbuildBuildOptions options, EsbuildAnalyzeOptions? analysis = null,
        CancellationToken cancellationToken = default)
    {
        List<string> arguments = EsbuildArguments.Build(options);
        arguments.Add(analysis?.Verbose == true ? "--analyze=verbose" : "--analyze");
        if (analysis?.Color is { } color)
            arguments.Add(color ? "--color=true" : "--color=false");
        return RunBuild(options, arguments, cancellationToken);
    }

    public async ValueTask<IEsbuildSession> Watch(EsbuildBuildOptions options, EsbuildWatchOptions? watch = null,
        CancellationToken cancellationToken = default)
    {
        List<string> arguments = EsbuildArguments.Build(options);
        if (options.Stdin != null)
            throw new ArgumentException("Watch requires file entry points.", nameof(options));
        arguments.Add("--watch=forever");
        if (watch?.Delay is { } delay)
            arguments.Add($"--watch-delay={delay}");
        return await StartBuild(options, arguments, true, cancellationToken).NoSync();
    }

    public async ValueTask<IEsbuildSession> Serve(EsbuildBuildOptions options, EsbuildServeOptions? server = null,
        bool watch = false, CancellationToken cancellationToken = default)
    {
        List<string> arguments = EsbuildArguments.Build(options);
        if (options.Stdin != null)
            throw new ArgumentException("Serve requires file entry points.", nameof(options));
        server ??= new EsbuildServeOptions();
        if (server.Port is < 0 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(server.Port));
        string host = server.Host ?? "127.0.0.1";
        if (host.Contains(':') && !host.StartsWith('['))
            host = $"[{host}]";
        arguments.Add($"--serve={host}:{server.Port ?? 0}");
        if (watch)
            arguments.Add("--watch=forever");
        EsbuildArguments.Add(arguments, "servedir", server.Servedir);
        EsbuildArguments.Add(arguments, "serve-fallback", server.Fallback);
        EsbuildArguments.Add(arguments, "keyfile", server.Keyfile);
        EsbuildArguments.Add(arguments, "certfile", server.Certfile);
        if (server.CorsOrigin != null)
            arguments.Add($"--cors-origin={string.Join(',', server.CorsOrigin)}");
        return await StartBuild(options, arguments, true, cancellationToken).NoSync();
    }
}
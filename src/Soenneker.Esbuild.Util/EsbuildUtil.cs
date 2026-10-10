using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Esbuild.Util.Abstract;
using Soenneker.Node.Util.Abstract;

namespace Soenneker.Esbuild.Util;

public sealed class EsbuildUtil : IEsbuildUtil
{
    private readonly INodeUtil _nodeUtil;
    private readonly string _version;
    private readonly string _directory;
    private readonly string? _minimumNodeVersion;
    private string? _node;
    private string? _module;
    private readonly SemaphoreSlim _installation = new(1, 1);

    public EsbuildUtil(INodeUtil nodeUtil, EsbuildInstallOptions? options = null)
    {
        _nodeUtil = nodeUtil;
        options ??= new();
        if (!Regex.IsMatch(options.Version, @"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Specify an exact esbuild semantic version.", nameof(options));
        _version = options.Version;
        _minimumNodeVersion = options.MinimumNodeVersion;
        _directory = Path.GetFullPath(options.Directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Soenneker", "Esbuild", _version));
    }

    public async ValueTask<string> EnsureInstalled(CancellationToken cancellationToken = default)
    {
        await _installation.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_module != null && File.Exists(Path.Combine(_module, "lib", "main.js"))) return _module;
            _node = await _nodeUtil.EnsureInstalled(_minimumNodeVersion, cancellationToken: cancellationToken).ConfigureAwait(false);
            Directory.CreateDirectory(_directory);
            // Coordinate installation across scopes and processes sharing the same cache.
            await using FileStream installationLock = await AcquireLock(cancellationToken).ConfigureAwait(false);
            string manifest = Path.Combine(_directory, "package.json");
            if (File.Exists(manifest))
            {
                JsonNode? existing = JsonNode.Parse(await File.ReadAllTextAsync(manifest, cancellationToken).ConfigureAwait(false));
                if (existing?["name"]?.GetValue<string>() != "soenneker-esbuild-runtime" || existing?["dependencies"]?["esbuild"]?.GetValue<string>() != _version)
                    throw new InvalidOperationException("The installation directory belongs to another project or esbuild version. Use a dedicated empty directory.");
            }
            else
            {
                var package = new JsonObject { ["name"] = "soenneker-esbuild-runtime", ["private"] = true,
                    ["dependencies"] = new JsonObject { ["esbuild"] = _version } };
                await File.WriteAllTextAsync(manifest, package.ToJsonString(), cancellationToken).ConfigureAwait(false);
            }
            string module = Path.Combine(_directory, "node_modules", "esbuild");
            if (!File.Exists(Path.Combine(module, "lib", "main.js")))
                await _nodeUtil.NpmInstall(_directory, skipIfUpToDate: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            try { await VerifyInstallation(module, cancellationToken).ConfigureAwait(false); }
            catch (Exception ex) when (ex is InvalidOperationException or EsbuildException)
            {
                // npm ci repairs incomplete or corrupt platform binaries in this dedicated runtime directory.
                await _nodeUtil.NpmInstall(_directory, cleanInstall: File.Exists(Path.Combine(_directory, "package-lock.json")),
                    skipIfUpToDate: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                await VerifyInstallation(module, cancellationToken).ConfigureAwait(false);
            }
            _module = module;
            return module;
        }
        finally { _installation.Release(); }
    }

    private async ValueTask VerifyInstallation(string module, CancellationToken cancellationToken)
    {
        await using var probe = new EsbuildProcess(_node!, module, _directory);
        string actualVersion = (await probe.Invoke("version", null, cancellationToken).ConfigureAwait(false))!.GetValue<string>();
        if (actualVersion != _version) throw new InvalidOperationException($"Expected esbuild {_version}, found {actualVersion}.");
        // Loading the JS module alone does not verify the platform binary.
        await probe.Invoke("transform", new JsonObject { ["input"] = "", ["options"] = new JsonObject() }, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<FileStream> AcquireLock(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { return new FileStream(Path.Combine(_directory, ".install.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException ex) when ((ex.HResult & 0xFFFF) is 11 or 32 or 33)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async ValueTask<EsbuildProcess> Start(string? directory, CancellationToken cancellationToken)
    {
        string module = await EnsureInstalled(cancellationToken).ConfigureAwait(false);
        return new EsbuildProcess(_node!, module, Path.GetFullPath(directory ?? Environment.CurrentDirectory));
    }

    public async ValueTask<string> GetVersion(CancellationToken cancellationToken = default)
    {
        await using EsbuildProcess process = await Start(null, cancellationToken).ConfigureAwait(false);
        return (await process.Invoke("version", null, cancellationToken).ConfigureAwait(false))!.GetValue<string>();
    }

    public async ValueTask<EsbuildBuildResult> Build(EsbuildBuildOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        await using EsbuildProcess process = await Start(options.AbsWorkingDir, cancellationToken).ConfigureAwait(false);
        return (await process.Invoke("build", JsonSerializer.SerializeToNode(options, EsbuildJsonContext.Default.EsbuildBuildOptions), cancellationToken).ConfigureAwait(false))
            .Deserialize(EsbuildJsonContext.Default.EsbuildBuildResult)!;
    }

    public async ValueTask<EsbuildTransformResult> Transform(string input, EsbuildTransformOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await using EsbuildProcess process = await Start(null, cancellationToken).ConfigureAwait(false);
        return (await process.Invoke("transform", new JsonObject { ["input"] = input,
            ["options"] = JsonSerializer.SerializeToNode(options ?? new(), EsbuildJsonContext.Default.EsbuildTransformOptions) }, cancellationToken).ConfigureAwait(false))
            .Deserialize(EsbuildJsonContext.Default.EsbuildTransformResult)!;
    }

    public async ValueTask<IEsbuildContext> CreateContext(EsbuildBuildOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        EsbuildProcess process = await Start(options.AbsWorkingDir, cancellationToken).ConfigureAwait(false);
        try
        {
            await process.Invoke("context", JsonSerializer.SerializeToNode(options, EsbuildJsonContext.Default.EsbuildBuildOptions), cancellationToken).ConfigureAwait(false);
            return new EsbuildContext(process);
        }
        catch { await process.DisposeAsync().ConfigureAwait(false); throw; }
    }

    public async ValueTask<string> AnalyzeMetafile(string metafile, EsbuildAnalyzeOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metafile);
        await using EsbuildProcess process = await Start(null, cancellationToken).ConfigureAwait(false);
        return (await process.Invoke("analyze", new JsonObject { ["metafile"] = metafile,
            ["options"] = JsonSerializer.SerializeToNode(options ?? new(), EsbuildJsonContext.Default.EsbuildAnalyzeOptions) }, cancellationToken).ConfigureAwait(false))!.GetValue<string>();
    }

    public async ValueTask<string[]> FormatMessages(EsbuildMessage[] messages, EsbuildFormatOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        await using EsbuildProcess process = await Start(null, cancellationToken).ConfigureAwait(false);
        return (await process.Invoke("format", new JsonObject { ["messages"] = JsonSerializer.SerializeToNode(messages, EsbuildJsonContext.Default.EsbuildMessageArray),
            ["options"] = JsonSerializer.SerializeToNode(options ?? new(), EsbuildJsonContext.Default.EsbuildFormatOptions) }, cancellationToken).ConfigureAwait(false))
            .Deserialize(EsbuildJsonContext.Default.StringArray)!;
    }
}

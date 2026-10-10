using Soenneker.Esbuild.Util.Dtos;
using Soenneker.Utils.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soenneker.Esbuild.Util.Abstract;
using Soenneker.Esbuild.Util.Registrars;
using Soenneker.Node.Util.Abstract;
using Soenneker.Utils.Process.Abstract;
using Soenneker.Utils.HttpClientCache.Abstract;

namespace Soenneker.Esbuild.Util.Tests;

public sealed class EsbuildIntegrationTests
{
    [Test]
    public async Task Cli_operations_with_real_esbuild()
    {
        string directory = Path.Combine(Path.GetTempPath(), "esbuild integration " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(new EsbuildInstallOptions { Directory = Path.Combine(directory, "runtime") });
        services.AddEsbuildUtilAsSingleton();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IEsbuildUtil util = provider.GetRequiredService<IEsbuildUtil>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        CancellationToken ct = timeout.Token;
        try
        {
            IHttpClientCache cache = provider.GetRequiredService<IHttpClientCache>();
            HttpClient registry = await cache.Get("EsbuildIntegrationTests.Registry", ct);
            using JsonDocument? metadata = JsonUtil.Deserialize<JsonDocument>(await registry.GetStringAsync("https://registry.npmjs.org/esbuild/latest", ct));
            string latest = metadata!.RootElement.GetProperty("version").GetString()!;
            string[] installations = await Task.WhenAll(util.EnsureInstalled(ct).AsTask(), util.EnsureInstalled(ct).AsTask());
            Check(installations[0] == installations[1], "Concurrent installation paths differ.");
            Check(await util.GetVersion(ct) == latest, "Latest release was not used.");
            EsbuildCommandResult transformed = await util.Transform("const answer: number = 42; console.log(answer)",
                new EsbuildTransformOptions { Loader = "ts", Minify = true, Sourcemap = "inline" }, ct);
            Check(transformed.StandardOutput.Contains("42") && !transformed.StandardOutput.Contains(": number") &&
                transformed.StandardOutput.Contains("sourceMappingURL=data:"), "TypeScript transform or inline source map failed.");
            EsbuildCommandResult css = await util.Transform("body { color: #ff0000; }", new EsbuildTransformOptions { Loader = "css", Minify = true }, ct);
            Check(css.StandardOutput.Contains("red"), "CSS minification failed.");
            EsbuildCommandResult define = await util.Transform("console.log(MESSAGE)", new EsbuildTransformOptions
            {
                Define = new Dictionary<string, string> { ["MESSAGE"] = JsonUtil.Serialize("spaces & quotes \" work")! },
                Banner = "// banner with spaces", Footer = "// footer with spaces"
            }, ct);
            Check(define.StandardOutput.Contains("spaces & quotes") && define.StandardOutput.Contains("banner with spaces") &&
                define.StandardOutput.Contains("footer with spaces"), "CLI quoting or banner/footer mapping failed.");
            await File.WriteAllTextAsync(Path.Combine(directory, "dependency.ts"), "export const value = 42;", ct);
            await File.WriteAllTextAsync(Path.Combine(directory, "entry file.ts"), "import {value} from './dependency'; console.log(value);", ct);
            var options = new EsbuildBuildOptions
            {
                AbsWorkingDir = directory, EntryPoints = ["entry file.ts"], Bundle = true, Format = "esm",
                Outdir = "dist", Metafile = "meta.json", Minify = true, Sourcemap = "external",
                Define = new Dictionary<string, string> { ["process.env.NODE_ENV"] = "\"production\"" }
            };
            await util.Build(options, ct);
            string output = Path.Combine(directory, "dist", "entry file.js");
            Check(File.ReadAllText(output).Contains("42") && File.Exists(output + ".map"), "File build or external source map failed.");
            using JsonDocument? metafile = JsonUtil.Deserialize<JsonDocument>(await File.ReadAllTextAsync(Path.Combine(directory, "meta.json"), ct));
            Check(metafile!.RootElement.GetProperty("inputs").EnumerateObject().Count() == 2, "Metafile missing dependencies.");
            EsbuildCommandResult analysis = await util.Analyze(options, new EsbuildAnalyzeOptions { Verbose = true, Color = false }, ct);
            Check(analysis.StandardError.Contains("entry file") && analysis.StandardError.Contains('%'), "Bundle analysis missing: " + analysis.StandardError);
            options.Sourcemap = null;
            await using (IEsbuildSession watcher = await util.Watch(options, new EsbuildWatchOptions { Delay = 10 }, ct))
            {
                await WaitUntil(() => watcher.StandardError.Contains("watching for changes"), ct);
                await File.WriteAllTextAsync(Path.Combine(directory, "dependency.ts"), "export const value = 43;", ct);
                await WaitUntil(() => File.ReadAllText(output).Contains("43"), ct);
                await watcher.Stop();
                Check(watcher.HasExited, "Watch did not stop.");
            }
            int port;
            await using (IEsbuildSession server = await util.Serve(options, new EsbuildServeOptions { Host = "127.0.0.1", Port = 0, Servedir = directory }, true, ct))
            {
                await WaitUntil(() => Regex.IsMatch(server.StandardError, @"http://127\.0\.0\.1:\d+"), ct);
                string address = Regex.Match(server.StandardError, @"http://127\.0\.0\.1:\d+").Value;
                port = new Uri(address).Port;
                HttpClient http = await cache.Get("EsbuildIntegrationTests.Server", ct);
                Check((await http.GetStringAsync(address + "/dist/entry%20file.js", ct)).Contains("43"), "Server output incorrect.");
            }
            await WaitUntil(() => !IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(endpoint => endpoint.Port == port), ct);
            EsbuildCommandResult stdin = await util.Build(new EsbuildBuildOptions
            {
                Stdin = new EsbuildStdinOptions { Contents = "import {value} from './dependency'; console.log(value)", ResolveDir = directory, Loader = "ts" },
                Bundle = true, Minify = true
            }, ct);
            Check(stdin.StandardOutput.Contains("43"), "Stdin bundling failed.");
            Check((await util.Run(["--help"], cancellationToken: ct)).StandardOutput.Contains("Usage:"), "Raw CLI command failed.");
            try
            {
                await util.Transform("const = ;", cancellationToken: ct);
                throw new Exception("Invalid syntax unexpectedly succeeded.");
            }
            catch (EsbuildException ex)
            {
                Check(ex.Result.ExitCode != 0 && ex.Result.StandardError.Contains("ERROR"), "CLI diagnostics missing.");
            }
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
            await using (IEsbuildSession watcher = await util.Watch(options, cancellationToken: cancellation.Token))
            {
                cancellation.Cancel();
                await watcher.WaitForExit(ct);
                Check(watcher.HasExited, "Cancellation left a watcher running.");
            }
            await File.WriteAllTextAsync(installations[0], "#!/usr/bin/env node\nthrow new Error('incomplete installation');", ct);
            var repaired = new EsbuildUtil(provider.GetRequiredService<INodeUtil>(), provider.GetRequiredService<IProcessUtil>(), cache,
                new EsbuildInstallOptions { Directory = Path.Combine(directory, "runtime") });
            Check(await repaired.GetVersion(ct) == latest, "Installation recovery failed.");
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static async Task WaitUntil(Func<bool> condition, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        while (true)
        {
            try { if (condition()) return; }
            catch (IOException) { }
            await Task.Delay(100, timeout.Token);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}

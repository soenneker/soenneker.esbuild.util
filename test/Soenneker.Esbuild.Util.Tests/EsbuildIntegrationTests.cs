using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soenneker.Esbuild.Util;
using Soenneker.Esbuild.Util.Abstract;
using Soenneker.Esbuild.Util.Registrars;

namespace Soenneker.Esbuild.Util.Tests;

public sealed class EsbuildIntegrationTests
{
    [Test]
    public async Task Operations_round_trip_with_real_esbuild()
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
            string[] installations = await Task.WhenAll(util.EnsureInstalled(ct).AsTask(), util.EnsureInstalled(ct).AsTask());
            Check(installations[0] == installations[1], "Concurrent installation paths differ.");
            Check(await util.GetVersion(ct) == "0.28.2", "Wrong esbuild version.");
            EsbuildTransformResult transform = await util.Transform("const answer: number = 42; console.log(answer)",
                new() { Loader = "ts", Minify = true, Sourcemap = JsonSerializer.SerializeToElement(true) }, ct);
            Check(!transform.Code.Contains(": number") && transform.Code.Contains("42") && transform.Map.Length > 0, "TypeScript transform or source map failed.");
            EsbuildTransformResult css = await util.Transform("body { color: #ff0000; }", new() { Loader = "css", Minify = true }, ct);
            Check(css.Code.Contains("red"), "CSS minification failed.");
            await File.WriteAllTextAsync(Path.Combine(directory, "dependency.ts"), "export const value = 42;", ct);
            await File.WriteAllTextAsync(Path.Combine(directory, "entry.ts"), "import {value} from './dependency'; console.log(value);", ct);
            var options = new EsbuildBuildOptions
            {
                AbsWorkingDir = directory, EntryPoints = ["entry.ts"], Bundle = true, Format = "esm",
                Outdir = "dist", Metafile = true, Minify = true
            };
            EsbuildBuildResult build = await util.Build(options, ct);
            Check(build.OutputFiles.Length == 1 && Encoding.UTF8.GetString(build.OutputFiles[0].Contents).Contains("42"), "Bundle bytes incorrect.");
            Check(!Directory.Exists(Path.Combine(directory, "dist")), "In-memory build wrote files.");
            Check((await util.AnalyzeMetafile(build.Metafile!.Value.GetRawText(), cancellationToken: ct)).Contains("entry"), "Metafile analysis failed.");
            int serverPort;
            await using (IEsbuildContext context = await util.CreateContext(options, ct))
            {
                await context.Rebuild(ct);
                await File.WriteAllTextAsync(Path.Combine(directory, "dependency.ts"), "export const value = 43;", ct);
                EsbuildBuildResult rebuilt = await context.Rebuild(ct);
                Check(Encoding.UTF8.GetString(rebuilt.OutputFiles[0].Contents).Contains("43"), "Incremental rebuild missed edit.");
                await context.Cancel(ct);
                Check((await context.Rebuild(ct)).Errors.Length == 0, "Context not reusable after cancel.");
                await context.Watch(cancellationToken: ct);
                EsbuildServeResult server = await context.Serve(new() { Host = "127.0.0.1", Port = 0, Servedir = directory }, ct);
                serverPort = server.Port;
                using var http = new HttpClient();
                string served = await http.GetStringAsync($"http://127.0.0.1:{server.Port}/dist/entry.js", ct);
                Check(served.Contains("43"), "Development server returned wrong build.");
            }
            Check(!IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(endpoint => endpoint.Port == serverPort),
                "Context disposal left the server listening.");
            options.Write = true;
            await util.Build(options, ct);
            Check(File.Exists(Path.Combine(directory, "dist", "entry.js")), "Disk output missing.");
            try
            {
                await util.Transform("const = ;", cancellationToken: ct);
                throw new Exception("Invalid syntax unexpectedly succeeded.");
            }
            catch (EsbuildException ex)
            {
                Check(ex.Errors.Length > 0 && ex.Errors[0].Location != null, "Structured diagnostics missing.");
                string[] formatted = await util.FormatMessages(ex.Errors, cancellationToken: ct);
                Check(formatted.Any(x => x.Contains("ERROR")), "Diagnostic formatting failed.");
            }
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            try { await util.Build(options, cancelled.Token); throw new Exception("Cancellation ignored."); }
            catch (OperationCanceledException) { }
            // A new utility instance must repair a broken installation instead of accepting a loadable package alone.
            await File.WriteAllTextAsync(Path.Combine(installations[0], "lib", "main.js"), "throw new Error('incomplete installation');", ct);
            var repaired = new EsbuildUtil(provider.GetRequiredService<Soenneker.Node.Util.Abstract.INodeUtil>(),
                new() { Directory = Path.Combine(directory, "runtime") });
            Check(await repaired.GetVersion(ct) == "0.28.2", "Installation recovery failed.");
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}

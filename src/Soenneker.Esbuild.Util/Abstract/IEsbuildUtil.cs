using Soenneker.Esbuild.Util.Dtos;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Esbuild.Util.Abstract;

/// <summary>Installs the latest esbuild using Node/npm and runs its CLI through Soenneker.Utils.Process.</summary>
public interface IEsbuildUtil
{
    /// <summary>Checks npm's latest release, installs it if needed, verifies the CLI, and returns its path. Requires network access.</summary>
    ValueTask<string> EnsureInstalled(CancellationToken cancellationToken = default);

    /// <summary>Ensures the latest release is installed and runs --version.</summary>
    ValueTask<string> GetVersion(CancellationToken cancellationToken = default);

    /// <summary>Runs a build. Outfile/Outdir write to disk; omitting both returns generated text in StandardOutput. Nonzero exit throws EsbuildException.</summary>
    ValueTask<EsbuildCommandResult> Build(EsbuildBuildOptions options, CancellationToken cancellationToken = default);

    /// <summary>Transforms stdin to stdout without bundling. Source maps can be inlined. Diagnostics are returned separately on stderr.</summary>
    ValueTask<EsbuildCommandResult> Transform(string input, EsbuildTransformOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Builds with --analyze or --analyze=verbose. The bundle report is returned in StandardError.</summary>
    ValueTask<EsbuildCommandResult> Analyze(EsbuildBuildOptions options, EsbuildAnalyzeOptions? analysis = null, CancellationToken cancellationToken = default);

    /// <summary>Starts --watch=forever. The session captures rebuild diagnostics; dispose or cancel to stop. Returns before the initial build completes.</summary>
    ValueTask<IEsbuildSession> Watch(EsbuildBuildOptions options, EsbuildWatchOptions? watch = null, CancellationToken cancellationToken = default);

    /// <summary>Starts a development server, optionally watching for edits. Stderr contains server addresses. Returns before readiness; dispose or cancel to stop.</summary>
    ValueTask<IEsbuildSession> Serve(EsbuildBuildOptions options, EsbuildServeOptions? server = null, bool watch = false, CancellationToken cancellationToken = default);

    /// <summary>Runs CLI arguments without shell interpretation. Input is sent to stdin. Nonzero exit throws EsbuildException. Use Start for long-running commands.</summary>
    ValueTask<EsbuildCommandResult> Run(IReadOnlyList<string> arguments, string? workingDirectory = null, string? input = null, CancellationToken cancellationToken = default);

    /// <summary>Starts CLI arguments without shell interpretation. Stdin is closed after supplied input; otherwise it stays open until disposal. Caller owns the session.</summary>
    ValueTask<IEsbuildSession> Start(IReadOnlyList<string> arguments, string? workingDirectory = null, string? input = null, CancellationToken cancellationToken = default);
}

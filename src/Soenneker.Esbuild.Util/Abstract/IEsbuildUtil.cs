using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Esbuild.Util.Abstract;

/// <summary>Installs Node.js and esbuild as needed and exposes esbuild's JavaScript API from C#.</summary>
public interface IEsbuildUtil
{
    /// <summary>Ensures Node.js and the configured esbuild version are installed, verifies the binary, and returns the esbuild module path.</summary>
    ValueTask<string> EnsureInstalled(CancellationToken cancellationToken = default);

    /// <summary>Returns the installed esbuild version after ensuring installation.</summary>
    ValueTask<string> GetVersion(CancellationToken cancellationToken = default);

    /// <summary>Builds or bundles entry points or stdin. Relative paths use AbsWorkingDir, or the current directory. Throws EsbuildException for build errors.</summary>
    ValueTask<EsbuildBuildResult> Build(EsbuildBuildOptions options, CancellationToken cancellationToken = default);

    /// <summary>Transforms a source string, including transpilation, minification and source maps. Does not resolve imports or bundle.</summary>
    ValueTask<EsbuildTransformResult> Transform(string input, EsbuildTransformOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Creates an incremental build context. The caller must dispose it to stop its process, watcher and development server.</summary>
    ValueTask<IEsbuildContext> CreateContext(EsbuildBuildOptions options, CancellationToken cancellationToken = default);

    /// <summary>Produces a human-readable bundle size report from a build's metafile JSON.</summary>
    ValueTask<string> AnalyzeMetafile(string metafile, EsbuildAnalyzeOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Formats diagnostics using esbuild's source excerpts and terminal formatting.</summary>
    ValueTask<string[]> FormatMessages(EsbuildMessage[] messages, EsbuildFormatOptions? options = null, CancellationToken cancellationToken = default);
}

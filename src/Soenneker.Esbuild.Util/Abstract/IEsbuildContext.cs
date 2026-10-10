using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Esbuild.Util.Abstract;

/// <summary>An incremental esbuild context. Dispose to release its process, watcher and server.</summary>
/// <remarks>Cancelling an operation's token terminates the bridge and makes the context unusable. Use Cancel to stop a build while retaining the context.</remarks>
public interface IEsbuildContext : IAsyncDisposable
{
    /// <summary>Builds using the context's options and cached work from previous builds.</summary>
    ValueTask<EsbuildBuildResult> Rebuild(CancellationToken cancellationToken = default);

    /// <summary>Starts watching files and rebuilding on changes. Returns once watching starts; dispose the context to stop.</summary>
    ValueTask Watch(EsbuildWatchOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Starts a development server and returns its addresses. Dispose the context to stop the server.</summary>
    ValueTask<EsbuildServeResult> Serve(EsbuildServeOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Cancels the current build and waits for cancellation to finish. The context remains available for subsequent builds.</summary>
    ValueTask Cancel(CancellationToken cancellationToken = default);
}

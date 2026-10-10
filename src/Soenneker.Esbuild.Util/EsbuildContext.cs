using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Esbuild.Util.Abstract;

namespace Soenneker.Esbuild.Util;

internal sealed class EsbuildContext(EsbuildProcess process) : IEsbuildContext
{
    public async ValueTask<EsbuildBuildResult> Rebuild(CancellationToken cancellationToken = default) =>
        (await process.Invoke("rebuild", null, cancellationToken).ConfigureAwait(false)).Deserialize(EsbuildJsonContext.Default.EsbuildBuildResult)!;

    public async ValueTask Watch(EsbuildWatchOptions? options = null, CancellationToken cancellationToken = default) =>
        await process.Invoke("watch", JsonSerializer.SerializeToNode(options ?? new(), EsbuildJsonContext.Default.EsbuildWatchOptions), cancellationToken).ConfigureAwait(false);

    public async ValueTask<EsbuildServeResult> Serve(EsbuildServeOptions? options = null, CancellationToken cancellationToken = default) =>
        (await process.Invoke("serve", JsonSerializer.SerializeToNode(options ?? new(), EsbuildJsonContext.Default.EsbuildServeOptions), cancellationToken).ConfigureAwait(false))
        .Deserialize(EsbuildJsonContext.Default.EsbuildServeResult)!;

    public async ValueTask Cancel(CancellationToken cancellationToken = default) =>
        await process.Invoke("cancel", null, cancellationToken).ConfigureAwait(false);

    public ValueTask DisposeAsync() => process.DisposeAsync();
}

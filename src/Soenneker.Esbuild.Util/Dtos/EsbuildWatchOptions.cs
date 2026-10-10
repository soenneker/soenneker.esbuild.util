namespace Soenneker.Esbuild.Util.Dtos;

/// <summary>File watcher settings.</summary>
public sealed class EsbuildWatchOptions
{
    /// <summary>Maps to esbuild’s delay option. Null uses esbuild’s default.</summary>
    public int? Delay { get; set; }

}

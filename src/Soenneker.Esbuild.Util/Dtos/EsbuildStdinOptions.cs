namespace Soenneker.Esbuild.Util.Dtos;

/// <summary>Source and resolution settings for a build supplied in memory.</summary>
public sealed class EsbuildStdinOptions
{
    /// <summary>Source text to build.</summary>
    public string Contents { get; set; } = string.Empty;

    /// <summary>Working directory for stdin import resolution. Overrides AbsWorkingDir for this command.</summary>
    public string? ResolveDir { get; set; }

    /// <summary>Maps to esbuild’s sourcefile option. Null uses esbuild’s default.</summary>
    public string? Sourcefile { get; set; }

    /// <summary>Maps to esbuild’s loader option. Null uses esbuild’s default.</summary>
    public string? Loader { get; set; }

}

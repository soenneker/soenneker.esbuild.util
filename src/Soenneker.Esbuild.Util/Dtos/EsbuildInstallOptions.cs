namespace Soenneker.Esbuild.Util.Dtos;

/// <summary>Controls the private esbuild installation. The caller’s package.json is never changed.</summary>
public sealed class EsbuildInstallOptions
{
    /// <summary>Optional cache directory. Each automatically resolved release is installed in its own subdirectory.</summary>
    public string? Directory { get; set; }

    /// <summary>Minimum Node.js version passed to INodeUtil.EnsureInstalled.</summary>
    public string? MinimumNodeVersion { get; set; } = "18";

}

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util;

/// <summary>Controls the private esbuild installation. The caller’s package.json is never changed.</summary>
public sealed class EsbuildInstallOptions
{
    /// <summary>Exact esbuild version to install.</summary>
    public string Version { get; set; } = "0.28.2";

    /// <summary>Optional dedicated installation directory. Must not be a project directory.</summary>
    public string? Directory { get; set; }

    /// <summary>Minimum Node.js version passed to INodeUtil.EnsureInstalled.</summary>
    public string? MinimumNodeVersion { get; set; } = "18";

}

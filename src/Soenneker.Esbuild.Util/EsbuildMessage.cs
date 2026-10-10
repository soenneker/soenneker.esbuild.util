using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util;

/// <summary>An esbuild error or warning, including source location and notes.</summary>
public sealed class EsbuildMessage
{
    /// <summary>Diagnostic identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Plugin that emitted the diagnostic.</summary>
    public string PluginName { get; set; } = string.Empty;

    /// <summary>Diagnostic text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Source location, if available.</summary>
    public EsbuildLocation? Location { get; set; }

    /// <summary>Additional diagnostic notes.</summary>
    public EsbuildNote[] Notes { get; set; } = [];

}

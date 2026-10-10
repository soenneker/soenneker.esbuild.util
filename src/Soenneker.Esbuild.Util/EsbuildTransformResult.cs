using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util;

/// <summary>Transformed source, source map and diagnostics.</summary>
public sealed class EsbuildTransformResult
{
    /// <summary>Transformed source.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Source map, when requested.</summary>
    public string Map { get; set; } = string.Empty;

    /// <summary>External legal comments.</summary>
    public string? LegalComments { get; set; }

    /// <summary>Transform warnings.</summary>
    public EsbuildMessage[] Warnings { get; set; } = [];

    /// <summary>Updated property mangling cache.</summary>
    public Dictionary<string, JsonElement>? MangleCache { get; set; }

}

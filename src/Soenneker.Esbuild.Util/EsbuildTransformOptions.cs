using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util;

/// <summary>Options for transforming a single source string without bundling.</summary>
public sealed class EsbuildTransformOptions : EsbuildCommonOptions
{
    /// <summary>Maps to esbuild’s loader option. Null uses esbuild’s default.</summary>
    public string? Loader { get; set; }

    /// <summary>Maps to esbuild’s banner option. Null uses esbuild’s default.</summary>
    public string? Banner { get; set; }

    /// <summary>Maps to esbuild’s footer option. Null uses esbuild’s default.</summary>
    public string? Footer { get; set; }

}

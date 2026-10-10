using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util;

/// <summary>Controls metafile analysis output.</summary>
public sealed class EsbuildAnalyzeOptions
{
    /// <summary>Maps to esbuild’s verbose option. Null uses esbuild’s default.</summary>
    public bool? Verbose { get; set; }

    /// <summary>Maps to esbuild’s color option. Null uses esbuild’s default.</summary>
    public bool? Color { get; set; }

}

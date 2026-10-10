using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util;

/// <summary>Controls diagnostic formatting.</summary>
public sealed class EsbuildFormatOptions
{
    /// <summary>Diagnostic kind: error or warning.</summary>
    public string Kind { get; set; } = "error";

    /// <summary>Maps to esbuild’s color option. Null uses esbuild’s default.</summary>
    public bool? Color { get; set; }

    /// <summary>Maps to esbuild’s terminalWidth option. Null uses esbuild’s default.</summary>
    public int? TerminalWidth { get; set; }

}

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util;

/// <summary>Source location for a diagnostic. Lines are one-based; columns and lengths count bytes.</summary>
public sealed class EsbuildLocation
{
    /// <summary>Source file.</summary>
    public string File { get; set; } = string.Empty;

    /// <summary>Source namespace.</summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>One-based line number.</summary>
    public int Line { get; set; }

    /// <summary>Zero-based byte column.</summary>
    public int Column { get; set; }

    /// <summary>Length in bytes.</summary>
    public int Length { get; set; }

    /// <summary>Source line.</summary>
    public string LineText { get; set; } = string.Empty;

    /// <summary>Suggested replacement.</summary>
    public string Suggestion { get; set; } = string.Empty;

}

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util;

/// <summary>An additional diagnostic note.</summary>
public sealed class EsbuildNote
{
    /// <summary>Note text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Source location, if available.</summary>
    public EsbuildLocation? Location { get; set; }

}

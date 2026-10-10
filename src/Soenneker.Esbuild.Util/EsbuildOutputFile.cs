using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util;

/// <summary>An emitted file. Contents preserves binary output without text conversion.</summary>
public sealed class EsbuildOutputFile
{
    /// <summary>Output path.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Raw output bytes.</summary>
    public byte[] Contents { get; set; } = [];

    /// <summary>esbuild content hash.</summary>
    public string Hash { get; set; } = string.Empty;

}

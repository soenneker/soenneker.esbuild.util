using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util;

/// <summary>Development server settings. Dispose the owning context to stop the server.</summary>
public sealed class EsbuildServeOptions
{
    /// <summary>Maps to esbuild’s port option. Null uses esbuild’s default.</summary>
    public int? Port { get; set; }

    /// <summary>Maps to esbuild’s host option. Null uses esbuild’s default.</summary>
    public string? Host { get; set; }

    /// <summary>Maps to esbuild’s servedir option. Null uses esbuild’s default.</summary>
    public string? Servedir { get; set; }

    /// <summary>Maps to esbuild’s keyfile option. Null uses esbuild’s default.</summary>
    public string? Keyfile { get; set; }

    /// <summary>Maps to esbuild’s certfile option. Null uses esbuild’s default.</summary>
    public string? Certfile { get; set; }

    /// <summary>Maps to esbuild’s fallback option. Null uses esbuild’s default.</summary>
    public string? Fallback { get; set; }

    /// <summary>Maps to esbuild’s verbose option. Null uses esbuild’s default.</summary>
    public bool? Verbose { get; set; }

}

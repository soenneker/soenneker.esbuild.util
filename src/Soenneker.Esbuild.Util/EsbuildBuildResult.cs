using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util;

/// <summary>Build diagnostics, optional metafile, and in-memory output files.</summary>
public sealed class EsbuildBuildResult
{
    /// <summary>Build errors.</summary>
    public EsbuildMessage[] Errors { get; set; } = [];

    /// <summary>Build warnings.</summary>
    public EsbuildMessage[] Warnings { get; set; } = [];

    /// <summary>Output files returned when Write is false.</summary>
    public EsbuildOutputFile[] OutputFiles { get; set; } = [];

    /// <summary>Dependency and bundle size metadata when requested.</summary>
    public JsonElement? Metafile { get; set; }

    /// <summary>Updated property mangling cache.</summary>
    public Dictionary<string, JsonElement>? MangleCache { get; set; }

}

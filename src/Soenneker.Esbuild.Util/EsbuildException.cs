using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Soenneker.Esbuild.Util;

/// <summary>An esbuild failure with structured errors and warnings.</summary>
public sealed class EsbuildException : Exception
{
    /// <summary>Errors reported by esbuild.</summary>
    public EsbuildMessage[] Errors { get; }

    /// <summary>Warnings reported before the failure.</summary>
    public EsbuildMessage[] Warnings { get; }

    internal EsbuildException(JsonNode error) : base(error["message"]?.GetValue<string>() ?? "esbuild failed.")
    {
        Errors = error["errors"].Deserialize(EsbuildJsonContext.Default.EsbuildMessageArray) ?? [];
        Warnings = error["warnings"].Deserialize(EsbuildJsonContext.Default.EsbuildMessageArray) ?? [];
    }
}

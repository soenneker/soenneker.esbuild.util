using Soenneker.Esbuild.Util.Dtos;
using Soenneker.Utils.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Soenneker.Esbuild.Util;

internal static class EsbuildArguments
{
    internal static List<string> Build(EsbuildBuildOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Stdin != null && options.EntryPoints is { Length: > 0 })
            throw new ArgumentException("Specify entry points or stdin, not both.", nameof(options));
        List<string> result = Convert(JsonUtil.SerializeToElement(options, EsbuildJsonContext.Default.EsbuildBuildOptions));
        if (options.Stdin is { } stdin)
        {
            Add(result, "loader", stdin.Loader);
            Add(result, "sourcefile", stdin.Sourcefile);
        }
        if (options.AdditionalArguments != null) result.AddRange(options.AdditionalArguments);
        if (options.EntryPoints != null)
            foreach (string entry in options.EntryPoints)
            {
                // A filename beginning with '-' must not become a CLI flag.
                result.Add(entry.StartsWith('-') ? "./" + entry : entry);
            }
        return result;
    }

    internal static List<string> Transform(EsbuildTransformOptions options)
    {
        List<string> result = Convert(JsonUtil.SerializeToElement(options, EsbuildJsonContext.Default.EsbuildTransformOptions));
        if (options.AdditionalArguments != null) result.AddRange(options.AdditionalArguments);
        return result;
    }

    private static List<string> Convert(JsonElement options)
    {
        var result = new List<string>();
        foreach (JsonProperty property in options.EnumerateObject())
        {
            string name = property.Name;
            JsonElement value = property.Value;
            if (value.ValueKind == JsonValueKind.Null || name is "entryPoints" or "absWorkingDir" or "stdin" or "nodePaths") continue;
            string flag = Regex.Replace(name, "[A-Z]", match => "-" + match.Value.ToLowerInvariant());
            switch (value.ValueKind)
            {
                case JsonValueKind.True:
                case JsonValueKind.False:
                    if (name is "color" or "treeShaking" or "sourcesContent" or "mangleQuoted")
                        result.Add($"--{flag}={value.GetRawText()}");
                    else if (value.GetBoolean()) result.Add($"--{flag}");
                    else if (name != "bundle") result.Add($"--{flag}=false");
                    break;
                case JsonValueKind.Array:
                    string[] values = value.EnumerateArray().Select(item => item.GetString()!).ToArray();
                    if (name is "external" or "inject" or "pure" or "drop")
                        foreach (string item in values) result.Add($"--{flag}:{item}");
                    else result.Add($"--{flag}={string.Join(',', values)}");
                    break;
                case JsonValueKind.Object when name != "tsconfigRaw":
                    foreach (JsonProperty item in value.EnumerateObject())
                        result.Add($"--{flag}:{item.Name}={(item.Value.ValueKind == JsonValueKind.String ? item.Value.GetString() : item.Value.GetRawText())}");
                    break;
                default:
                    result.Add($"--{flag}={(value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText())}");
                    break;
            }
        }
        return result;
    }

    internal static void Add(List<string> arguments, string flag, string? value)
    {
        if (value != null) arguments.Add($"--{flag}={value}");
    }
}

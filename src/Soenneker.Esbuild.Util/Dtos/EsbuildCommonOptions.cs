using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util.Dtos;

/// <summary>Shared CLI options. Sourcemap accepts linked, inline, external, or both.</summary>
public class EsbuildCommonOptions
{
    /// <summary>Additional CLI arguments, each passed as a separate argument without shell interpretation.</summary>
    [JsonIgnore]
    public string[]? AdditionalArguments { get; set; }

    /// <summary>Maps to esbuild’s minify option. Null uses esbuild’s default.</summary>
    public bool? Minify { get; set; }

    /// <summary>Maps to esbuild’s minifyWhitespace option. Null uses esbuild’s default.</summary>
    public bool? MinifyWhitespace { get; set; }

    /// <summary>Maps to esbuild’s minifyIdentifiers option. Null uses esbuild’s default.</summary>
    public bool? MinifyIdentifiers { get; set; }

    /// <summary>Maps to esbuild’s minifySyntax option. Null uses esbuild’s default.</summary>
    public bool? MinifySyntax { get; set; }

    /// <summary>Maps to esbuild’s keepNames option. Null uses esbuild’s default.</summary>
    public bool? KeepNames { get; set; }

    /// <summary>Maps to esbuild’s treeShaking option. Null uses esbuild’s default.</summary>
    public bool? TreeShaking { get; set; }

    /// <summary>Maps to esbuild’s ignoreAnnotations option. Null uses esbuild’s default.</summary>
    public bool? IgnoreAnnotations { get; set; }

    /// <summary>Maps to esbuild’s sourcesContent option. Null uses esbuild’s default.</summary>
    public bool? SourcesContent { get; set; }

    /// <summary>Maps to esbuild’s jsxDev option. Null uses esbuild’s default.</summary>
    public bool? JsxDev { get; set; }

    /// <summary>Maps to esbuild’s jsxSideEffects option. Null uses esbuild’s default.</summary>
    public bool? JsxSideEffects { get; set; }

    /// <summary>Maps to esbuild’s mangleQuoted option. Null uses esbuild’s default.</summary>
    public bool? MangleQuoted { get; set; }

    /// <summary>Maps to esbuild’s color option. Null uses esbuild’s default.</summary>
    public bool? Color { get; set; }

    /// <summary>Maps to esbuild’s format option. Null uses esbuild’s default.</summary>
    public string? Format { get; set; }

    /// <summary>Maps to esbuild’s platform option. Null uses esbuild’s default.</summary>
    public string? Platform { get; set; }

    /// <summary>Maps to esbuild’s charset option. Null uses esbuild’s default.</summary>
    public string? Charset { get; set; }

    /// <summary>Maps to esbuild’s legalComments option. Null uses esbuild’s default.</summary>
    public string? LegalComments { get; set; }

    /// <summary>Maps to esbuild’s sourceRoot option. Null uses esbuild’s default.</summary>
    public string? SourceRoot { get; set; }

    /// <summary>Maps to esbuild’s sourcefile option. Null uses esbuild’s default.</summary>
    public string? Sourcefile { get; set; }

    /// <summary>Maps to esbuild’s jsx option. Null uses esbuild’s default.</summary>
    public string? Jsx { get; set; }

    /// <summary>Maps to esbuild’s jsxFactory option. Null uses esbuild’s default.</summary>
    public string? JsxFactory { get; set; }

    /// <summary>Maps to esbuild’s jsxFragment option. Null uses esbuild’s default.</summary>
    public string? JsxFragment { get; set; }

    /// <summary>Maps to esbuild’s jsxImportSource option. Null uses esbuild’s default.</summary>
    public string? JsxImportSource { get; set; }

    /// <summary>Maps to esbuild’s mangleProps option. Null uses esbuild’s default.</summary>
    public string? MangleProps { get; set; }

    /// <summary>Maps to esbuild’s reserveProps option. Null uses esbuild’s default.</summary>
    public string? ReserveProps { get; set; }

    /// <summary>Maps to esbuild’s logLevel option. Null uses esbuild’s default.</summary>
    public string? LogLevel { get; set; }

    /// <summary>Maps to esbuild’s lineLimit option. Null uses esbuild’s default.</summary>
    public int? LineLimit { get; set; }

    /// <summary>Maps to esbuild’s logLimit option. Null uses esbuild’s default.</summary>
    public int? LogLimit { get; set; }

    /// <summary>Maps to esbuild’s target option. Null uses esbuild’s default.</summary>
    public string[]? Target { get; set; }

    /// <summary>Maps to esbuild’s drop option. Null uses esbuild’s default.</summary>
    public string[]? Drop { get; set; }

    /// <summary>Maps to esbuild’s dropLabels option. Null uses esbuild’s default.</summary>
    public string[]? DropLabels { get; set; }

    /// <summary>Maps to esbuild’s pure option. Null uses esbuild’s default.</summary>
    public string[]? Pure { get; set; }

    /// <summary>Maps to esbuild’s absPaths option. Null uses esbuild’s default.</summary>
    public string[]? AbsPaths { get; set; }

    /// <summary>Maps to esbuild’s define option. Null uses esbuild’s default.</summary>
    public Dictionary<string, string>? Define { get; set; }

    /// <summary>Maps to esbuild’s logOverride option. Null uses esbuild’s default.</summary>
    public Dictionary<string, string>? LogOverride { get; set; }

    /// <summary>Maps to esbuild’s supported option. Null uses esbuild’s default.</summary>
    public Dictionary<string, bool>? Supported { get; set; }

    /// <summary>Maps to esbuild’s sourcemap option. Null uses esbuild’s default.</summary>
    public string? Sourcemap { get; set; }

    /// <summary>Maps to esbuild’s tsconfigRaw option. Null uses esbuild’s default.</summary>
    public JsonElement? TsconfigRaw { get; set; }

    /// <summary>Path to the JSON file used by --mangle-cache.</summary>
    public string? MangleCache { get; set; }
}
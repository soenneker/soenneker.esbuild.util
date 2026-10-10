using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util;

/// <summary>Options for bundling JavaScript, TypeScript, JSX, CSS and other supported assets.</summary>
public sealed class EsbuildBuildOptions : EsbuildCommonOptions
{
    /// <summary>Maps to esbuild’s bundle option. Null uses esbuild’s default.</summary>
    public bool? Bundle { get; set; }

    /// <summary>Maps to esbuild’s splitting option. Null uses esbuild’s default.</summary>
    public bool? Splitting { get; set; }

    /// <summary>Maps to esbuild’s preserveSymlinks option. Null uses esbuild’s default.</summary>
    public bool? PreserveSymlinks { get; set; }

    /// <summary>Maps to esbuild’s allowOverwrite option. Null uses esbuild’s default.</summary>
    public bool? AllowOverwrite { get; set; }

    /// <summary>Maps to esbuild’s metafile option. Null uses esbuild’s default.</summary>
    public bool? Metafile { get; set; }

    /// <summary>Whether to write output files. Defaults to false so results are returned in memory.</summary>
    public bool Write { get; set; } = false;

    /// <summary>Maps to esbuild’s absWorkingDir option. Null uses esbuild’s default.</summary>
    public string? AbsWorkingDir { get; set; }

    /// <summary>Maps to esbuild’s outfile option. Null uses esbuild’s default.</summary>
    public string? Outfile { get; set; }

    /// <summary>Maps to esbuild’s outdir option. Null uses esbuild’s default.</summary>
    public string? Outdir { get; set; }

    /// <summary>Maps to esbuild’s outbase option. Null uses esbuild’s default.</summary>
    public string? Outbase { get; set; }

    /// <summary>Maps to esbuild’s publicPath option. Null uses esbuild’s default.</summary>
    public string? PublicPath { get; set; }

    /// <summary>Maps to esbuild’s assetNames option. Null uses esbuild’s default.</summary>
    public string? AssetNames { get; set; }

    /// <summary>Maps to esbuild’s chunkNames option. Null uses esbuild’s default.</summary>
    public string? ChunkNames { get; set; }

    /// <summary>Maps to esbuild’s entryNames option. Null uses esbuild’s default.</summary>
    public string? EntryNames { get; set; }

    /// <summary>Maps to esbuild’s globalName option. Null uses esbuild’s default.</summary>
    public string? GlobalName { get; set; }

    /// <summary>Maps to esbuild’s tsconfig option. Null uses esbuild’s default.</summary>
    public string? Tsconfig { get; set; }

    /// <summary>Maps to esbuild’s packages option. Null uses esbuild’s default.</summary>
    public string? Packages { get; set; }

    /// <summary>Maps to esbuild’s entryPoints option. Null uses esbuild’s default.</summary>
    public string[]? EntryPoints { get; set; }

    /// <summary>Maps to esbuild’s external option. Null uses esbuild’s default.</summary>
    public string[]? External { get; set; }

    /// <summary>Maps to esbuild’s inject option. Null uses esbuild’s default.</summary>
    public string[]? Inject { get; set; }

    /// <summary>Maps to esbuild’s mainFields option. Null uses esbuild’s default.</summary>
    public string[]? MainFields { get; set; }

    /// <summary>Maps to esbuild’s conditions option. Null uses esbuild’s default.</summary>
    public string[]? Conditions { get; set; }

    /// <summary>Maps to esbuild’s nodePaths option. Null uses esbuild’s default.</summary>
    public string[]? NodePaths { get; set; }

    /// <summary>Maps to esbuild’s resolveExtensions option. Null uses esbuild’s default.</summary>
    public string[]? ResolveExtensions { get; set; }

    /// <summary>Maps to esbuild’s loader option. Null uses esbuild’s default.</summary>
    public Dictionary<string, string>? Loader { get; set; }

    /// <summary>Maps to esbuild’s alias option. Null uses esbuild’s default.</summary>
    public Dictionary<string, string>? Alias { get; set; }

    /// <summary>Maps to esbuild’s outExtension option. Null uses esbuild’s default.</summary>
    public Dictionary<string, string>? OutExtension { get; set; }

    /// <summary>Maps to esbuild’s banner option. Null uses esbuild’s default.</summary>
    public Dictionary<string, string>? Banner { get; set; }

    /// <summary>Maps to esbuild’s footer option. Null uses esbuild’s default.</summary>
    public Dictionary<string, string>? Footer { get; set; }

    /// <summary>Optional source supplied instead of an entry-point file.</summary>
    public EsbuildStdinOptions? Stdin { get; set; }

}

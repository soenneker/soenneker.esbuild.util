using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util;

/// <summary>File watcher settings.</summary>
public sealed class EsbuildWatchOptions
{
    /// <summary>Maps to esbuild’s delay option. Null uses esbuild’s default.</summary>
    public int? Delay { get; set; }

}

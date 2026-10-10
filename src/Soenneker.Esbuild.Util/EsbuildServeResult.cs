using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util;

/// <summary>Addresses and port of a running development server.</summary>
public sealed class EsbuildServeResult
{
    /// <summary>Bound host addresses.</summary>
    public string[] Hosts { get; set; } = [];

    /// <summary>Bound port.</summary>
    public int Port { get; set; }

}

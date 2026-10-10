using Soenneker.Esbuild.Util.Dtos;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(EsbuildBuildOptions))]
[JsonSerializable(typeof(EsbuildTransformOptions))]
internal partial class EsbuildJsonContext : JsonSerializerContext;

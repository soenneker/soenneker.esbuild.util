using System.Text.Json.Serialization;

namespace Soenneker.Esbuild.Util;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(EsbuildBuildOptions))]
[JsonSerializable(typeof(EsbuildTransformOptions))]
[JsonSerializable(typeof(EsbuildServeOptions))]
[JsonSerializable(typeof(EsbuildWatchOptions))]
[JsonSerializable(typeof(EsbuildAnalyzeOptions))]
[JsonSerializable(typeof(EsbuildFormatOptions))]
[JsonSerializable(typeof(EsbuildBuildResult))]
[JsonSerializable(typeof(EsbuildTransformResult))]
[JsonSerializable(typeof(EsbuildServeResult))]
[JsonSerializable(typeof(EsbuildMessage[]))]
[JsonSerializable(typeof(string[]))]
internal partial class EsbuildJsonContext : JsonSerializerContext;

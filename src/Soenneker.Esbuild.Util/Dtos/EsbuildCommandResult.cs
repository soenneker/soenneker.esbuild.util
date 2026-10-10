namespace Soenneker.Esbuild.Util.Dtos;

/// <summary>The exit code and separate output streams from an esbuild command.</summary>
public sealed class EsbuildCommandResult
{
    /// <summary>Process exit code. Zero indicates success.</summary>
    public int ExitCode { get; init; }

    /// <summary>Text emitted to stdout, including generated code when no output path is specified.</summary>
    public string StandardOutput { get; init; } = string.Empty;

    /// <summary>Text emitted to stderr, including diagnostics and bundle analysis.</summary>
    public string StandardError { get; init; } = string.Empty;
}

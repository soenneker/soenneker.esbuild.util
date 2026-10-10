using Soenneker.Esbuild.Util.Dtos;
using System;

namespace Soenneker.Esbuild.Util;

/// <summary>An unsuccessful esbuild command with its exit code and CLI diagnostics.</summary>
public sealed class EsbuildException : Exception
{
    /// <summary>The command's exit code and captured output.</summary>
    public EsbuildCommandResult Result { get; }

    /// <summary>Creates an exception for an unsuccessful command.</summary>
    public EsbuildException(EsbuildCommandResult result) : base(
        $"esbuild exited with code {result.ExitCode}: {result.StandardError}") => Result = result;
}
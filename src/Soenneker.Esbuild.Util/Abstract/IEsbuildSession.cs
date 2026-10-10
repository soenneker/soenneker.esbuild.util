using Soenneker.Esbuild.Util.Dtos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Esbuild.Util.Abstract;

/// <summary>A running esbuild command. Dispose to stop its process tree and release resources.</summary>
public interface IEsbuildSession : IAsyncDisposable
{
    /// <summary>The operating system process ID.</summary>
    int ProcessId { get; }

    /// <summary>Whether the command has exited.</summary>
    bool HasExited { get; }

    /// <summary>Captured stdout so far. Output is captured as UTF-8 text lines.</summary>
    string StandardOutput { get; }

    /// <summary>Captured stderr so far, including watcher diagnostics and server addresses.</summary>
    string StandardError { get; }

    /// <summary>Waits for exit and returns output. Cancelling the wait terminates the process tree.</summary>
    ValueTask<EsbuildCommandResult> WaitForExit(CancellationToken cancellationToken = default);

    /// <summary>Stops the command and its child processes, then waits for exit.</summary>
    ValueTask Stop();
}

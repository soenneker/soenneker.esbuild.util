using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Esbuild.Util;

internal sealed class EsbuildProcess : IAsyncDisposable
{
    private readonly Process _process;
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonNode?>> _pending = new();
    private readonly SemaphoreSlim _writer = new(1, 1);
    private readonly Task<string> _stderr;
    private readonly Task _reader;
    private long _nextId;
    private int _disposed;

    internal EsbuildProcess(string node, string module, string directory)
    {
        var start = new ProcessStartInfo(node)
        {
            WorkingDirectory = directory, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        using Stream resource = typeof(EsbuildProcess).Assembly.GetManifestResourceStream("Soenneker.Esbuild.Util.bridge.cjs")!;
        using var script = new StreamReader(resource);
        start.ArgumentList.Add("-e");
        start.ArgumentList.Add(script.ReadToEnd());
        start.ArgumentList.Add(module);
        _process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the esbuild bridge.");
        _stderr = _process.StandardError.ReadToEndAsync();
        _reader = ReadResponses();
    }

    internal async ValueTask<JsonNode?> Invoke(string operation, JsonNode? payload, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        cancellationToken.ThrowIfCancellationRequested();
        long id = Interlocked.Increment(ref _nextId);
        var completion = new TaskCompletionSource<JsonNode?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = completion;
        try
        {
            await _writer.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_process.HasExited) throw new InvalidOperationException("The esbuild bridge has exited.");
                var request = new JsonObject { ["id"] = id, ["operation"] = operation, ["payload"] = payload };
                await _process.StandardInput.WriteLineAsync(request.ToJsonString().AsMemory(), cancellationToken).ConfigureAwait(false);
                await _process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally { _writer.Release(); }
            return await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // A partially written request or interrupted build cannot safely be reused.
            Kill();
            throw;
        }
        finally { _pending.TryRemove(id, out _); }
    }

    private async Task ReadResponses()
    {
        Exception failure;
        try
        {
            while (await _process.StandardOutput.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                JsonNode response = JsonNode.Parse(line)!;
                if (!_pending.TryRemove(response["id"]!.GetValue<long>(), out TaskCompletionSource<JsonNode?>? completion)) continue;
                if (response["error"] is { } error)
                    completion.TrySetException(new EsbuildException(error));
                else
                    completion.TrySetResult(response["result"]?.DeepClone());
            }
            await _process.WaitForExitAsync().ConfigureAwait(false);
            failure = new InvalidOperationException($"The esbuild bridge exited ({_process.ExitCode}). {await _stderr.ConfigureAwait(false)}");
        }
        catch (Exception ex) { failure = ex; Kill(); }
        foreach (var item in _pending)
            if (_pending.TryRemove(item.Key, out var completion)) completion.TrySetException(failure);
    }

    private void Kill()
    {
        try { if (!_process.HasExited) _process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        Kill();
        await _reader.ConfigureAwait(false);
        _process.Dispose();
    }
}

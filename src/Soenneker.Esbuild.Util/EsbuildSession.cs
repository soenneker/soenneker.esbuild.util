using Soenneker.Esbuild.Util.Dtos;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Esbuild.Util.Abstract;
using Soenneker.Utils.Process.Abstract;
using Soenneker.Utils.Process.Dtos;

namespace Soenneker.Esbuild.Util;

internal sealed class EsbuildSession : IEsbuildSession
{
    private readonly StringBuilder _output = new();
    private readonly StringBuilder _error = new();
    private readonly CancellationTokenSource _lifetime;
    private Process _process = null!;
    private int _disposed;

    private EsbuildSession(CancellationToken cancellationToken) =>
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

    internal static async ValueTask<EsbuildSession> Start(IProcessUtil processUtil, string executable,
        IReadOnlyList<string> arguments, string? directory, string? input, bool keepInputOpen,
        IReadOnlyDictionary<string, string>? environment, CancellationToken cancellationToken)
    {
        var session = new EsbuildSession(cancellationToken);
        try
        {
            session._process = await processUtil.StartDetached(new ProcessStartDto
                               {
                                   FileName = executable, ArgumentList = arguments, WorkingDirectory = directory,
                                   Log = false,
                                   RedirectStandardInput = true, EnvironmentVariables = environment,
                                   OutputCallback = line =>
                                   {
                                       lock (session._output)
                                           session._output.AppendLine(line);
                                   },
                                   ErrorCallback = line =>
                                   {
                                       lock (session._error)
                                           session._error.AppendLine(line);
                                   }
                               }, session._lifetime.Token).NoSync() ??
                               throw new InvalidOperationException($"Could not start {executable}.");
            if (input != null)
            {
                try
                {
                    await session._process.StandardInput.WriteAsync(input.AsMemory(), cancellationToken)
                                 .NoSync();
                    await session._process.StandardInput.FlushAsync(cancellationToken).NoSync();
                }
                catch (System.IO.IOException)
                {
                    await session._process.WaitForExitAsync(cancellationToken).NoSync();
                    if (session._process.ExitCode == 0)
                        throw;
                }
            }

            if (!keepInputOpen)
                session._process.StandardInput.Close();
            cancellationToken.ThrowIfCancellationRequested();
            return session;
        }
        catch
        {
            if (session._process != null)
                await session.DisposeAsync().NoSync();
            else
                session._lifetime.Dispose();
            throw;
        }
    }

    public int ProcessId => _process.Id;
    public bool HasExited => _process.HasExited;

    public string StandardOutput
    {
        get
        {
            lock (_output)
                return _output.ToString();
        }
    }

    public string StandardError
    {
        get
        {
            lock (_error)
                return _error.ToString();
        }
    }

    public async ValueTask<EsbuildCommandResult> WaitForExit(CancellationToken cancellationToken = default)
    {
        try
        {
            await _process.WaitForExitAsync(cancellationToken).NoSync();
        }
        catch (OperationCanceledException)
        {
            await Stop().NoSync();
            throw;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new EsbuildCommandResult
            { ExitCode = _process.ExitCode, StandardOutput = StandardOutput, StandardError = StandardError };
    }

    public async ValueTask Stop()
    {
        // ProcessUtil owns cancellation and kills the entire process tree, including CLI launchers.
        await _lifetime.CancelAsync().NoSync();
        await _process.WaitForExitAsync().NoSync();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        await Stop().NoSync();
        _process.Dispose();
        _lifetime.Dispose();
    }
}
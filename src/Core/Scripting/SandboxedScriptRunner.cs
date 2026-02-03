using Microsoft.CodeAnalysis.Scripting;

namespace SuperDeck.Core.Scripting;

public class SandboxedScriptRunner
{
    private readonly ScriptExecutionOptions _options;

    public SandboxedScriptRunner(ScriptExecutionOptions? options = null)
    {
        _options = options ?? new ScriptExecutionOptions();
    }

    public void Run<T>(Script<object> script, T globals, CancellationToken cancellationToken = default)
    {
        if (!_options.EnableMemoryLimit)
        {
            // Run without memory monitoring
            script.RunAsync(globals, cancellationToken).GetAwaiter().GetResult();
            return;
        }

        // Capture baseline memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long baselineMemory = GC.GetTotalMemory(forceFullCollection: false);

        using var memoryMonitorCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var memoryExceeded = false;
        long peakMemory = 0;

        // Start memory monitoring task
        var monitorTask = Task.Run(async () =>
        {
            while (!memoryMonitorCts.Token.IsCancellationRequested)
            {
                long currentMemory = GC.GetTotalMemory(forceFullCollection: false);
                long usedMemory = currentMemory - baselineMemory;

                if (usedMemory > peakMemory)
                {
                    peakMemory = usedMemory;
                }

                if (usedMemory > _options.MaxMemoryBytes)
                {
                    memoryExceeded = true;
                    memoryMonitorCts.Cancel();
                    break;
                }

                try
                {
                    await Task.Delay(_options.MemoryCheckIntervalMs, memoryMonitorCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, memoryMonitorCts.Token);

        try
        {
            // Run the script with combined cancellation
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_options.TimeoutMs));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                memoryMonitorCts.Token, timeoutCts.Token, cancellationToken);

            try
            {
                script.RunAsync(globals, combinedCts.Token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !memoryExceeded)
            {
                throw new ScriptTimeoutException($"Script execution timed out after {_options.TimeoutMs}ms");
            }
            catch (OperationCanceledException) when (memoryExceeded)
            {
                throw new ScriptMemoryException(peakMemory, _options.MaxMemoryBytes);
            }
        }
        finally
        {
            memoryMonitorCts.Cancel();
            try
            {
                monitorTask.Wait(100);
            }
            catch
            {
                // Ignore monitoring task exceptions
            }
        }

        // Final memory check after execution
        if (memoryExceeded)
        {
            throw new ScriptMemoryException(peakMemory, _options.MaxMemoryBytes);
        }
    }

    public Action<T> CreateRunner<T>(Script<object> script)
    {
        return globals => Run(script, globals);
    }
}

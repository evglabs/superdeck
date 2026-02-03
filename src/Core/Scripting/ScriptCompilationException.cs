namespace SuperDeck.Core.Scripting;

public class ScriptCompilationException : Exception
{
    public ScriptCompilationException(string message) : base(message) { }
    public ScriptCompilationException(string message, Exception inner) : base(message, inner) { }
}

public class ScriptTimeoutException : Exception
{
    public ScriptTimeoutException(string message) : base(message) { }
}

public class ScriptExecutionException : Exception
{
    public ScriptExecutionException(string message) : base(message) { }
    public ScriptExecutionException(string message, Exception inner) : base(message, inner) { }
}

public class ScriptMemoryException : Exception
{
    public long MemoryUsed { get; }
    public long MemoryLimit { get; }

    public ScriptMemoryException(long memoryUsed, long memoryLimit)
        : base($"Script exceeded memory limit: {memoryUsed / 1024 / 1024}MB used, {memoryLimit / 1024 / 1024}MB allowed")
    {
        MemoryUsed = memoryUsed;
        MemoryLimit = memoryLimit;
    }
}

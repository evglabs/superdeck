namespace SuperDeck.Core.Scripting;

public class ScriptExecutionOptions
{
    public int TimeoutMs { get; set; } = 500;
    public long MaxMemoryBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public bool EnableMemoryLimit { get; set; } = true;
    public int MemoryCheckIntervalMs { get; set; } = 50;
}

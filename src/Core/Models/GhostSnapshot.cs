namespace SuperDeck.Core.Models;

public class GhostSnapshot
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SourceCharacterId { get; set; } = string.Empty;
    public string SerializedCharacterState { get; set; } = string.Empty;  // JSON
    public int GhostMMR { get; set; } = 1000;
    public int Wins { get; set; } = 0;
    public int Losses { get; set; } = 0;
    public int TimesUsed { get; set; } = 0;
    public string AIProfileId { get; set; } = string.Empty;
    public DateTime? DownloadedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRetirementGhost { get; set; } = false;
}

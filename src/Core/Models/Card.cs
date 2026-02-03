using SuperDeck.Core.Models.Enums;

namespace SuperDeck.Core.Models;

public class Card
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Suit Suit { get; set; }
    public CardType Type { get; set; }
    public Rarity Rarity { get; set; }
    public string Description { get; set; } = string.Empty;

    // Script definitions (raw strings from JSON)
    public ImmediateEffect? ImmediateEffect { get; set; }
    public GrantsStatus? GrantsStatusTo { get; set; }

    // Animation (ignored by console client)
    public AnimationData? Animation { get; set; }

    // Runtime flags
    public bool IsWaitCard { get; set; } = false;
    public bool IsGhost { get; set; } = false;  // Ghost copy (temporary card)
    public bool TargetSelf { get; set; } = false;  // Redirect target to self

    public Card Clone()
    {
        return new Card
        {
            Id = Id,
            Name = Name,
            Suit = Suit,
            Type = Type,
            Rarity = Rarity,
            Description = Description,
            ImmediateEffect = ImmediateEffect,
            GrantsStatusTo = GrantsStatusTo,
            Animation = Animation,
            IsWaitCard = IsWaitCard,
            IsGhost = IsGhost,
            TargetSelf = TargetSelf
        };
    }
}

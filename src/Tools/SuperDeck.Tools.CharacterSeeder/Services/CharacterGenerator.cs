using SuperDeck.Core.Models;
using SuperDeck.Core.Models.Enums;

namespace SuperDeck.Tools.CharacterSeeder.Services;

public class CharacterGenerator
{
    private readonly Random _random = new();

    public Character Generate(string name, Suit suit, int level, List<string> deckCardIds)
    {
        var profile = GetStatProfile(suit);
        var totalStatPoints = (level * 2) + 5;

        var (attack, defense, speed) = DistributeStats(totalStatPoints, profile);
        var mmr = CalculateMMR(level);
        var (wins, losses) = GeneratePlausibleRecord(level, mmr);

        return new Character
        {
            Id = $"ghost_{name.ToLowerInvariant()}_{suit.ToString().ToLowerInvariant()}_lv{level}",
            Name = $"{name} Lv{level}",
            Level = level,
            XP = 0,
            Attack = attack,
            Defense = defense,
            Speed = speed,
            DeckCardIds = deckCardIds,
            Wins = wins,
            Losses = losses,
            MMR = mmr,
            IsGhost = true,
            IsPublished = false,
            OwnerPlayerId = null,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };
    }

    private StatProfile GetStatProfile(Suit suit)
    {
        return suit switch
        {
            Suit.Berserker => new StatProfile(0.50, 0.10, 0.40),    // Aggressive
            Suit.Fire => new StatProfile(0.50, 0.10, 0.40),         // Aggressive
            Suit.Military => new StatProfile(0.50, 0.10, 0.40),     // Aggressive
            Suit.Magic => new StatProfile(0.20, 0.50, 0.30),        // Defensive
            Suit.Mental => new StatProfile(0.20, 0.50, 0.30),       // Defensive
            Suit.Speedster => new StatProfile(0.30, 0.10, 0.60),    // Speedster
            Suit.Electricity => new StatProfile(0.30, 0.10, 0.60),  // Speedster
            _ => new StatProfile(0.35, 0.30, 0.35)                  // Balanced
        };
    }

    private (int Attack, int Defense, int Speed) DistributeStats(int totalPoints, StatProfile profile)
    {
        var attack = (int)Math.Round(totalPoints * profile.AttackRatio);
        var defense = (int)Math.Round(totalPoints * profile.DefenseRatio);
        var speed = (int)Math.Round(totalPoints * profile.SpeedRatio);

        // Ensure we use all points by adding remainder to highest ratio stat
        var remainder = totalPoints - (attack + defense + speed);
        if (remainder > 0)
        {
            if (profile.SpeedRatio >= profile.AttackRatio && profile.SpeedRatio >= profile.DefenseRatio)
                speed += remainder;
            else if (profile.AttackRatio >= profile.DefenseRatio)
                attack += remainder;
            else
                defense += remainder;
        }

        return (attack, defense, speed);
    }

    private int CalculateMMR(int level)
    {
        // MMR = 700 + (level * 80) + random(-30, 30)
        // Level 1: ~780, Level 5: ~1100, Level 10: ~1500
        var baseMMR = 700 + (level * 80);
        var variance = _random.Next(-30, 31);
        return baseMMR + variance;
    }

    private (int Wins, int Losses) GeneratePlausibleRecord(int level, int mmr)
    {
        // Generate a plausible win/loss record based on MMR
        // Higher MMR = higher win rate
        var totalGames = level * 10 + _random.Next(5, 20);
        var winRate = Math.Min(0.80, Math.Max(0.30, (mmr - 600) / 1000.0));
        var wins = (int)Math.Round(totalGames * winRate);
        var losses = totalGames - wins;
        return (wins, losses);
    }

    private record StatProfile(double AttackRatio, double DefenseRatio, double SpeedRatio);
}

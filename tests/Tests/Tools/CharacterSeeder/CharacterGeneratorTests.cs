using FluentAssertions;
using SuperDeck.Core.Models.Enums;
using SuperDeck.Tools.CharacterSeeder.Services;

namespace SuperDeck.Tests.Tools.CharacterSeeder;

public class CharacterGeneratorTests
{
    private readonly CharacterGenerator _generator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Generate_ShouldCreateCharacterWithCorrectLevel(int level)
    {
        var deck = new List<string> { "card1", "card2" };
        var character = _generator.Generate("Test", Suit.Fire, level, deck);

        character.Level.Should().Be(level);
    }

    [Fact]
    public void Generate_ShouldCreateCharacterWithCorrectId()
    {
        var deck = new List<string> { "card1" };
        var character = _generator.Generate("Blaze", Suit.Fire, 5, deck);

        character.Id.Should().Be("ghost_blaze_fire_lv5");
    }

    [Fact]
    public void Generate_ShouldSetIsGhostToTrue()
    {
        var deck = new List<string> { "card1" };
        var character = _generator.Generate("Test", Suit.Berserker, 1, deck);

        character.IsGhost.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Generate_ShouldDistributeStatPointsReasonably(int level)
    {
        var deck = new List<string> { "card1" };
        var character = _generator.Generate("Test", Suit.Fire, level, deck);

        var totalStats = character.Attack + character.Defense + character.Speed;
        // Total stat points = (level * 2) + 5, but rounding may cause +/-1 variance
        var expectedBase = (level * 2) + 5;
        totalStats.Should().BeInRange(expectedBase - 1, expectedBase + 1);
    }

    [Theory]
    [InlineData(1, 750, 810)]   // 700 + 80 +/- 30
    [InlineData(5, 1070, 1130)] // 700 + 400 +/- 30
    [InlineData(10, 1470, 1530)] // 700 + 800 +/- 30
    public void Generate_ShouldCalculateMMRWithinExpectedRange(int level, int minMMR, int maxMMR)
    {
        var deck = new List<string> { "card1" };

        // Run multiple times to account for randomness
        for (var i = 0; i < 100; i++)
        {
            var character = _generator.Generate("Test", Suit.Fire, level, deck);
            character.MMR.Should().BeInRange(minMMR, maxMMR);
        }
    }

    [Theory]
    [InlineData(Suit.Berserker)]
    [InlineData(Suit.Fire)]
    [InlineData(Suit.Military)]
    public void Generate_AggressiveSuits_ShouldHaveHigherAttack(Suit suit)
    {
        var deck = new List<string> { "card1" };
        var character = _generator.Generate("Test", suit, 10, deck);

        // Aggressive profile: 50% attack, 10% defense, 40% speed
        // With 25 total points: ~12-13 attack, ~2-3 defense, ~10 speed
        character.Attack.Should().BeGreaterThan(character.Defense);
    }

    [Theory]
    [InlineData(Suit.Magic)]
    [InlineData(Suit.Mental)]
    public void Generate_DefensiveSuits_ShouldHaveHigherDefense(Suit suit)
    {
        var deck = new List<string> { "card1" };
        var character = _generator.Generate("Test", suit, 10, deck);

        // Defensive profile: 20% attack, 50% defense, 30% speed
        character.Defense.Should().BeGreaterThan(character.Attack);
    }

    [Theory]
    [InlineData(Suit.Speedster)]
    [InlineData(Suit.Electricity)]
    public void Generate_SpeedsterSuits_ShouldHaveHighestSpeed(Suit suit)
    {
        var deck = new List<string> { "card1" };
        var character = _generator.Generate("Test", suit, 10, deck);

        // Speedster profile: 30% attack, 10% defense, 60% speed
        character.Speed.Should().BeGreaterThan(character.Attack);
        character.Speed.Should().BeGreaterThan(character.Defense);
    }

    [Fact]
    public void Generate_ShouldAssignDeckCardIds()
    {
        var deck = new List<string> { "card1", "card2", "card3" };
        var character = _generator.Generate("Test", Suit.Fire, 1, deck);

        character.DeckCardIds.Should().BeEquivalentTo(deck);
    }

    [Fact]
    public void Generate_ShouldHavePlausibleWinLossRecord()
    {
        var deck = new List<string> { "card1" };
        var character = _generator.Generate("Test", Suit.Fire, 5, deck);

        character.Wins.Should().BeGreaterOrEqualTo(0);
        character.Losses.Should().BeGreaterOrEqualTo(0);
        (character.Wins + character.Losses).Should().BeGreaterThan(0);
    }

    [Fact]
    public void Generate_ShouldSetCorrectName()
    {
        var deck = new List<string> { "card1" };
        var character = _generator.Generate("Blaze", Suit.Fire, 5, deck);

        character.Name.Should().Be("Blaze Lv5");
    }
}

using FluentAssertions;
using SuperDeck.Core.Models.Enums;
using SuperDeck.Tools.CharacterSeeder.Services;

namespace SuperDeck.Tests.Tools.CharacterSeeder;

public class DeckBuilderTests
{
    private readonly string _testCardsPath;

    public DeckBuilderTests()
    {
        // Find the ServerCards directory relative to test execution
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var solutionRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        _testCardsPath = Path.Combine(solutionRoot, "src", "Server", "Data", "ServerCards");
    }

    [Fact]
    public async Task LoadCardsAsync_ShouldLoadCardsFromDirectory()
    {
        if (!Directory.Exists(_testCardsPath))
        {
            return; // Skip if cards not found in test environment
        }

        var builder = new DeckBuilder();
        await builder.LoadCardsAsync(_testCardsPath);

        // Building a deck should work without throwing
        var deck = builder.BuildDeck(Suit.Fire, 1);
        deck.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 10)]
    [InlineData(3, 10)]
    [InlineData(4, 12)]
    [InlineData(5, 12)]
    [InlineData(6, 12)]
    [InlineData(7, 13)]
    [InlineData(8, 13)]
    [InlineData(9, 13)]
    [InlineData(10, 15)]
    public async Task BuildDeck_ShouldReturnCorrectDeckSize(int level, int expectedSize)
    {
        if (!Directory.Exists(_testCardsPath))
        {
            return;
        }

        var builder = new DeckBuilder();
        await builder.LoadCardsAsync(_testCardsPath);

        var deck = builder.BuildDeck(Suit.Fire, level);

        deck.Should().HaveCount(expectedSize);
    }

    [Fact]
    public async Task BuildDeck_ShouldOnlyIncludeSuitAndBasicCards()
    {
        if (!Directory.Exists(_testCardsPath))
        {
            return;
        }

        var builder = new DeckBuilder();
        await builder.LoadCardsAsync(_testCardsPath);

        var deck = builder.BuildDeck(Suit.Fire, 5);

        // All cards should be from Fire or Basic suits
        deck.All(id => id.StartsWith("fire_") || id.StartsWith("basic_")).Should().BeTrue();
    }

    [Theory]
    [InlineData(Suit.Berserker)]
    [InlineData(Suit.Electricity)]
    [InlineData(Suit.Espionage)]
    [InlineData(Suit.Fire)]
    [InlineData(Suit.Magic)]
    [InlineData(Suit.MartialArts)]
    [InlineData(Suit.Mental)]
    [InlineData(Suit.Military)]
    [InlineData(Suit.Money)]
    [InlineData(Suit.Showbiz)]
    [InlineData(Suit.Speedster)]
    public async Task BuildDeck_ShouldWorkForAllSuits(Suit suit)
    {
        if (!Directory.Exists(_testCardsPath))
        {
            return;
        }

        var builder = new DeckBuilder();
        await builder.LoadCardsAsync(_testCardsPath);

        var deck = builder.BuildDeck(suit, 5);

        deck.Should().NotBeEmpty();
    }

    [Fact]
    public void BuildDeck_WithoutLoadingCards_ShouldThrow()
    {
        var builder = new DeckBuilder();

        var action = () => builder.BuildDeck(Suit.Fire, 1);

        action.Should().Throw<InvalidOperationException>();
    }
}

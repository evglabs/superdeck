using System.Text.Json;
using SuperDeck.Core.Models;
using SuperDeck.Core.Models.Enums;

namespace SuperDeck.Tools.CharacterSeeder.Services;

public class DeckBuilder
{
    private readonly List<Card> _cards = new();
    private readonly Random _random = new();

    public async Task LoadCardsAsync(string cardsPath)
    {
        _cards.Clear();

        var jsonFiles = Directory.GetFiles(cardsPath, "*.json");
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        foreach (var file in jsonFiles)
        {
            var json = await File.ReadAllTextAsync(file);
            var card = JsonSerializer.Deserialize<Card>(json, options);
            if (card != null)
            {
                _cards.Add(card);
            }
        }
    }

    public List<string> BuildDeck(Suit[] suits, int level)
    {
        var deckConfig = GetDeckConfig(level);
        var suitSet = new HashSet<Suit>(suits) { Suit.Basic };
        var availableCards = _cards
            .Where(c => suitSet.Contains(c.Suit))
            .ToList();

        if (availableCards.Count == 0)
        {
            var suitNames = string.Join(", ", suits);
            throw new InvalidOperationException($"No cards found for suits {suitNames} or Basic");
        }

        var deck = new List<string>();
        var cardsByRarity = availableCards.GroupBy(c => c.Rarity).ToDictionary(g => g.Key, g => g.ToList());

        // Fill deck according to rarity distribution
        foreach (var (rarity, percentage) in deckConfig.RarityDistribution)
        {
            var targetCount = (int)Math.Ceiling(deckConfig.DeckSize * percentage);
            if (!cardsByRarity.TryGetValue(rarity, out var rarityCards) || rarityCards.Count == 0)
            {
                continue;
            }

            for (var i = 0; i < targetCount && deck.Count < deckConfig.DeckSize; i++)
            {
                var card = rarityCards[_random.Next(rarityCards.Count)];
                deck.Add(card.Id);
            }
        }

        // Fill remaining slots with common cards
        var commonCards = cardsByRarity.GetValueOrDefault(Rarity.Common, availableCards);
        while (deck.Count < deckConfig.DeckSize)
        {
            var card = commonCards[_random.Next(commonCards.Count)];
            deck.Add(card.Id);
        }

        return deck;
    }

    private DeckConfig GetDeckConfig(int level)
    {
        return level switch
        {
            >= 1 and <= 3 => new DeckConfig(10, new Dictionary<Rarity, double>
            {
                { Rarity.Common, 0.80 },
                { Rarity.Uncommon, 0.20 }
            }),
            >= 4 and <= 6 => new DeckConfig(12, new Dictionary<Rarity, double>
            {
                { Rarity.Common, 0.50 },
                { Rarity.Uncommon, 0.35 },
                { Rarity.Rare, 0.15 }
            }),
            >= 7 and <= 9 => new DeckConfig(13, new Dictionary<Rarity, double>
            {
                { Rarity.Common, 0.30 },
                { Rarity.Uncommon, 0.35 },
                { Rarity.Rare, 0.25 },
                { Rarity.Epic, 0.10 }
            }),
            10 => new DeckConfig(15, new Dictionary<Rarity, double>
            {
                { Rarity.Common, 0.20 },
                { Rarity.Uncommon, 0.30 },
                { Rarity.Rare, 0.30 },
                { Rarity.Epic, 0.15 },
                { Rarity.Legendary, 0.05 }
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(level), "Level must be between 1 and 10")
        };
    }

    private record DeckConfig(int DeckSize, Dictionary<Rarity, double> RarityDistribution);
}

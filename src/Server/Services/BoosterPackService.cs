using SuperDeck.Core.Data.Repositories;
using SuperDeck.Core.Models;
using SuperDeck.Core.Models.Enums;
using SuperDeck.Core.Settings;

namespace SuperDeck.Server.Services;

public class BoosterPackService
{
    private readonly CardService _cardService;
    private readonly ICharacterRepository _characterRepository;
    private readonly GameSettings _settings;
    private readonly Random _rng = new();

    public BoosterPackService(CardService cardService, ICharacterRepository characterRepository, GameSettings settings)
    {
        _cardService = cardService;
        _characterRepository = characterRepository;
        _settings = settings;
    }

    public BoosterPack GeneratePack(Character character, int? cardCount = null)
    {
        int packSize = cardCount ?? _settings.CardPack.BoosterPackSize;

        var pack = new BoosterPack
        {
            Id = Guid.NewGuid().ToString(),
            CharacterId = character.Id
        };

        var allCards = _cardService.GetAllCards().ToList();
        if (!allCards.Any())
        {
            return pack;
        }

        // Calculate suit weights based on deck composition
        var suitWeights = CalculateSuitWeights(character.DeckCardIds, allCards);

        for (int i = 0; i < packSize; i++)
        {
            // Roll rarity
            var rarity = RollRarity();

            // Roll suit based on weights
            var suit = RollSuit(suitWeights);

            // Select a card of that rarity and suit
            var candidates = allCards
                .Where(c => c.Rarity == rarity && c.Suit == suit)
                .ToList();

            // Fallback to any card of that rarity if no matching suit
            if (!candidates.Any())
            {
                candidates = allCards.Where(c => c.Rarity == rarity).ToList();
            }

            // Fallback to any card if still empty
            if (!candidates.Any())
            {
                candidates = allCards;
            }

            var selectedCard = candidates[_rng.Next(candidates.Count)];
            pack.Cards.Add(selectedCard.Clone());
        }

        return pack;
    }

    public async Task<Character?> SelectCardsAsync(string characterId, string packId, List<int> selectedIndices, List<string>? cardsToRemove = null)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null) return null;

        // This is a simplified implementation
        // In a real implementation, we'd validate the pack still exists and indices are valid

        // Process card removals (sacrifice a pick to remove a card)
        if (cardsToRemove != null)
        {
            foreach (var cardId in cardsToRemove)
            {
                character.DeckCardIds.Remove(cardId);
            }
        }

        // Add selected cards (would come from actual pack in full implementation)
        // For now, this is a placeholder since we don't persist packs

        character.LastModified = DateTime.UtcNow;
        return await _characterRepository.UpdateAsync(character);
    }

    private Rarity RollRarity()
    {
        int roll = _rng.Next(_settings.CardPack.RarityRollMax);

        if (roll < _settings.RarityWeights.CommonThreshold) return Rarity.Common;
        if (roll < _settings.RarityWeights.UncommonThreshold) return Rarity.Uncommon;
        if (roll < _settings.RarityWeights.RareThreshold) return Rarity.Rare;
        return Rarity.Legendary;
    }

    private Suit RollSuit(Dictionary<Suit, double> weights)
    {
        double totalWeight = weights.Values.Sum();
        double roll = _rng.NextDouble() * totalWeight;

        double cumulative = 0;
        foreach (var (suit, weight) in weights)
        {
            cumulative += weight;
            if (roll <= cumulative)
            {
                return suit;
            }
        }

        // Fallback
        return Suit.Basic;
    }

    private Dictionary<Suit, double> CalculateSuitWeights(List<string> deckCardIds, List<Card> allCards)
    {
        var weights = new Dictionary<Suit, double>();

        // Count cards of each suit in deck
        var deckCards = deckCardIds
            .Select(id => allCards.FirstOrDefault(c => c.Id == id))
            .Where(c => c != null)
            .ToList();

        var suitCounts = new Dictionary<Suit, int>();
        foreach (var card in deckCards)
        {
            if (!suitCounts.ContainsKey(card!.Suit))
            {
                suitCounts[card.Suit] = 0;
            }
            suitCounts[card.Suit]++;
        }

        // Calculate final weights
        foreach (var suit in Enum.GetValues<Suit>())
        {
            // Get base weight from settings dictionary, default to 10 if not found
            double baseWeight = _settings.SuitWeights.GetValueOrDefault(suit.ToString(), 10);
            int ownedCards = suitCounts.GetValueOrDefault(suit, 0);

            // Bonus per owned card of this suit
            double bonusWeight = ownedCards * _settings.CardPack.SuitBonusPerOwnedCard;

            weights[suit] = baseWeight + bonusWeight;
        }

        return weights;
    }
}

public class BoosterPack
{
    public string Id { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public List<Card> Cards { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

using System.Text.Json;
using SuperDeck.Core.Data.Repositories;
using SuperDeck.Core.Models;
using SuperDeck.Core.Models.Enums;

namespace SuperDeck.Server.Services;

public class AIBehaviorService
{
    private readonly IAIProfileRepository _profileRepository;

    public AIBehaviorService(IAIProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<BehaviorRules> GetBehaviorRulesAsync(string profileId)
    {
        var profile = await _profileRepository.GetByIdAsync(profileId);
        if (profile == null)
        {
            profile = await _profileRepository.GetDefaultAsync();
        }

        if (profile == null)
        {
            return new BehaviorRules();
        }

        try
        {
            return JsonSerializer.Deserialize<BehaviorRules>(profile.BehaviorRulesJson)
                ?? new BehaviorRules();
        }
        catch
        {
            return new BehaviorRules();
        }
    }

    public List<Card> SelectCardsForQueue(
        List<Card> availableCards,
        int slotsToFill,
        BehaviorRules rules,
        Character self,
        Character opponent,
        Random rng)
    {
        var selectedCards = new List<Card>();
        var remainingCards = new List<Card>(availableCards);

        double hpRatio = (double)self.CurrentHP / self.MaxHP;
        double opponentHpRatio = (double)opponent.CurrentHP / opponent.MaxHP;
        bool hasHPAdvantage = hpRatio > opponentHpRatio;

        for (int i = 0; i < slotsToFill && remainingCards.Count > 0; i++)
        {
            var scoredCards = remainingCards
                .Select(card => new
                {
                    Card = card,
                    Score = CalculateCardScore(card, rules, hpRatio, hasHPAdvantage, rng)
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            var selectedCard = scoredCards.First().Card;
            selectedCards.Add(selectedCard);
            remainingCards.Remove(selectedCard);
        }

        return selectedCards;
    }

    private double CalculateCardScore(
        Card card,
        BehaviorRules rules,
        double hpRatio,
        bool hasHPAdvantage,
        Random rng)
    {
        double score = 1.0;

        // Base score from card type preference
        var cardTypeName = card.Type.ToString();
        if (rules.CardTypePreferences.TryGetValue(cardTypeName, out var typePreference))
        {
            score *= typePreference;
        }

        // Defensive behavior when low HP
        if (hpRatio <= rules.DefensiveThreshold && card.Type == CardType.Defense)
        {
            score *= 2.0;
        }

        // Prioritize attacks when HP advantage
        if (rules.PriorityAttackWhenHPAdvantage && hasHPAdvantage && card.Type == CardType.Attack)
        {
            score *= 1.5;
        }

        // Suit preference bonus
        var suitName = card.Suit.ToString();
        if (rules.SuitPreferences.TryGetValue(suitName, out var suitPreference))
        {
            score *= suitPreference;
        }

        // Add randomness
        double randomFactor = 1.0 + (rng.NextDouble() * 2 - 1) * rules.RandomnessFactor;
        score *= randomFactor;

        return score;
    }
}

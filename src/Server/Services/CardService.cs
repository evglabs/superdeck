using System.Text.Json;
using System.Text.Json.Serialization;
using SuperDeck.Core.Models;
using SuperDeck.Core.Models.Enums;
using SuperDeck.Core.Scripting;
using SuperDeck.Core.Settings;

namespace SuperDeck.Server.Services;

public class CardService
{
    private readonly Dictionary<string, Card> _cardLibrary = new();
    private readonly ScriptCompiler _scriptCompiler;
    private readonly GameSettings _settings;
    private readonly string _cardsPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public CardService(ScriptCompiler compiler, IConfiguration config, GameSettings settings)
    {
        _scriptCompiler = compiler;
        _settings = settings;
        _cardsPath = config["CardLibraryPath"] ?? "Data/ServerCards";

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task LoadCardsAsync()
    {
        var cardsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _cardsPath);

        if (!Directory.Exists(cardsPath))
        {
            cardsPath = _cardsPath;
        }

        if (!Directory.Exists(cardsPath))
        {
            Console.WriteLine($"Warning: Card library path not found: {cardsPath}");
            // Create default cards in memory
            CreateDefaultCards();
            return;
        }

        var jsonFiles = Directory.GetFiles(cardsPath, "*.json", SearchOption.AllDirectories);

        foreach (var file in jsonFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var card = JsonSerializer.Deserialize<Card>(json, _jsonOptions);

                if (card != null && !string.IsNullOrEmpty(card.Id))
                {
                    // Note: Scripts are compiled lazily on first use for faster startup
                    _cardLibrary[card.Id] = card;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading card from {file}: {ex.Message}");
            }
        }

        Console.WriteLine($"Loaded {_cardLibrary.Count} cards from library");

        // If no cards were loaded, create defaults
        if (_cardLibrary.Count == 0)
        {
            CreateDefaultCards();
        }
    }

    private void CreateDefaultCards()
    {
        // Create a basic set of starter cards for each suit
        var basicCards = new List<Card>
        {
            // Basic attacks
            new Card { Id = "punch_basic", Name = "Punch", Suit = Suit.Basic, Type = CardType.Attack, Rarity = Rarity.Common,
                Description = "Deal 10 damage",
                ImmediateEffect = new ImmediateEffect { Target = TargetType.Opponent, Script = "Opponent.CurrentHP -= 10; Log(PlayerDisplayName + \" punches for 10 damage!\");" }
            },
            new Card { Id = "kick_basic", Name = "Kick", Suit = Suit.Basic, Type = CardType.Attack, Rarity = Rarity.Common,
                Description = "Deal 12 damage",
                ImmediateEffect = new ImmediateEffect { Target = TargetType.Opponent, Script = "Opponent.CurrentHP -= 12; Log(PlayerDisplayName + \" kicks for 12 damage!\");" }
            },
            new Card { Id = "block_basic", Name = "Block", Suit = Suit.Basic, Type = CardType.Defense, Rarity = Rarity.Common,
                Description = "Reduce damage taken by 5 for 1 turn",
                GrantsStatusTo = new GrantsStatus { Target = TargetType.Self, Status = new StatusDefinition {
                    Name = "Blocking", Duration = 1, Hooks = new Dictionary<string, string> {
                        { "OnTakeDamage", "Amount = Math.Max(0, Amount - 5); Log(\"Block reduces damage by 5!\");" }
                    }
                }}
            },

            // Fire cards
            new Card { Id = "fireball", Name = "Fireball", Suit = Suit.Fire, Type = CardType.Attack, Rarity = Rarity.Rare,
                Description = "Deal 12 damage and apply Burn for 3 turns (3 damage per turn)",
                ImmediateEffect = new ImmediateEffect { Target = TargetType.Opponent, Script = "Opponent.CurrentHP -= 12; Log(PlayerDisplayName + \" hurls a fireball for 12 damage!\");" },
                GrantsStatusTo = new GrantsStatus { Target = TargetType.Opponent, Status = new StatusDefinition {
                    Name = "Burn", Duration = 3, Hooks = new Dictionary<string, string> {
                        { "OnTurnStart", "Player.CurrentHP -= 3; Log(PlayerDisplayName + \" burns for 3 damage!\");" }
                    }
                }}
            },
            new Card { Id = "flame_punch", Name = "Flame Punch", Suit = Suit.Fire, Type = CardType.Attack, Rarity = Rarity.Uncommon,
                Description = "Deal 15 damage",
                ImmediateEffect = new ImmediateEffect { Target = TargetType.Opponent, Script = "Opponent.CurrentHP -= 15; Log(PlayerDisplayName + \" flame punches for 15 damage!\");" }
            },
            new Card { Id = "ignite", Name = "Ignite", Suit = Suit.Fire, Type = CardType.Attack, Rarity = Rarity.Common,
                Description = "Deal 8 damage",
                ImmediateEffect = new ImmediateEffect { Target = TargetType.Opponent, Script = "Opponent.CurrentHP -= 8; Log(PlayerDisplayName + \" ignites the opponent for 8 damage!\");" }
            },

            // Martial Arts cards
            new Card { Id = "power_strike", Name = "Power Strike", Suit = Suit.MartialArts, Type = CardType.Attack, Rarity = Rarity.Uncommon,
                Description = "Deal 15 damage and gain +5 Attack for 2 turns",
                ImmediateEffect = new ImmediateEffect { Target = TargetType.Opponent, Script = "Opponent.CurrentHP -= 15; Log(PlayerDisplayName + \" delivers a power strike for 15 damage!\");" },
                GrantsStatusTo = new GrantsStatus { Target = TargetType.Self, Status = new StatusDefinition {
                    Name = "Empowered", Duration = 2, Hooks = new Dictionary<string, string> {
                        { "OnCalculateAttack", "Amount += 5; Log(\"Empowered adds +5 Attack!\");" }
                    }
                }}
            },
            new Card { Id = "shield_block", Name = "Shield Block", Suit = Suit.MartialArts, Type = CardType.Defense, Rarity = Rarity.Uncommon,
                Description = "Reduce all damage taken by 50% for 2 turns",
                GrantsStatusTo = new GrantsStatus { Target = TargetType.Self, Status = new StatusDefinition {
                    Name = "Shielded", Duration = 2, Hooks = new Dictionary<string, string> {
                        { "OnTakeDamage", "Amount = (int)(Amount * 0.5); Log(\"Shield absorbs half the damage!\");" }
                    }
                }}
            },
            new Card { Id = "swift_strike", Name = "Swift Strike", Suit = Suit.MartialArts, Type = CardType.Attack, Rarity = Rarity.Common,
                Description = "Deal 8 damage and gain +2 Speed for 1 turn",
                ImmediateEffect = new ImmediateEffect { Target = TargetType.Opponent, Script = "Opponent.CurrentHP -= 8; Log(PlayerDisplayName + \" swift strikes for 8 damage!\");" },
                GrantsStatusTo = new GrantsStatus { Target = TargetType.Self, Status = new StatusDefinition {
                    Name = "Swift", Duration = 1, Hooks = new Dictionary<string, string> {
                        { "OnCalculateSpeed", "Amount += 2;" }
                    }
                }}
            },

            // Magic cards
            new Card { Id = "arcane_blast", Name = "Arcane Blast", Suit = Suit.Magic, Type = CardType.Attack, Rarity = Rarity.Common,
                Description = "Deal 10 damage",
                ImmediateEffect = new ImmediateEffect { Target = TargetType.Opponent, Script = "Opponent.CurrentHP -= 10; Log(PlayerDisplayName + \" blasts with arcane energy for 10 damage!\");" }
            },
            new Card { Id = "heal", Name = "Heal", Suit = Suit.Magic, Type = CardType.Buff, Rarity = Rarity.Uncommon,
                Description = "Restore 15 HP",
                ImmediateEffect = new ImmediateEffect { Target = TargetType.Self, Script = "Heal(Player, 15);" }
            },
            new Card { Id = "mana_shield", Name = "Mana Shield", Suit = Suit.Magic, Type = CardType.Defense, Rarity = Rarity.Rare,
                Description = "Block the next 20 damage taken",
                GrantsStatusTo = new GrantsStatus { Target = TargetType.Self, Status = new StatusDefinition {
                    Name = "Mana Shield", Duration = 3, Hooks = new Dictionary<string, string> {
                        { "OnTakeDamage", "int blocked = Math.Min(Amount, 20); Amount -= blocked; Log(\"Mana Shield blocks \" + blocked + \" damage!\");" }
                    }
                }}
            },

            // Electricity cards
            new Card { Id = "lightning_bolt", Name = "Lightning Bolt", Suit = Suit.Electricity, Type = CardType.Attack, Rarity = Rarity.Uncommon,
                Description = "Deal 14 damage",
                ImmediateEffect = new ImmediateEffect { Target = TargetType.Opponent, Script = "Opponent.CurrentHP -= 14; Log(PlayerDisplayName + \" zaps with lightning for 14 damage!\");" }
            },
            new Card { Id = "shock", Name = "Shock", Suit = Suit.Electricity, Type = CardType.Attack, Rarity = Rarity.Common,
                Description = "Deal 8 damage and reduce opponent's Speed by 2 for 1 turn",
                ImmediateEffect = new ImmediateEffect { Target = TargetType.Opponent, Script = "Opponent.CurrentHP -= 8; Log(PlayerDisplayName + \" shocks for 8 damage!\");" },
                GrantsStatusTo = new GrantsStatus { Target = TargetType.Opponent, Status = new StatusDefinition {
                    Name = "Shocked", Duration = 1, Hooks = new Dictionary<string, string> {
                        { "OnCalculateSpeed", "Amount = Math.Max(1, Amount - 2); Log(\"Shock slows speed!\");" }
                    }
                }}
            },

            // Mental cards
            new Card { Id = "mind_blast", Name = "Mind Blast", Suit = Suit.Mental, Type = CardType.Attack, Rarity = Rarity.Uncommon,
                Description = "Deal 11 damage",
                ImmediateEffect = new ImmediateEffect { Target = TargetType.Opponent, Script = "Opponent.CurrentHP -= 11; Log(PlayerDisplayName + \" attacks mentally for 11 damage!\");" }
            },
            new Card { Id = "confuse", Name = "Confuse", Suit = Suit.Mental, Type = CardType.Debuff, Rarity = Rarity.Rare,
                Description = "Shuffle opponent's queue",
                ImmediateEffect = new ImmediateEffect { Target = TargetType.Opponent, Script = "Shuffle(OpponentQueue); Log(PlayerDisplayName + \" confuses the opponent!\");" }
            }
        };

        foreach (var card in basicCards)
        {
            _cardLibrary[card.Id] = card;
        }

        Console.WriteLine($"Created {_cardLibrary.Count} default cards");
    }

    public Card? GetCard(string cardId) => _cardLibrary.GetValueOrDefault(cardId);

    public IEnumerable<Card> GetAllCards() => _cardLibrary.Values;

    public IEnumerable<Card> GetCardsBySuit(Suit suit) =>
        _cardLibrary.Values.Where(c => c.Suit == suit);

    public IEnumerable<Card> GetCardsByRarity(Rarity rarity) =>
        _cardLibrary.Values.Where(c => c.Rarity == rarity);

    public IEnumerable<Card> GetBasicStarterDeck()
    {
        var punch = _cardLibrary.GetValueOrDefault("basic_punch");
        var block = _cardLibrary.GetValueOrDefault("basic_block");

        var deck = new List<Card>();
        for (int i = 0; i < _settings.CardPack.StarterDeckPunchCount; i++)
        {
            if (punch != null) deck.Add(punch);
        }
        for (int i = 0; i < _settings.CardPack.StarterDeckBlockCount; i++)
        {
            if (block != null) deck.Add(block);
        }

        return deck;
    }

    public IEnumerable<Card> GetStarterPackCards(Suit suit)
    {
        var suitCards = GetCardsBySuit(suit).ToList();

        if (!suitCards.Any())
            return Enumerable.Empty<Card>();

        var random = new Random();
        var pack = new List<Card>();

        // Rarity weights from settings
        var rarityWeights = new Dictionary<Rarity, int>
        {
            { Rarity.Common, _settings.RarityWeights.StarterCommonWeight },
            { Rarity.Uncommon, _settings.RarityWeights.StarterUncommonWeight },
            { Rarity.Rare, _settings.RarityWeights.StarterRareWeight },
            { Rarity.Epic, _settings.RarityWeights.StarterEpicWeight },
            { Rarity.Legendary, _settings.RarityWeights.StarterLegendaryWeight }
        };

        // Build weighted card list
        var weightedCards = new List<(Card card, int weight)>();
        foreach (var card in suitCards)
        {
            var weight = rarityWeights.GetValueOrDefault(card.Rarity, 10);
            weightedCards.Add((card, weight));
        }

        var totalWeight = weightedCards.Sum(c => c.weight);

        // Pick cards using weighted random selection
        for (int i = 0; i < _settings.CardPack.StarterPackSize; i++)
        {
            var roll = random.Next(totalWeight);
            var cumulative = 0;
            foreach (var (card, weight) in weightedCards)
            {
                cumulative += weight;
                if (roll < cumulative)
                {
                    pack.Add(card);
                    break;
                }
            }
        }

        return pack.OrderBy(c => c.Rarity).ThenBy(c => c.Name);
    }

    public IEnumerable<Card> GetStarterDeck(Suit suit, int count)
    {
        // Get cards from the specified suit for ghost opponents
        var suitCards = GetCardsBySuit(suit)
            .Where(c => c.Rarity <= Rarity.Uncommon)
            .ToList();

        // Fall back to basic cards if suit has no cards
        if (!suitCards.Any())
        {
            suitCards = GetCardsBySuit(Suit.Basic)
                .Where(c => c.Rarity <= Rarity.Uncommon)
                .ToList();
        }

        var deck = new List<Card>();
        if (suitCards.Any())
        {
            // Fill deck by cycling through available cards
            for (int i = 0; i < count; i++)
            {
                deck.Add(suitCards[i % suitCards.Count]);
            }
        }

        return deck;
    }

    public Card CreateWaitCard()
    {
        return new Card
        {
            Id = "wait_ghost_" + Guid.NewGuid().ToString("N")[..8],
            Name = "Wait",
            Type = CardType.Utility,
            Suit = Suit.Basic,
            Rarity = Rarity.Common,
            Description = "Do nothing.",
            ImmediateEffect = null,
            IsWaitCard = true
        };
    }
}

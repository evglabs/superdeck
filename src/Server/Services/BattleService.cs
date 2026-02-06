using System.Collections.Concurrent;
using System.Text.Json;
using SuperDeck.Core.Data.Repositories;
using SuperDeck.Core.Models;
using SuperDeck.Core.Models.Enums;
using SuperDeck.Core.Models.Events;
using SuperDeck.Core.Scripting;
using SuperDeck.Core.Services;
using SuperDeck.Core.Settings;

namespace SuperDeck.Server.Services;

public class BattleService
{
    private readonly ConcurrentDictionary<string, BattleSession> _activeSessions = new();
    private readonly CardService _cardService;
    private readonly ScriptCompiler _scriptCompiler;
    private readonly HookExecutor _hookExecutor;
    private readonly HookRegistry _hookRegistry;
    private readonly ICharacterRepository _characterRepository;
    private readonly IGhostRepository _ghostRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly CharacterService _characterService;
    private readonly GameSettings _settings;
    private readonly EloCalculator? _eloCalculator;
    private readonly AIBehaviorService? _aiBehaviorService;

    public BattleService(
        CardService cardService,
        ScriptCompiler compiler,
        HookExecutor hookExecutor,
        HookRegistry hookRegistry,
        ICharacterRepository characterRepo,
        IGhostRepository ghostRepo,
        IPlayerRepository playerRepo,
        CharacterService characterService,
        GameSettings settings,
        AIBehaviorService? aiBehaviorService = null)
    {
        _cardService = cardService;
        _scriptCompiler = compiler;
        _hookExecutor = hookExecutor;
        _hookRegistry = hookRegistry;
        _characterRepository = characterRepo;
        _ghostRepository = ghostRepo;
        _playerRepository = playerRepo;
        _characterService = characterService;
        _settings = settings;
        _aiBehaviorService = aiBehaviorService;

        if (settings.MMR.UseEloFormula)
        {
            _eloCalculator = new EloCalculator(settings.MMR.Elo);
        }
    }

    public async Task<BattleState> StartBattleAsync(
        string characterId,
        int? seed = null,
        bool autoBattle = false,
        AutoBattleMode autoBattleMode = AutoBattleMode.Watch,
        string? playerAIProfileId = null)
    {
        var character = await _characterRepository.GetByIdAsync(characterId)
            ?? throw new ArgumentException("Character not found");

        // Find opponent ghost by MMR
        Character opponent;
        string ghostId;

        var ghosts = await _ghostRepository.GetByMMRRangeAsync(
            character.MMR - _settings.Battle.GhostSearchRange,
            character.MMR + _settings.Battle.GhostSearchRange,
            count: _settings.Battle.GhostCandidateCount
        );

        // Filter out player's own ghost to prevent self-matching
        var eligibleGhosts = ghosts.Where(g => g.SourceCharacterId != characterId);
        var ghost = eligibleGhosts.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();

        if (ghost != null)
        {
            opponent = JsonSerializer.Deserialize<Character>(ghost.SerializedCharacterState)
                ?? CreateDefaultOpponent(character.Level);
            // Assign unique battle ID to prevent ID collision with player
            opponent.Id = Guid.NewGuid().ToString();
            ghostId = ghost.Id;
        }
        else
        {
            // Create a default opponent if no ghosts available
            opponent = CreateDefaultOpponent(character.Level);
            ghostId = "default_ghost";
        }

        var rng = seed.HasValue ? new Random(seed.Value) : new Random();

        var battle = new BattleState
        {
            BattleId = Guid.NewGuid().ToString(),
            Player = CloneCharacter(character),
            Opponent = opponent,
            // Copy battle settings from configuration
            BaseQueueSlots = _settings.Battle.BaseQueueSlots,
            MaxQueueSlots = _settings.Battle.MaxQueueSlots,
            StartingHandSize = _settings.Battle.StartingHandSize,
            CardsDrawnPerTurn = _settings.Battle.CardsDrawnPerTurn
        };

        // Initialize HP and battle stats
        battle.Player.InitializeForBattle();
        battle.Opponent.InitializeForBattle();

        // Initialize effective stats (base stats, no buffs yet)
        battle.PlayerEffectiveStats = new EffectiveStats
        {
            Attack = battle.Player.BattleStats.Attack,
            Defense = battle.Player.BattleStats.Defense,
            Speed = battle.Player.BattleStats.Speed
        };
        battle.OpponentEffectiveStats = new EffectiveStats
        {
            Attack = battle.Opponent.BattleStats.Attack,
            Defense = battle.Opponent.BattleStats.Defense,
            Speed = battle.Opponent.BattleStats.Speed
        };

        // Load deck cards
        battle.PlayerDeck = LoadDeck(battle.Player.DeckCardIds, rng);
        battle.OpponentDeck = LoadDeck(battle.Opponent.DeckCardIds, rng);

        // Shuffle decks
        Shuffle(battle.PlayerDeck, rng);
        Shuffle(battle.OpponentDeck, rng);

        // Create session
        var session = new BattleSession
        {
            BattleId = battle.BattleId,
            PlayerId = character.OwnerPlayerId ?? string.Empty,
            CharacterId = characterId,
            GhostId = ghostId,
            AIProfileId = ghost?.AIProfileId ?? "default",
            PlayerMMRAtStart = character.MMR,
            OpponentMMRAtStart = opponent.MMR,
            State = battle,
            Rng = rng,
            AutoBattleEnabled = autoBattle,
            AutoBattleMode = autoBattleMode,
            PlayerAIProfileId = playerAIProfileId
        };

        // Load player AI behavior rules if auto-battle is enabled
        if (autoBattle && _aiBehaviorService != null)
        {
            session.PlayerBehaviorRules = await _aiBehaviorService.GetBehaviorRulesAsync(playerAIProfileId ?? "default");
        }

        _activeSessions[battle.BattleId] = session;

        battle.Log($"Battle started: You vs {battle.Opponent.Name}!");

        // Start first round
        await StartNewRoundAsync(session);

        return battle;
    }

    private Character CreateDefaultOpponent(int level)
    {
        var totalStatPoints = (level - 1) * _settings.Character.StatPointsPerLevel;
        var rng = new Random();
        int atk = 0, def = 0, spd = 0;

        for (int i = 0; i < totalStatPoints; i++)
        {
            switch (rng.Next(3))
            {
                case 0: atk++; break;
                case 1: def++; break;
                case 2: spd++; break;
            }
        }

        var opponent = new Character
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Training Dummy",
            Level = level,
            Attack = atk,
            Defense = def,
            Speed = spd,
            MMR = _settings.MMR.StartingMMR,
            IsGhost = true
        };

        // Give opponent a basic deck
        var starterCards = _cardService.GetStarterDeck(Suit.Basic, _settings.Battle.DefaultOpponentDeckSize);
        opponent.DeckCardIds = starterCards.Select(c => c.Id).ToList();

        return opponent;
    }

    public async Task<BattleState> StartNewRoundAsync(BattleSession session)
    {
        var battle = session.State;
        battle.Round++;
        battle.Phase = BattlePhase.Draw;

        battle.Log($"--- Round {battle.Round} ---");

        // Emit round start event
        battle.EmitEvent(new RoundStartEvent
        {
            RoundNumber = battle.Round,
            PlayerHP = battle.Player.CurrentHP,
            OpponentHP = battle.Opponent.CurrentHP
        });

        // Execute onTurnStart hooks
        _hookExecutor.ExecuteHooks(HookType.OnTurnStart, battle, battle.Player, battle.Opponent, session.Rng);
        _hookExecutor.ExecuteHooks(HookType.OnTurnStart, battle, battle.Opponent, battle.Player, session.Rng);

        // Execute onDrawPhase hooks (can modify hand size)
        _hookExecutor.ExecuteHooks(HookType.OnDrawPhase, battle, battle.Player, battle.Opponent, session.Rng);
        _hookExecutor.ExecuteHooks(HookType.OnDrawPhase, battle, battle.Opponent, battle.Player, session.Rng);

        // Draw cards - starting hand on round 1, normal draw on subsequent rounds
        int cardsToDraw = battle.Round == 1 ? battle.StartingHandSize : battle.CardsDrawnPerTurn;
        DrawCards(battle, battle.Player, cardsToDraw, session.Rng);
        DrawCards(battle, battle.Opponent, cardsToDraw, session.Rng);

        // Reset queue slots
        battle.CurrentPlayerQueueSlots = battle.BaseQueueSlots;
        battle.CurrentOpponentQueueSlots = battle.BaseQueueSlots;

        // Move to queue phase
        battle.Phase = BattlePhase.Queue;

        // Execute onQueuePhaseStart hooks (can modify queue slots)
        _hookExecutor.ExecuteHooks(HookType.OnQueuePhaseStart, battle, battle.Player, battle.Opponent, session.Rng);
        _hookExecutor.ExecuteHooks(HookType.OnQueuePhaseStart, battle, battle.Opponent, battle.Player, session.Rng);

        session.UpdateActivity();
        return battle;
    }

    public async Task<(bool Valid, string? Message, BattleState State)> ProcessActionAsync(
        string battleId,
        PlayerAction action)
    {
        if (!_activeSessions.TryGetValue(battleId, out var session))
        {
            return (false, "Battle not found", null!);
        }

        session.UpdateActivity();
        var battle = session.State;

        if (battle.IsComplete)
        {
            return (false, "Battle has already ended", battle);
        }

        return action.Action.ToLowerInvariant() switch
        {
            "queue_card" => await QueueCardAsync(session, action),
            "confirm_queue" => await ConfirmQueueAsync(session),
            "auto_queue" => await AutoQueueAndConfirmAsync(session),
            "forfeit" => await ForfeitAsync(session),
            _ => (false, $"Unknown action: {action.Action}", battle)
        };
    }

    private async Task<(bool, string?, BattleState)> QueueCardAsync(BattleSession session, PlayerAction action)
    {
        var battle = session.State;

        if (battle.Phase != BattlePhase.Queue)
        {
            return (false, $"Cannot queue card: not in queue phase (current: {battle.Phase})", battle);
        }

        if (!action.HandIndex.HasValue || action.HandIndex < 0 || action.HandIndex >= battle.PlayerHand.Count)
        {
            return (false, $"Invalid hand index: {action.HandIndex}. Hand has {battle.PlayerHand.Count} cards.", battle);
        }

        if (battle.PlayerQueue.Count >= battle.CurrentPlayerQueueSlots)
        {
            return (false, $"Queue is full ({battle.CurrentPlayerQueueSlots} slots)", battle);
        }

        var card = battle.PlayerHand[action.HandIndex.Value];

        // Execute onQueue hooks BEFORE queuing - they can prevent the queue
        if (!_hookExecutor.ExecuteQueueHooks(battle, battle.Player, battle.Opponent, session.Rng, card))
        {
            return (false, "A status effect prevented this card from being queued", battle);
        }

        battle.PlayerHand.RemoveAt(action.HandIndex.Value);
        battle.PlayerQueue.Add(card);

        battle.Log($"You queue {card.Name}");

        return (true, null, battle);
    }

    private async Task<(bool, string?, BattleState)> ConfirmQueueAsync(BattleSession session)
    {
        var battle = session.State;

        if (battle.Phase != BattlePhase.Queue)
        {
            return (false, "Cannot confirm queue: not in queue phase", battle);
        }

        // Pad player queue with Wait cards if needed
        while (battle.PlayerQueue.Count < battle.CurrentPlayerQueueSlots)
        {
            battle.PlayerQueue.Add(_cardService.CreateWaitCard());
        }

        // AI queues cards for opponent
        await AIQueueCardsAsync(session);

        // Execute beforeQueueResolve hooks
        _hookExecutor.ExecuteHooks(HookType.BeforeQueueResolve, battle, battle.Player, battle.Opponent, session.Rng);
        _hookExecutor.ExecuteHooks(HookType.BeforeQueueResolve, battle, battle.Opponent, battle.Player, session.Rng);

        // Start resolution
        await StartResolutionAsync(session);

        return (true, null, battle);
    }

    private async Task StartResolutionAsync(BattleSession session)
    {
        var battle = session.State;
        battle.Phase = BattlePhase.Resolution;
        battle.CurrentQueueIndex = 0;

        // Determine first turn (speed-based probabilistic roll)
        int playerSpeed = _hookExecutor.GetCalculatedStat(
            HookType.OnCalculateSpeed, battle, battle.Player, session.Rng, battle.Player.BattleStats.Speed);
        int opponentSpeed = _hookExecutor.GetCalculatedStat(
            HookType.OnCalculateSpeed, battle, battle.Opponent, session.Rng, battle.Opponent.BattleStats.Speed);

        // Ensure speeds are at least 1 to avoid division by zero
        playerSpeed = Math.Max(1, playerSpeed);
        opponentSpeed = Math.Max(1, opponentSpeed);

        double playerChance = (double)playerSpeed / (playerSpeed + opponentSpeed);
        battle.PlayerGoesFirst = session.Rng.NextDouble() < playerChance;

        battle.Log($"Speed roll: You ({playerSpeed}) vs {battle.Opponent.Name} ({opponentSpeed})");
        battle.Log($"{(battle.PlayerGoesFirst ? "You go" : battle.Opponent.Name + " goes")} first!");

        // Emit speed roll event
        battle.EmitEvent(new SpeedRollEvent
        {
            PlayerSpeed = playerSpeed,
            OpponentSpeed = opponentSpeed,
            PlayerGoesFirst = battle.PlayerGoesFirst,
            PlayerName = "You",
            OpponentName = battle.Opponent.Name
        });

        // Execute all cards
        await ResolveAllCardsAsync(session);
    }

    private async Task ResolveAllCardsAsync(BattleSession session)
    {
        var battle = session.State;
        int maxCards = Math.Max(battle.PlayerQueue.Count, battle.OpponentQueue.Count);

        for (int i = 0; i < maxCards && !battle.IsComplete; i++)
        {
            battle.CurrentQueueIndex = i;

            // Determine order for this pair
            var (firstPlayer, firstQueue, firstOpponent) = battle.PlayerGoesFirst
                ? (battle.Player, battle.PlayerQueue, battle.Opponent)
                : (battle.Opponent, battle.OpponentQueue, battle.Player);

            var (secondPlayer, secondQueue, secondOpponent) = battle.PlayerGoesFirst
                ? (battle.Opponent, battle.OpponentQueue, battle.Player)
                : (battle.Player, battle.PlayerQueue, battle.Opponent);

            // Execute first player's card
            if (i < firstQueue.Count)
            {
                await ExecuteCardAsync(session, firstPlayer, firstQueue[i], firstOpponent);
                if (CheckWinCondition(battle)) break;
            }

            // Execute second player's card
            if (i < secondQueue.Count && !battle.IsComplete)
            {
                await ExecuteCardAsync(session, secondPlayer, secondQueue[i], secondOpponent);
                if (CheckWinCondition(battle)) break;
            }
        }

        if (!battle.IsComplete)
        {
            await CleanupPhaseAsync(session);
        }
    }

    private async Task ExecuteCardAsync(BattleSession session, Character caster, Card card, Character target)
    {
        var battle = session.State;

        var casterDisplayName = GetDisplayName(battle, caster);
        var casterIsPlayer = ReferenceEquals(caster, battle.Player);

        if (card.IsWaitCard)
        {
            battle.Log($"{casterDisplayName} waits...");
            battle.EmitEvent(new MessageEvent
            {
                Message = $"{casterDisplayName} waits...",
                Category = "wait"
            });
            return;
        }

        battle.Log($"{casterDisplayName} plays {card.Name}!");

        // Emit card played event
        battle.EmitEvent(new CardPlayedEvent
        {
            CasterName = casterDisplayName,
            CasterIsPlayer = casterIsPlayer,
            Card = card,
            TargetName = GetDisplayName(battle, target),
            TargetIsPlayer = ReferenceEquals(target, battle.Player)
        });

        // Execute onOpponentPlay hooks (before card resolves)
        _hookExecutor.ExecuteHooks(HookType.OnOpponentPlay, battle, target, caster, session.Rng, card);

        // Execute immediate effect
        if (card.ImmediateEffect?.Script != null)
        {
            try
            {
                var action = await _scriptCompiler.CompileToActionAsync(card.ImmediateEffect.Script);
                var effectTarget = card.ImmediateEffect.Target == TargetType.Self ? caster : target;

                var globals = new ScriptGlobals
                {
                    Player = caster,
                    Opponent = target,
                    Caster = caster,
                    Target = effectTarget,
                    Battle = battle,
                    This = card,
                    Rng = session.Rng,
                    AttackPercentPerPoint = _settings.Character.AttackPercentPerPoint,
                    DefensePercentPerPoint = _settings.Character.DefensePercentPerPoint
                };
                action(globals);
            }
            catch (Exception ex)
            {
                battle.Log($"[Error] Card script failed: {ex.Message}");
            }
        }

        // Grant status effects
        if (card.GrantsStatusTo != null)
        {
            var statusTarget = card.GrantsStatusTo.Target == TargetType.Self ? caster : target;
            var compiledStatus = await _hookRegistry.CompileStatusHooksAsync(card.GrantsStatusTo.Status);
            compiledStatus.SourceCardId = card.Id;

            var targetStatuses = statusTarget == battle.Player ? battle.PlayerStatuses : battle.OpponentStatuses;
            targetStatuses.Add(compiledStatus);
            battle.Log($"{GetDisplayName(battle, statusTarget)} gains {compiledStatus.Name}!");

            // Emit status gained event
            battle.EmitEvent(new StatusGainedEvent
            {
                StatusName = compiledStatus.Name,
                Duration = compiledStatus.Duration,
                IsBuff = compiledStatus.IsBuff,
                TargetIsPlayer = ReferenceEquals(statusTarget, battle.Player),
                TargetName = GetDisplayName(battle, statusTarget),
                SourceCardName = card.Name
            });
        }

        // Execute onPlay hooks
        _hookExecutor.ExecuteHooks(HookType.OnPlay, battle, caster, target, session.Rng, card);

        // Execute onCardResolve hooks (for all players)
        _hookExecutor.ExecuteHooks(HookType.OnCardResolve, battle, battle.Player, battle.Opponent, session.Rng, card);
        _hookExecutor.ExecuteHooks(HookType.OnCardResolve, battle, battle.Opponent, battle.Player, session.Rng, card);

        // Update effective stats after card effects
        UpdateEffectiveStats(battle, session.Rng);
    }

    private async Task CleanupPhaseAsync(BattleSession session)
    {
        var battle = session.State;
        battle.Phase = BattlePhase.Cleanup;

        // Move queued cards to discard
        battle.PlayerDiscard.AddRange(battle.PlayerQueue.Where(c => !c.IsWaitCard));
        battle.PlayerQueue.Clear();
        battle.OpponentDiscard.AddRange(battle.OpponentQueue.Where(c => !c.IsWaitCard));
        battle.OpponentQueue.Clear();

        // Execute onDiscard hooks
        _hookExecutor.ExecuteHooks(HookType.OnDiscard, battle, battle.Player, battle.Opponent, session.Rng);
        _hookExecutor.ExecuteHooks(HookType.OnDiscard, battle, battle.Opponent, battle.Player, session.Rng);

        // Execute onTurnEnd hooks
        _hookExecutor.ExecuteHooks(HookType.OnTurnEnd, battle, battle.Player, battle.Opponent, session.Rng);
        _hookExecutor.ExecuteHooks(HookType.OnTurnEnd, battle, battle.Opponent, battle.Player, session.Rng);

        // Tick status durations and remove expired
        await TickStatusDurationsAsync(battle.PlayerStatuses, battle, battle.Player, battle.Opponent, session.Rng);
        await TickStatusDurationsAsync(battle.OpponentStatuses, battle, battle.Opponent, battle.Player, session.Rng);

        // Update effective stats after status changes
        UpdateEffectiveStats(battle, session.Rng);

        // System damage after configured round
        if (battle.Round >= _settings.Battle.SystemDamageStartRound)
        {
            int roundsPast = battle.Round - _settings.Battle.SystemDamageStartRound;
            int systemDamage = (int)Math.Pow(_settings.Battle.SystemDamageBase, roundsPast)
                + (_settings.Battle.SystemDamagePerRound * roundsPast);
            battle.Player.CurrentHP -= systemDamage;
            battle.Opponent.CurrentHP -= systemDamage;
            battle.Log($"System damage: both players take {systemDamage} damage!");
        }

        // Check win condition
        if (!CheckWinCondition(battle))
        {
            // Start next round
            await StartNewRoundAsync(session);
        }
    }

    private async Task TickStatusDurationsAsync(List<StatusEffect> statuses, BattleState battle, Character player, Character opponent, Random rng)
    {
        var toRemove = new List<StatusEffect>();

        foreach (var status in statuses.ToList())
        {
            // Skip tick on the round the status was applied
            if (status.JustApplied)
            {
                status.JustApplied = false;
                continue;
            }

            status.RemainingDuration--;

            if (status.RemainingDuration <= 0)
            {
                // Execute onBuffExpire hook before removal
                if (status.CompiledHooks.TryGetValue(HookType.OnBuffExpire, out var expireHook))
                {
                    var context = new HookContext
                    {
                        Player = player,
                        Opponent = opponent,
                        Battle = battle,
                        Status = status,
                        ExpiringStatus = status,
                        Rng = rng,
                        Log = msg => battle.Log(msg)
                    };
                    try
                    {
                        expireHook(context);
                        // If hook set PreventExpire, don't remove the status
                        if (context.PreventExpire)
                        {
                            status.RemainingDuration = 1; // Extend by 1 turn
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        battle.Log($"[Error] Status expiration hook failed: {ex.Message}");
                    }
                }
                toRemove.Add(status);
            }
        }

        foreach (var status in toRemove)
        {
            statuses.Remove(status);
            battle.Log($"{status.Name} has expired!");

            // Emit status expired event
            battle.EmitEvent(new StatusExpiredEvent
            {
                StatusName = status.Name,
                WasOnPlayer = statuses == battle.PlayerStatuses,
                TargetName = statuses == battle.PlayerStatuses ? "You" : battle.Opponent.Name
            });
        }
    }

    private bool CheckWinCondition(BattleState battle)
    {
        bool playerDead = battle.Player.CurrentHP <= 0;
        bool opponentDead = battle.Opponent.CurrentHP <= 0;

        if (playerDead && opponentDead)
        {
            // Simultaneous KO - last to act wins
            battle.WinnerId = battle.PlayerGoesFirst ? battle.Opponent.Id : battle.Player.Id;
        }
        else if (playerDead)
        {
            battle.WinnerId = battle.Opponent.Id;
        }
        else if (opponentDead)
        {
            battle.WinnerId = battle.Player.Id;
        }

        if (battle.WinnerId != null)
        {
            battle.Phase = BattlePhase.Ended;
            var winner = battle.WinnerId == battle.Player.Id ? battle.Player : battle.Opponent;
            var playerWon = battle.WinnerId == battle.Player.Id;
            battle.Log($"Battle ended! {GetDisplayName(battle, winner)} wins!");

            // Execute onBattleEnd hooks
            _hookExecutor.ExecuteHooks(HookType.OnBattleEnd, battle, battle.Player, battle.Opponent, new Random());
            _hookExecutor.ExecuteHooks(HookType.OnBattleEnd, battle, battle.Opponent, battle.Player, new Random());

            // Emit battle end event
            string reason = playerDead && opponentDead
                ? "Simultaneous KO - last to act wins"
                : playerDead
                    ? "Player defeated"
                    : "Opponent defeated";

            battle.EmitEvent(new BattleEndEvent
            {
                WinnerId = battle.WinnerId,
                WinnerName = GetDisplayName(battle, winner),
                PlayerWon = playerWon,
                PlayerFinalHP = Math.Max(0, battle.Player.CurrentHP),
                OpponentFinalHP = Math.Max(0, battle.Opponent.CurrentHP),
                OverkillDamage = battle.OverkillDamage,
                Reason = reason
            });
        }

        return battle.IsComplete;
    }

    private async Task AIQueueCardsAsync(BattleSession session)
    {
        var battle = session.State;
        var availableCards = new List<Card>(battle.OpponentHand);
        var slotsToFill = battle.CurrentOpponentQueueSlots;

        List<Card> selectedCards;

        if (_aiBehaviorService != null)
        {
            // Use profile-based AI behavior
            var rules = await _aiBehaviorService.GetBehaviorRulesAsync(session.AIProfileId);
            selectedCards = _aiBehaviorService.SelectCardsForQueue(
                availableCards,
                slotsToFill,
                rules,
                battle.Opponent,
                battle.Player,
                session.Rng);
        }
        else
        {
            // Fallback to simple AI: prioritize attack cards, then queue random
            selectedCards = availableCards
                .OrderByDescending(c => c.Type == CardType.Attack ? 3 : c.Type == CardType.Defense ? 2 : 1)
                .ThenBy(_ => session.Rng.Next())
                .Take(slotsToFill)
                .ToList();
        }

        foreach (var card in selectedCards)
        {
            battle.OpponentHand.Remove(card);
            battle.OpponentQueue.Add(card);
        }

        // Pad with Wait cards
        while (battle.OpponentQueue.Count < battle.CurrentOpponentQueueSlots)
        {
            battle.OpponentQueue.Add(_cardService.CreateWaitCard());
        }
    }

    private async Task<(bool, string?, BattleState)> ForfeitAsync(BattleSession session)
    {
        var battle = session.State;
        battle.WinnerId = battle.Opponent.Id;
        battle.Phase = BattlePhase.Ended;
        battle.Log($"You forfeit! {battle.Opponent.Name} wins!");

        // Emit battle end event for forfeit
        battle.EmitEvent(new BattleEndEvent
        {
            WinnerId = battle.Opponent.Id,
            WinnerName = battle.Opponent.Name,
            PlayerWon = false,
            PlayerFinalHP = battle.Player.CurrentHP,
            OpponentFinalHP = battle.Opponent.CurrentHP,
            Reason = "Player forfeited"
        });

        return (true, null, battle);
    }

    private async Task<(bool, string?, BattleState)> AutoQueueAndConfirmAsync(BattleSession session)
    {
        var battle = session.State;
        if (battle.Phase != BattlePhase.Queue)
        {
            return (false, "Not in queue phase", battle);
        }

        // Auto-select player cards
        await PlayerAutoQueueCardsAsync(session);

        // Pad with Wait cards if needed
        while (battle.PlayerQueue.Count < battle.CurrentPlayerQueueSlots)
        {
            battle.PlayerQueue.Add(_cardService.CreateWaitCard());
        }

        // AI queues cards for opponent
        await AIQueueCardsAsync(session);

        // Execute beforeQueueResolve hooks
        _hookExecutor.ExecuteHooks(HookType.BeforeQueueResolve, battle, battle.Player, battle.Opponent, session.Rng);
        _hookExecutor.ExecuteHooks(HookType.BeforeQueueResolve, battle, battle.Opponent, battle.Player, session.Rng);

        // Start resolution
        await StartResolutionAsync(session);

        return (true, null, battle);
    }

    private async Task PlayerAutoQueueCardsAsync(BattleSession session)
    {
        var battle = session.State;
        var availableCards = new List<Card>(battle.PlayerHand);
        var slotsToFill = battle.CurrentPlayerQueueSlots;

        List<Card> selectedCards;

        if (_aiBehaviorService != null && session.PlayerBehaviorRules != null)
        {
            selectedCards = _aiBehaviorService.SelectCardsForQueue(
                availableCards,
                slotsToFill,
                session.PlayerBehaviorRules,
                battle.Player,
                battle.Opponent,
                session.Rng);
        }
        else
        {
            // Fallback: simple priority (attack > defense > other)
            selectedCards = availableCards
                .OrderByDescending(c => c.Type == CardType.Attack ? 3 : c.Type == CardType.Defense ? 2 : 1)
                .ThenBy(_ => session.Rng.Next())
                .Take(slotsToFill)
                .ToList();
        }

        foreach (var card in selectedCards)
        {
            battle.PlayerHand.Remove(card);
            battle.PlayerQueue.Add(card);
            battle.Log($"[Auto] You queue {card.Name}");
        }
    }

    public async Task<(bool Success, string? Error, BattleState State)> ToggleAutoBattleAsync(
        string battleId, bool enabled, string? aiProfileId = null)
    {
        if (!_activeSessions.TryGetValue(battleId, out var session))
        {
            return (false, "Battle not found", null!);
        }

        session.AutoBattleEnabled = enabled;

        if (enabled && _aiBehaviorService != null)
        {
            session.PlayerAIProfileId = aiProfileId;
            session.PlayerBehaviorRules = await _aiBehaviorService.GetBehaviorRulesAsync(aiProfileId ?? "default");
        }

        return (true, null, session.State);
    }

    public async Task<BattleState> RunInstantBattleAsync(BattleSession session)
    {
        var battle = session.State;

        while (!battle.IsComplete)
        {
            if (battle.Phase == BattlePhase.Queue)
            {
                // Auto-select player cards
                await PlayerAutoQueueCardsAsync(session);

                // Pad with Wait cards if needed
                while (battle.PlayerQueue.Count < battle.CurrentPlayerQueueSlots)
                {
                    battle.PlayerQueue.Add(_cardService.CreateWaitCard());
                }

                // AI queues cards for opponent
                await AIQueueCardsAsync(session);

                // Execute beforeQueueResolve hooks
                _hookExecutor.ExecuteHooks(HookType.BeforeQueueResolve, battle, battle.Player, battle.Opponent, session.Rng);
                _hookExecutor.ExecuteHooks(HookType.BeforeQueueResolve, battle, battle.Opponent, battle.Player, session.Rng);

                // Start resolution (this will run through resolution and cleanup phases)
                await StartResolutionAsync(session);
            }
            // Other phases (Resolution, Cleanup) are handled automatically by the existing flow
        }

        return battle;
    }

    // Helper methods
    private List<Card> LoadDeck(List<string> cardIds, Random rng)
    {
        var deck = new List<Card>();
        foreach (var id in cardIds)
        {
            var card = _cardService.GetCard(id);
            if (card != null)
            {
                deck.Add(card.Clone());
            }
        }

        // Pad with Wait cards if needed
        while (deck.Count < _settings.Battle.MinDeckSize)
        {
            deck.Add(_cardService.CreateWaitCard());
        }

        return deck;
    }

    private void DrawCards(BattleState battle, Character character, int count, Random rng)
    {
        var (deck, hand, discard) = character == battle.Player
            ? (battle.PlayerDeck, battle.PlayerHand, battle.PlayerDiscard)
            : (battle.OpponentDeck, battle.OpponentHand, battle.OpponentDiscard);

        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0 && discard.Count > 0)
            {
                Shuffle(discard, rng);
                deck.AddRange(discard);
                discard.Clear();
                battle.Log($"{GetDisplayName(battle, character)}'s discard pile shuffled into deck!");
            }

            if (deck.Count > 0)
            {
                hand.Add(deck[0]);
                deck.RemoveAt(0);
            }
        }
    }

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static Character CloneCharacter(Character source)
    {
        return new Character
        {
            Id = source.Id,
            Name = source.Name,
            Level = source.Level,
            XP = source.XP,
            Attack = source.Attack,
            Defense = source.Defense,
            Speed = source.Speed,
            DeckCardIds = new List<string>(source.DeckCardIds),
            Wins = source.Wins,
            Losses = source.Losses,
            MMR = source.MMR,
            IsGhost = source.IsGhost
        };
    }

    public BattleSession? GetSession(string battleId) =>
        _activeSessions.GetValueOrDefault(battleId);

    // Helper to get display name (returns "You" for human player)
    private static string GetDisplayName(BattleState battle, Character c) =>
        ReferenceEquals(c, battle.Player) ? "You" : c.Name;

    private void UpdateEffectiveStats(BattleState battle, Random rng)
    {
        // Calculate player effective stats
        battle.PlayerEffectiveStats.Attack = _hookExecutor.GetCalculatedStat(
            HookType.OnCalculateAttack, battle, battle.Player, rng, battle.Player.BattleStats.Attack);
        battle.PlayerEffectiveStats.Defense = _hookExecutor.GetCalculatedStat(
            HookType.OnCalculateDefense, battle, battle.Player, rng, battle.Player.BattleStats.Defense);
        battle.PlayerEffectiveStats.Speed = _hookExecutor.GetCalculatedStat(
            HookType.OnCalculateSpeed, battle, battle.Player, rng, battle.Player.BattleStats.Speed);

        // Calculate opponent effective stats
        battle.OpponentEffectiveStats.Attack = _hookExecutor.GetCalculatedStat(
            HookType.OnCalculateAttack, battle, battle.Opponent, rng, battle.Opponent.BattleStats.Attack);
        battle.OpponentEffectiveStats.Defense = _hookExecutor.GetCalculatedStat(
            HookType.OnCalculateDefense, battle, battle.Opponent, rng, battle.Opponent.BattleStats.Defense);
        battle.OpponentEffectiveStats.Speed = _hookExecutor.GetCalculatedStat(
            HookType.OnCalculateSpeed, battle, battle.Opponent, rng, battle.Opponent.BattleStats.Speed);
    }

    public async Task<BattleResult?> FinalizeBattleAsync(string battleId)
    {
        if (!_activeSessions.TryRemove(battleId, out var session))
        {
            return null;
        }

        var battle = session.State;
        if (!battle.IsComplete)
        {
            return null;
        }

        bool playerWon = battle.WinnerId == battle.Player.Id;

        // Update character stats
        var character = await _characterRepository.GetByIdAsync(session.CharacterId);
        int xpGain = 0;
        int mmrChange = 0;
        int ghostMmrChange = 0;
        int levelsGained = 0;
        int newLevel = 0;

        if (character != null)
        {
            int oldLevel = character.Level;

            // Calculate MMR change using Elo formula or flat values
            if (_eloCalculator != null)
            {
                var (playerChange, opponentChange) = _eloCalculator.CalculateBothMMRChanges(
                    session.PlayerMMRAtStart,
                    session.OpponentMMRAtStart,
                    playerWon);
                mmrChange = playerChange;
                ghostMmrChange = opponentChange;
            }
            else
            {
                mmrChange = playerWon ? _settings.MMR.MMRGainOnWin : -_settings.MMR.MMRLossOnLoss;
                ghostMmrChange = playerWon ? -_settings.MMR.MMRLossOnLoss : _settings.MMR.MMRGainOnWin;
            }

            if (playerWon)
            {
                character.Wins++;
                xpGain = _settings.XP.XPForWin;
            }
            else
            {
                character.Losses++;
                xpGain = _settings.XP.XPForLoss;
            }

            character.MMR = Math.Max(_settings.MMR.MinimumMMR, character.MMR + mmrChange);
            await _characterRepository.UpdateAsync(character);

            // Update player stats
            var player = await _playerRepository.GetByIdAsync(session.PlayerId);
            if (player != null)
            {
                player.TotalBattles++;
                if (playerWon)
                    player.TotalWins++;
                else
                    player.TotalLosses++;

                if (character.MMR > player.HighestMMR)
                    player.HighestMMR = character.MMR;

                await _playerRepository.UpdateAsync(player);
            }

            // Add XP through CharacterService to trigger level-up logic
            var updatedCharacter = await _characterService.AddXPAsync(session.CharacterId, xpGain);
            if (updatedCharacter != null)
            {
                newLevel = updatedCharacter.Level;
                levelsGained = newLevel - oldLevel;
            }
        }

        // Update ghost stats with Elo-calculated MMR change
        if (session.GhostId != "default_ghost")
        {
            await _ghostRepository.UpdateStatsAsync(session.GhostId, !playerWon, ghostMmrChange);
        }

        return new BattleResult
        {
            BattleId = battleId,
            WinnerId = battle.WinnerId!,
            PlayerWon = playerWon,
            XPGained = xpGain,
            MMRChange = mmrChange,
            LevelsGained = levelsGained,
            NewLevel = newLevel,
            BattleLog = battle.BattleLog
        };
    }
}

public class BattleResult
{
    public string BattleId { get; set; } = string.Empty;
    public string WinnerId { get; set; } = string.Empty;
    public bool PlayerWon { get; set; }
    public int XPGained { get; set; }
    public int MMRChange { get; set; }
    public int LevelsGained { get; set; }
    public int NewLevel { get; set; }
    public List<string> BattleLog { get; set; } = new();
}

# SuperDeck - Master Design Document

**Version:** 1.3
**Date:** 2026-01-31
**Status:** In Progress - Card Scripting Specification Complete

---

## Summary

**SuperDeck** is a deck-building superhero card game featuring a unique hook-based scripting system that enables fully moddable cards and effects. Players create characters, collect themed cards from "suits" (fire, magic, martial arts, etc.), and battle opponents in turn-based card duels. The game supports both offline play with full modding capabilities and online competitive play with server-authoritative validation.

**Key Innovation:** All card and status effect logic is defined as C# scripts that compile at runtime via Roslyn, allowing unlimited moddability without game recompilation.

**Tech Stack:** C# .NET 10, Roslyn scripting engine, ASP.NET Core for server, console UI for prototype

**Current State:** Not started

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Game Design](#2-game-design)
3. [Card Definition and Scripting](#27-card-definition-and-scripting)
4. [Technical Architecture](#3-technical-architecture)


---

## 1. Project Overview

### 1.1 Vision

SuperDeck aims to provide a deeply customizable card game experience where players can:
- Build unique superhero characters with distinct playstyles
- Collect themed cards from various "suits"
- Compete locally or online
- Mod the game with custom cards

### 1.2 Core Pillars

1. **Moddability First**: All game content defined in editable JSON files with embedded C# scripts
2. **Strategic Depth**: Turn-based combat with hidden queuing and speed-based turn order
3. **Character Progression**: Simple leveling system (max level 10) with meaningful stat allocation
4. **Unified Architecture**: Same codebase supports offline modding and competitive online play
5. **Full Content Access**: No unlocks for cosmetics, suits, or any other content. All content is available to every player from the start.

---

## 2. Game Design

### 2.1 Core Game Loop

```
Character Creation → Battle → Earn XP → Level Up → Stat Allocation → Booster Pack → Repeat
```

### 2.2 Battle Flow

Each battle consists of multiple rounds until a winner is determined:

```
┌─────────────────────────────────────┐
│         1. DRAW PHASE               │
│  Each player draws 5 cards          │
│  Trigger deck cycling if needed     │
└──────────────┬──────────────────────┘
               ▼
┌─────────────────────────────────────┐
│         2. QUEUE PHASE              │
│  Each player secretly selects       │
│  3 cards from hand                  |
│  (or more/less from status effects  |
└──────────────┬──────────────────────┘
               ▼
┌─────────────────────────────────────┐
│      3. RESOLUTION PHASE            │
│  Determine turn order (speed-based) │
│  Execute cards alternating:         │
│  A1 → B1 → A2 → B2 → A3 → B3        │
│  Check win condition after each     │
└──────────────┬──────────────────────┘
               ▼
┌─────────────────────────────────────┐
│        4. CLEANUP PHASE             │
│  Move cards to discard              │
│  Process status effects             │
│  Tick durations, remove expired     │
└──────────────┬──────────────────────┘
               ▼
        [Check Win Condition]
               │
    ┌──────────┴──────────┐
    │                     │
   Yes                   No
    │                     │
    ▼                     ▼
[Battle End]      [Next Round]
```

### 2.3 Win Conditions

- **Primary:** Reduce opponent's HP to zero
- **Secondary:** System damage starts from round 10 (configurable through serversettings.json) and doubles each turn, ensuring battles conclude even if no direct damage occurs
- **Tie:** If simultaneous KO, last player to act wins

### 2.4 Card Economy

**No Resource System**: Cards cost nothing to play. Balance is achieved through:
- Queue limit: 3 cards per round (unless altered through status effects)
- Hand starting size: 5 cards
- Deck construction constraints: Minimum 9 cards (if deck is less than the configured minimum, "ghost" (visible only during battle phase) wait cards (do nothing) are added to pad deck to minimum
- Rarity-based power scaling

### 2.5 Health System

```
HP = 100 + (Level × 10)
```

**Examples:**
- Level 1: 110 HP
- Level 5: 150 HP
- Level 10: 200 HP (max)

### 2.6 Deck Building

**Rules:**
- Minimum 9 cards required
- If deck has fewer than 9 cards, it is padded with ghost "Wait" cards during battle (do-nothing cards that only exist in battle phase, not in deck/card editing)
- No maximum deck size
- No copy limits (full freedom)
- Starting hand size is 5, but card mechanics can add more cards beyond this limit
- Cards can be from any suit combination
- All owned cards are part of the deck

**Acquisition:**
- On character creation, player is given a choice of 5 suits + random. When picked a booster pack of only that suit is created (or cards of random suits if random was picked).
- Booster packs granted on level up
- 10 cards shown, player picks up to 3
- Players can sacrifice a choice to remove a card from their deck
- Remaining cards return to pool
- Suit weighting based on current deck composition (suits are more likely to show up based on the amount of suited cards owned)
- Booster packs can contain duplicate cards
- Players can skip selecting cards if they choose

---

## 2.7 Card Definition and Scripting

### 2.7.1 Card JSON Schema

Cards are defined in JSON files with embedded C# scripts:

```json
{
  "id": "card_unique_id",
  "name": "Card Name",
  "suit": "fire|magic|martial|tech|nature|berserker|electricity|espionage|mental|military|money|radiation|showbiz|speedster",
  "type": "attack|defense|buff|debuff|utility",
  "rarity": 1-5,
  "description": "Human-readable description",
  
  "immediateEffect": {
    "target": "self|opponent|both",
    "script": "C# code string with full Game State access"
  },
  
  "grantsStatusTo": {
    "target": "self|opponent|both",
    "status": {
      "name": "Status Name",
      "duration": 3,
      "hooks": {
        "onTurnStart": "C# code",
        "onTurnEnd": "C# code",
        "onTakeDamage": "C# code",
        "onDealDamage": "C# code",
        "onCalculateAttack": "C# code",
        "onCalculateDefense": "C# code",
        "onCalculateSpeed": "C# code",
        "onQueue": "C# code",
        "onPlay": "C# code",
        "onDiscard": "C# code",
        "onCardResolve": "C# code",
        "onHeal": "C# code",
        "onDeath": "C# code",
        "onDrawPhase": "C# code",
        "onQueuePhaseStart": "C# code",
        "beforeQueueResolve": "C# code",
        "onOpponentPlay": "C# code",
        "onBuffExpire": "C# code",
        "onBattleEnd": "C# code"
      }
    }
  },
  
  "animation": {
    "sprite": "filename.png",
    "frames": 8,
    "speed": 0.1
  }
}
```

### 2.7.2 ScriptContext API - Full Game State Access

Card scripts have **full access** to the live game object model. Helper functions are provided for convenience, but cards can directly manipulate any public property or field.

**Core Character References:**
```csharp
Character Player          // The card's owner
Character Opponent        // The enemy
Character Caster          // Who triggered this (usually Player)
Character Target          // Based on target field
```

**Battle State:**
```csharp
BattleState Battle        // Full battle state with all collections
```

**Direct Collection Access (Fully Modifiable):**
```csharp
List<Card> PlayerHand              // Direct hand reference - can add/remove
List<Card> OpponentHand
List<Card> PlayerQueue             // Can reorder, remove, insert cards
List<Card> OpponentQueue
List<Card> PlayerDiscard           // Move cards between piles freely
List<Card> OpponentDiscard
List<StatusEffect> PlayerStatuses  // Add/remove/modify statuses
List<StatusEffect> OpponentStatuses
List<Card> PlayerDeck              // Access to draw pile
List<Card> OpponentDeck
```

**Card Reference:**
```csharp
Card This                 // The card being executed
```

**Helper Utilities:**
```csharp
Random Rng               // Shared RNG instance for deterministic randomness
void Log(string message) // Output to battle log
```

**Common Operations (convenience methods):**
```csharp
void DealDamage(Character target, int amount)           // Apply damage with defense calculation
void DealRawDamage(Character target, int amount)        // Bypass defense
void Heal(Character target, int amount)                 // Restore HP
void ApplyStatus(Character target, StatusEffect status) // Add status effect
void RemoveStatus(Character target, string statusName)  // Remove specific status
void DrawCards(Character character, int count)          // Draw from deck
void DiscardCards(Character character, int count)       // Force discard
void Shuffle<T>(List<T> list)                          // Fisher-Yates shuffle
```

### 2.7.3 Script Execution Model

**Full Object Model Access:**
- Cards can directly manipulate any public property or field
- Private reflection is allowed (use with caution)
- No artificial restrictions on what cards can do
- Server-authoritative validation still applies to final state changes

**Examples of Direct Manipulation:**
```csharp
// Instead of using helpers, cards can directly modify:
Opponent.CurrentHP -= 15;
Player.BattleStats.Attack += 5;
Battle.PlayerQueue.RemoveAt(0);
Player.Deck.Add(CardLibrary.Get("punch"));
OpponentHand.Clear();
```

**Hook Context Variables:**
Specific hooks receive context variables that can be modified:
- `OnTakeDamage`: `int Amount` (modifiable), `Card Source`
- `OnCalculateAttack/Defense/Speed`: `int Amount` (modifiable)
- `OnCardResolve`: `Card Card`, `Character Player`, `Character Opponent`
- `OnBuffExpire`: `StatusEffect Status` (can call `Status.Remove()`)

### 2.7.4 Sample Card Definitions

**Simple Attack - Punch:**
```json
{
  "id": "punch_basic",
  "name": "Punch",
  "suit": "martial",
  "type": "attack",
  "rarity": 1,
  "description": "Deal 10 damage",
  "immediateEffect": {
    "target": "opponent",
    "script": "Opponent.CurrentHP -= 10;"
  }
}
```

**Attack with Self-Buff - Power Strike:**
```json
{
  "id": "power_strike",
  "name": "Power Strike",
  "suit": "martial",
  "type": "attack",
  "rarity": 2,
  "description": "Deal 15 damage. Gain +5 Attack for 2 turns.",
  "immediateEffect": {
    "target": "opponent",
    "script": "Opponent.CurrentHP -= 15;"
  },
  "grantsStatusTo": {
    "target": "self",
    "status": {
      "name": "Empowered",
      "duration": 2,
      "hooks": {
        "onCalculateAttack": "Amount += 5;"
      }
    }
  }
}
```

**Defensive Card - Shield Block:**
```json
{
  "id": "shield_block",
  "name": "Shield Block",
  "suit": "martial",
  "type": "defense",
  "rarity": 2,
  "description": "Reduce all damage taken by 50% for 2 turns",
  "grantsStatusTo": {
    "target": "self",
    "status": {
      "name": "Shielded",
      "duration": 2,
      "hooks": {
        "onTakeDamage": "Amount = (int)(Amount * 0.5);"
      }
    }
  }
}
```

**Complex Card - Fireball with Burn:**
```json
{
  "id": "fireball",
  "name": "Fireball",
  "suit": "fire",
  "type": "attack",
  "rarity": 3,
  "description": "Deal 12 damage and apply Burn for 3 turns (3 damage per turn)",
  "immediateEffect": {
    "target": "opponent",
    "script": "Opponent.CurrentHP -= 12;"
  },
  "grantsStatusTo": {
    "target": "opponent",
    "status": {
      "name": "Burn",
      "duration": 3,
      "hooks": {
        "onTurnStart": "Player.CurrentHP -= 3; Log(Player.Name + \" takes 3 burn damage!\");"
      }
    }
  }
}
```

**Queue Manipulation - ADHD:**
```json
{
  "id": "adhd",
  "name": "ADHD",
  "suit": "mental",
  "type": "debuff",
  "rarity": 2,
  "description": "Shuffle opponent's queue",
  "immediateEffect": {
    "target": "opponent",
    "script": "for (int i = OpponentQueue.Count - 1; i > 0; i--) { int j = Rng.Next(i + 1); var temp = OpponentQueue[i]; OpponentQueue[i] = OpponentQueue[j]; OpponentQueue[j] = temp; }"
  }
}
```

**Complex Buff - Battery (Accumulating):**
```json
{
  "id": "battery",
  "name": "Battery",
  "suit": "electricity",
  "type": "buff",
  "rarity": 3,
  "description": "Accumulate 10 damage per Electricity card played for 3 turns, then unleash all damage",
  "immediateEffect": {
    "target": "self",
    "script": "var battery = new StatusEffect { Name = \"Battery Charge\", Duration = 3, CustomState = new Dictionary<string, object> { [\"charge\"] = 0 } }; battery.Hooks[\"onPlay\"] = \"if (Card.Suit == \\\"Electricity\\\") { State[\\\"charge\\\"] = (int)State[\\\"charge\\\"] + 10; }\"; battery.Hooks[\"onBuffExpire\"] = \"Opponent.CurrentHP -= (int)State[\\\"charge\\\"];\"; PlayerStatuses.Add(battery);"
  }
}
```

**Counterspell - Smokebomb:**
```json
{
  "id": "smokebomb",
  "name": "Smokebomb",
  "suit": "espionage",
  "type": "debuff",
  "rarity": 3,
  "description": "Cancel opponent's next attack card. You lose 1 queue slot next turn.",
  "immediateEffect": {
    "target": "opponent",
    "script": "var nextAttack = OpponentQueue.FirstOrDefault(c => c.Type == CardType.Attack); if (nextAttack != null) { OpponentQueue.Remove(nextAttack); OpponentDiscard.Add(nextAttack); Log(\"Attack cancelled by Smokebomb!\"); }"
  },
  "grantsStatusTo": {
    "target": "self",
    "status": {
      "name": "Queue Restricted",
      "duration": 1,
      "hooks": {
        "onQueuePhaseStart": "Battle.MaxQueueSlots = Math.Max(1, Battle.MaxQueueSlots - 1);"
      }
    }
  }
}
```

**Revive Mechanic - Phoenix:**
```json
{
  "id": "phoenix",
  "name": "Phoenix",
  "suit": "fire",
  "type": "buff",
  "rarity": 5,
  "description": "If you reach 0 HP, revive at 50% health and reshuffle all cards",
  "grantsStatusTo": {
    "target": "self",
    "status": {
      "name": "Phoenix Rising",
      "duration": 1,
      "hooks": {
        "onBattleEnd": "if (Player.CurrentHP <= 0) { Player.CurrentHP = (int)(Player.MaxHP * 0.5); Player.Deck.AddRange(PlayerDiscard); PlayerDiscard.Clear(); Log(\"Phoenix rises from the ashes!\"); }"
      }
    }
  }
}
```

### 2.7.5 Security and Validation

**Server-Authoritative Boundaries:**
Even with full Game State access, these remain server-controlled:
- **Win conditions**: Server validates HP <= 0 after each card resolution
- **Character persistence**: Changes saved only after battle completion
- **Card validation**: Server verifies card exists in card library
- **Script timeout**: 500ms execution limit (configurable in serversettings.json)
- **Memory limits**: Sandboxed execution with memory caps

**Online Mode Security:**
- Only server operators can add/modify card JSON files
- Clients cannot inject custom scripts
- All script execution happens server-side
- Clients receive only validated battle state snapshots

**Offline Mode:**
- Full trust model - users can create any cards they want
- No artificial restrictions on script capabilities
- Users responsible for their own mod stability

---

## 2.8 Battle System and Turn Resolution

### 2.8.1 Battle Phase Overview

Each battle consists of multiple rounds until a winner is determined:

```
┌─────────────────────────────────────┐
│         1. ROUND SETUP PHASE        │
│  Calculate queue slots (base: 3)    │
│  Execute onQueuePhaseStart hooks    │
│  Players secretly select cards      │
│  Empty slots filled with Wait cards │
│  Execute beforeQueueResolve hooks   │
└──────────────┬──────────────────────┘
               ▼
┌─────────────────────────────────────┐
│      2. FIRST TURN DETERMINATION    │
│  Probabilistic roll based on        │
│  Speed stat ratio                   │
│  Lock in Player A and Player B      │
└──────────────┬──────────────────────┘
               ▼
┌─────────────────────────────────────┐
│      3. RESOLUTION PHASE            │
│  Alternating execution:             │
│  A1 → B1 → A2 → B2 → A3 → B3        │
│  Check win condition after each     │
│  Execute onCardResolve hooks        │
└──────────────┬──────────────────────┘
               ▼
┌─────────────────────────────────────┐
│        4. CLEANUP PHASE             │
│  Move cards to discard piles        │
│  Process status effect durations    │
│  Execute onTurnEnd hooks            │
│  Remove expired status effects      │
└──────────────┬──────────────────────┘
               ▼
        [Check Win Condition]
               │
    ┌──────────┴──────────┐
    │                     │
   Yes                   No
    │                     │
    ▼                     ▼
[Battle End]      [Next Round]
```

### 2.8.2 Round Setup Phase

**Queue Slot Calculation:**
- Base queue size: 3 cards
- Modified by active status effects via `onQueuePhaseStart` hooks
- Minimum queue size: 1 slot
- Maximum queue size: Configurable in serversettings.json (default: 5)

**Card Selection:**
- Players secretly select cards from their hand
- Cards are committed to the queue simultaneously
- Players cannot see opponent's queue until resolution begins
- Empty queue slots are automatically filled with "Wait" ghost cards

**Pre-Resolution Hooks:**
- `beforeQueueResolve` hooks execute after both players lock in their queues
- These hooks can modify queues, add/remove cards, or apply effects before resolution begins

### 2.8.3 First Turn Determination

**Speed-Based Probabilistic Roll:**

The player with the higher Speed stat has a proportionally higher chance to go first:

```
Player A First Turn Chance = PlayerA.Speed / (PlayerA.Speed + PlayerB.Speed)
Player B First Turn Chance = PlayerB.Speed / (PlayerA.Speed + PlayerB.Speed)
```

**Examples:**
- Player A (Speed 10) vs Player B (Speed 5):
  - Player A: 66.7% chance to go first
  - Player B: 33.3% chance to go first
  
- Player A (Speed 5) vs Player B (Speed 5):
  - Both players: 50% chance to go first
  
- Player A (Speed 20) vs Player B (Speed 5):
  - Player A: 80% chance to go first
  - Player B: 20% chance to go first

**Important Notes:**
- The roll uses the current Speed stats including any active buffs/debuffs
- Speed is calculated using the `onCalculateSpeed` hook before the roll
- Once "Player A" and "Player B" are determined, the alternation is locked for the entire round
- Speed changes during resolution do NOT affect the current round's order

### 2.8.4 Card Flow & Pile Management

**Deck to Draw Pile Relationship:**

At the start of each battle, the character's deck becomes the draw pile (PlayerDeck/OpponentDeck in BattleState). The deck is shuffled/randomized when the battle begins.

**Card Flow Lifecycle:**
```
┌──────────────┐     ┌──────────┐     ┌──────────┐     ┌──────────────┐     ┌──────────────┐
│    DECK      │ --> │   HAND   │ --> │  QUEUE   │ --> │ RESOLUTION   │ --> │   DISCARD    │
│ (Draw Pile)  │     │ (Playable)│    │ (Committed)│   │  (Execute)   │     │  (Used Cards) │
└──────────────┘     └──────────┘     └──────────┘     └──────────────┘     └──────────────┘
       │                                                                            │
       │<─────────────────────── DECK CYCLING ──────────────────────────────────────│
       │                                                                            │
       └───────────────── When draw pile empty, shuffle discard into deck ──────────┘
```

**Drawing Cards:**
- Cards move from `PlayerDeck` (draw pile) → `PlayerHand`
- Once in hand, cards are playable but not yet committed
- Drawing removes cards from the draw pile permanently for this battle
- **Script access:** `Battle.PlayerDeck.RemoveAt(0)` then `Battle.PlayerHand.Add(card)`

**Queuing Cards:**
- Cards move from `PlayerHand` → `PlayerQueue`
- Committing a card to the queue removes it from hand
- Queue represents cards that will be played this round
- **Script access:** `Battle.PlayerHand.Remove(card)` then `Battle.PlayerQueue.Add(card)`

**Card Resolution:**
- Cards execute from `PlayerQueue` in order
- After execution, card moves to `PlayerDiscard`
- Status effects may move cards elsewhere (e.g., remove from game, return to hand)
- **Script access:** `Battle.PlayerQueue.Remove(card)` then `Battle.PlayerDiscard.Add(card)`

**Deck Recycling (When Draw Pile Empty):**

When a player attempts to draw but `PlayerDeck` is empty:
1. Check if `PlayerDiscard` has cards
2. If yes: Shuffle discard pile
3. Move all cards from `PlayerDiscard` → `PlayerDeck`
4. Continue drawing from the recycled deck
5. If both deck and discard are empty: No cards to draw

**Script Example - Recycling:**
```csharp
// In DrawCards function or OnDrawPhase hook
if (PlayerDeck.Count == 0 && PlayerDiscard.Count > 0)
{
    // Shuffle discard
    Shuffle(PlayerDiscard);
    // Move to deck
    PlayerDeck.AddRange(PlayerDiscard);
    PlayerDiscard.Clear();
    Log("Discard pile shuffled into deck!");
}
```

**Key Properties in BattleState:**
```csharp
public class BattleState
{
    // Draw piles (where cards are drawn from)
    public List<Card> PlayerDeck { get; set; }      // Player's draw pile
    public List<Card> OpponentDeck { get; set; }    // Opponent's draw pile
    
    // Current playable cards
    public List<Card> PlayerHand { get; set; }      // Cards available to queue
    public List<Card> OpponentHand { get; set; }
    
    // Committed cards for this round
    public List<Card> PlayerQueue { get; set; }     // Cards to be played
    public List<Card> OpponentQueue { get; set; }
    
    // Used cards
    public List<Card> PlayerDiscard { get; set; }   // Played/discarded cards
    public List<Card> OpponentDiscard { get; set; }
}
```

**Important Notes:**
- The original character's deck is copied at battle start, not referenced
- Changes to the draw pile during battle don't affect the character's permanent deck
- Cards can be moved between piles by scripts (e.g., "Return 2 cards from discard to hand")
- The order of cards in the draw pile matters (top card drawn first)
- Shuffling should use a seeded RNG for deterministic replay/debugging

---

## 2.9 Data Persistence Layer

### 2.9.1 Architecture Overview

SuperDeck uses a unified database schema that works across both offline (SQLite) and online (MariaDB) modes. This approach ensures code reusability, consistent data models, and simplified maintenance.

**Key Design Principles:**
1. **Unified Schema**: Same table structure works for both SQLite and MariaDB
2. **Mode-Specific Tables**: Some tables only exist in online mode (PlayerAccounts)
3. **Ghost System**: AI opponents stored as character snapshots with independent MMR tracking
4. **Cross-Mode Ghost Downloads**: Offline players can download ghost pools from online servers

### 2.9.2 Database Schema

#### Characters Table (Both Modes)
```sql
CREATE TABLE Characters (
    Id VARCHAR(36) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Level INTEGER DEFAULT 1,
    XP INTEGER DEFAULT 0,
    Attack INTEGER DEFAULT 0,
    Defense INTEGER DEFAULT 0,
    Speed INTEGER DEFAULT 0,
    DeckCardIds TEXT, -- JSON array: ["punch_01", "fireball_02", ...]
    Wins INTEGER DEFAULT 0,
    Losses INTEGER DEFAULT 0,
    MMR INTEGER DEFAULT 1000,
    IsGhost BOOLEAN DEFAULT FALSE,
    IsPublished BOOLEAN DEFAULT FALSE,
    OwnerPlayerId VARCHAR(36) NULL, -- NULL for offline/local characters
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    LastModified DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (OwnerPlayerId) REFERENCES PlayerAccounts(Id)
);
```

**Field Descriptions:**
- **DeckCardIds**: JSON array storing card IDs (rebuilt from card files at runtime)
- **IsGhost**: TRUE if this character is an AI opponent (not controlled by a human)
- **IsPublished**: TRUE if character is available as a ghost for other players (online mode)
- **OwnerPlayerId**: Links to PlayerAccounts for online mode, NULL for offline local characters

#### PlayerAccounts Table (Online Mode Only)
```sql
CREATE TABLE PlayerAccounts (
    Id VARCHAR(36) PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Email VARCHAR(255),
    TotalWins INTEGER DEFAULT 0,
    TotalLosses INTEGER DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    LastLogin DATETIME
);
```

**Purpose:**
- Authentication for online play
- Aggregate statistics across all characters
- Account management (password reset, email, etc.)

#### GhostSnapshots Table (Both Modes)
```sql
CREATE TABLE GhostSnapshots (
    Id VARCHAR(36) PRIMARY KEY,
    SourceCharacterId VARCHAR(36) NOT NULL,
    SerializedCharacterState TEXT NOT NULL, -- Full Character JSON snapshot
    GhostMMR INTEGER DEFAULT 1000,
    Wins INTEGER DEFAULT 0,
    Losses INTEGER DEFAULT 0,
    TimesUsed INTEGER DEFAULT 0,
    AIProfileId VARCHAR(36) NOT NULL,
    DownloadedAt DATETIME, -- NULL for server-side ghosts
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (AIProfileId) REFERENCES AIProfiles(Id)
);
```

**Field Descriptions:**
- **SerializedCharacterState**: Complete Character object serialized as JSON (stats, deck, etc.)
- **GhostMMR**: Independent MMR tracked for this ghost (separate from original character)
- **DownloadedAt**: Timestamp when ghost was downloaded to offline client (NULL for server ghosts)
- **TimesUsed**: How many times this ghost has been matched against

#### AIProfiles Table (Both Modes)
```sql
CREATE TABLE AIProfiles (
    Id VARCHAR(36) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description TEXT,
    BehaviorRules TEXT NOT NULL, -- JSON configuration
    Difficulty INTEGER, -- 1-10 scale for matchmaking
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

**BehaviorRules JSON Structure:**
```json
{
  "priorityAttackWhenHPadvantage": 0.7,
  "alwaysQueueMaxCards": true,
  "defensiveThreshold": 0.3,
  "suitSynergyWeight": 0.5,
  "randomnessFactor": 0.2,
  "cardTypePreferences": {
    "attack": 0.4,
    "defense": 0.3,
    "buff": 0.2,
    "debuff": 0.1
  }
}
```

### 2.9.3 Repository Pattern Implementation

**Interface: ICharacterRepository**
```csharp
public interface ICharacterRepository
{
    Task<Character> GetByIdAsync(string id);
    Task<IEnumerable<Character>> GetByPlayerIdAsync(string playerId);
    Task<Character> CreateAsync(Character character);
    Task<Character> UpdateAsync(Character character);
    Task<bool> DeleteAsync(string id);
    Task<IEnumerable<Character>> GetGhostsByMMRRangeAsync(int minMMR, int maxMMR, int count);
    Task UpdateGhostStatsAsync(string ghostId, bool won);
}
```

**Implementations:**
- **SQLiteCharacterRepository**: For offline mode
- **MariaDBCharacterRepository**: For online mode
- **Unified schema** means both implementations share the same SQL queries

### 2.9.4 Offline Mode (SQLite)

**Characteristics:**
- No authentication required
- Single database file: `superdeck.db` in user data directory
- Local characters have NULL OwnerPlayerId
- Can download ghost snapshots from online servers

**Local Storage Path:**
```
Windows: %APPDATA%/SuperDeck/superdeck.db
macOS: ~/Library/Application Support/SuperDeck/superdeck.db
Linux: ~/.local/share/SuperDeck/superdeck.db
```

### 2.9.5 Online Mode (MariaDB)

**Characteristics:**
- Full authentication via PlayerAccounts table
- Characters linked to accounts via OwnerPlayerId
- Published characters become available as ghosts
- Server maintains authoritative ghost pool

**Ghost Publication Flow:**
1. Player finishes battle with their character
2. System automatically creates GhostSnapshot from character state
3. Ghost added to server pool with starting MMR based on character performance
4. Other players can match against this ghost

### 2.9.6 Ghost Download Feature (Offline Mode)

**Purpose:** Allow offline players to download diverse ghost opponents from online servers.

**API Endpoint:**
```
GET /api/ghosts/download
Returns: Array of GhostSnapshot objects
```

**Client Workflow:**
1. Player clicks "Update Ghosts" button in main menu
2. Client makes API request to server (no authentication required)
3. Server returns ALL ghost snapshots (not filtered by MMR)
4. Client stores ghosts in local GhostSnapshots table
5. Matchmaking uses local ghost pool

**Benefits:**
- Offline players always have fresh opponents
- No MMR filtering on download (player's MMR changes over time)
- Ghosts have independent MMR tracked locally
- Clean separation (downloaded ghosts don't affect server)

**Ghost Refresh Strategy:**
- Manual update via "Update Ghosts" button
- Recommended refresh: Weekly or before tournament play
- Old ghosts can be purged if not used for 30+ days

### 2.9.7 Data Migration and Portability

**Offline to Online:**
- Not automatically supported (separate database instances)
- Player can manually recreate character on server
- Deck card IDs must exist in server's card library

**Online to Offline:**
- Not supported (prevent cheating/copying high-level characters)
- Ghost downloads are read-only snapshots

**Ghost Pool Sync:**
- One-way sync: Server → Offline clients only
- Ghost MMR on server diverges from client ghost MMR over time
- Client ghost stats are local-only

### 2.9.8 Backup and Recovery

**Offline Mode:**
- Database file can be manually backed up
- Auto-backup on character modification (optional)
- Simple SQLite file copy for backup

**Online Mode:**
- Server-managed backups
- Character data persisted in MariaDB with regular backups
- Account recovery via email

---

### 2.8.4 Resolution Phase - Alternating Execution

**Turn Order:**
Once first-turn priority is established, cards resolve in strict alternation:

```
A1 → B1 → A2 → B2 → A3 → B3
```

**Key Rules:**
- Individual card speeds do NOT affect resolution order
- The queue order is the play order (first card queued = first card played for that player)
- Players cannot reorder their queue during resolution (unless a card/script allows it)

**Card Execution Flow:**
1. Execute `onOpponentPlay` hooks on opponent's active statuses (before card resolves)
2. Execute the card's `immediateEffect` script
3. Apply any `grantsStatusTo` effects
4. Execute `onPlay` hooks on player's active statuses
5. Check win conditions (HP <= 0)
6. Execute `onCardResolve` hooks on all active statuses
7. Move to next player's card

**Stun and Disable Effects:**
When a card is stunned or disabled:
- The card becomes a "Wait" action (does nothing)
- The player still consumes their turn slot
- The alternation continues: stunned card takes a turn, then opponent plays
- Example: If A1 is stunned, sequence is: A1(Wait) → B1 → A2 → B2...

**Mid-Resolution Modifications:**
Cards can modify queues during resolution:
- **Shuffling**: Reorders remaining unplayed cards (e.g., ADHD)
- **Canceling**: Removes a card from the queue (e.g., Smokebomb)
- **Adding**: Inserts new cards into the queue
- Modifications affect only unplayed cards; already-played cards remain resolved

### 2.8.5 Speed Changes and Stat Modifications

**Immediate Application:**
- Speed changes from card effects apply immediately to character stats
- However, they only affect the next round's first-turn roll
- Current round resolution continues with the locked alternation

**Example:**
- Round 1: Player A (Speed 10) wins first turn roll vs Player B (Speed 5)
- During Round 1 resolution: Player A plays a card that reduces their Speed to 5
- Round 1 continues with Player A as "Player A" (already locked in)
- Round 2: First turn roll uses new Speed values (both at 5 = 50/50 chance)

### 2.8.6 Win Condition Checking

**After Each Card Resolution:**
- Server checks if any player's HP <= 0
- If one player has HP <= 0: That player loses, battle ends
- If both players have HP <= 0 simultaneously: Last player to act wins
- Battle end triggers `onBattleEnd` hooks before final state is saved

**System Damage (Timeout Mechanic):**
Starting from round 10 (configurable in serversettings.json):
- Both players take escalating damage at round end
- Damage doubles each round: 1, 2, 4, 8, 16...
- Ensures battles conclude even without direct damage
- System damage can trigger win conditions

### 2.8.7 Wait Cards (Ghost Cards)

**Purpose:**
- Fill empty queue slots when a player has fewer cards than the queue allows
- Maintain alternation integrity

**Behavior:**
- Do nothing when played (no script execution)
- Still consume a turn slot
- Move to discard pile after "playing"
- Do not exist outside the battle (not in deck editor, not collectible)
- Cannot be targeted by card effects

**Example Scenario:**
- Player A has 2 cards, Player B has 3 cards
- Queue size is 3
- Player A's queue: [Card1, Card2, Wait]
- Player B's queue: [Card1, Card2, Card3]
- Resolution: A1 → B1 → A2 → B2 → A(Wait) → B3

---

## 3. Technical Architecture

### 3.1 Project Structure

```
SuperDeck/
├── Core/                           # Game logic and domain models
│   ├── Models/
│   │   ├── Character.cs           # Full character with all methods
│   │   ├── Card.cs                # Card with compiled script
│   │   ├── CardType.cs            # Enum: Attack/Defense/Buff/Debuff
│   │   ├── CharacterMode.cs       # Enum: Offline/Online
│   │   ├── GameState.cs           # Battle state machine
│   │   └── StatusEffect.cs        # Status with 19 hook types
│   │
│   ├── Scripting/                 # Roslyn compilation system
│   │   ├── ScriptCompiler.cs      # Compiles C# strings to delegates
│   │   └── ScriptContext.cs       # Global context for scripts
│   │
│   └── Data/                      # Database abstraction layer
│       ├── Repositories/          # Repository pattern implementations
│       │   ├── ICharacterRepository.cs  # Interface
│       │   ├── CharacterRepository.cs   # Implementation
│       │   ├── SQLiteCharacterRepository.cs  # SQLite implementation
│       │   └── MariaDBCharacterRepository.cs # MariaDB implementation
│       └── Models/                # Database models
│           └── CharacterDb.cs     # Database entity (may differ from domain model)
│
├── Client/                        # Main implementation (primary project)
│   ├── Program.cs                 # Entry point
│   │
│   ├── UI/                        # Console interface
│   │   ├── MainMenu.cs
│   │   ├── BattleUI.cs
│   │   ├── BoosterPackUI.cs
│   │   ├── LevelUpUI.cs
│   │   ├── SaveManager.cs
│   │   └── GameRunner.cs          # Main orchestrator
│   │
│   └── Networking/                # Server communication
│       ├── ApiClient.cs           # REST client
│       └── EmbeddedServerManager.cs # Manages server subprocess
│
├── Server/                        # ASP.NET Core API
│   ├── ServerProgram.cs           # Minimal HTTP API
│   ├── ServerSettings.cs          # Server configuration
│   ├── serversettings.json        # Server balance settings
│   │
│   ├── Services/
│   │   ├── CardService.cs         # Card loading & serving
│   │   ├── CharacterService.cs    # Character persistence (uses ICharacterRepository)
│   │   ├── BattleService.cs       # Battle execution logic
│   │   ├── OpponentService.cs     # Matchmaking & AI
│   │   └── BoosterPackService.cs  # Pack generation
│   │
│   └── Data/
│       └── ServerCards/           # Server-side card definitions (JSON)
│           ├── punch.json
│           ├── fireball.json
│           └── ... (many more)
│
│
└── SuperDeck.slnx                 # Solution file
```

### 3.2 Technology Stack

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| Language | C# | .NET 10.0 (LTS) | Core language |
| Scripting | Roslyn | Microsoft.CodeAnalysis.CSharp.Scripting 4.12.0 | Runtime C# compilation |
| Server | ASP.NET Core | .NET 10.0 (LTS) | HTTP API for online play |
| Persistence (Offline mode) | SQLite + JSON | SQLite (characters), JSON (cards) | Hybrid storage |
| Persistence (Online mode) | MariaDB + JSON | MariaDB (characters), JSON (cards) | Hybrid storage |
| Database Abstraction | Repository Pattern | Custom interface + Dapper | Unified data access |
| UI | Console → GUI | Spectre.Console → MonoGame/Raylib | Prototype → 2D graphics |
| Graphics | PNG + Palette Swap | Custom SpriteBatch | 2D rendering |

### 3.3 Client Types

SuperDeck will support two types of clients:

#### 3.3.1 Console-Based Client
- **Purpose**: First client to be developed for prototyping and testing.
- **Features**:
  - Text-based interface with colorful elements for easy distinction.
  - Full functionality for game mechanics testing.
  - Ideal for rapid iteration and debugging.
- **Implementation**: Built using `Spectre.Console` for advanced console rendering, including rich text formatting, tables, and interactive prompts.

#### 3.3.2 Graphics-Based Client
- **Purpose**: Final client for a polished user experience.
- **Features**:
  - 2D graphics with animations for card effects and battles.
  - Support for PNG-based sprites and palette swapping.
  - Interactive UI for deck building, character customization, and battles.
- **Implementation**: Built using MonoGame or Raylib for cross-platform compatibility.
- **Design Considerations**:
  - Animation sequences for card effects (e.g., fireball travel).
  - Modular design to accommodate future UI enhancements without major refactoring.
  - Support for both keyboard and mouse input.

**Key Points:**
- The console-based client will be developed first to facilitate prototyping and testing.
- The graphics-based client will be developed last but needs to be kept in mind for design choices during Core/Server development to avoid refactoring.
- Both clients will interact with the same server-authoritative backend, ensuring consistency in game logic and rules.

### 3.3 Architecture Patterns

| Pattern | Application | Benefit |
|---------|-------------|---------|
| **Hook System** | Status effects use script hooks at lifecycle events | Full moddability without code changes |
| **Singleton** | ServerSettings | Centralized configuration |
| **Dependency Injection** | ASP.NET Core services | Clean separation of concerns |
| **Template Method** | Card execution pipeline | Consistent script execution |
| **Observer** | Status effects observe character actions | Dynamic behavior extension |
| **Factory** | ScriptCompiler creates delegates from strings | Runtime code generation |
| **Repository** | SQLite/MariaDB for characters, JSON for cards | Hybrid persistence |
| **Sprite Batch** | PNG rendering with palette swaps | Efficient 2D graphics |

### 3.4 Key Design Decisions

#### 3.4.0 Server-Controlled Architecture

**Decision:** The server controls all aspects of the game, including characters, cards, and settings. The client is only used for player decisions.

**Example:**
```json
// serversettings.json (controlled by the server operator)
{
  "BaseHP": 100,
  "HPPerLevel": 10,
  "MaxDeckSize": 60,
  "ScriptTimeoutMs": 500,
  "InitialHandSize": 5,
  "CardsDrawnPerTurn": 3,
  "AttackDamageMultiplier": 0.02,
  "DefenseDamageReduction": 0.02,
  "XPPerWin": 1.0,
  "XPPerLoss": 0.5,
  "MaxLevel": 10
}
```

**Implementation:**
- **Offline Mode**: User runs a local server subprocess with their chosen settings.
- **Online Mode**: User connects to a remote server with fixed settings.
- **Client**: Always defers to the server for all game logic and rules.

#### 3.4.1 Roslyn Runtime Compilation

**Decision:** All cards and effects are C# scripts compiled and executed by the server at runtime.

**Benefits:**
- **Unlimited Moddability**: Users can create custom cards and effects without recompiling the game.
- **Full Access**: Scripts can use game state and C# features, but only within server-defined limits.
- **Type Safety**: Compile-time validation ensures scripts are syntactically correct.
- **Performance**: Compiled scripts are cached for reuse.

**Trade-offs:**
- **Initial Load Time**: Compilation adds latency at startup.
- **Memory**: Compiled scripts increase memory usage.
- **Security**: Online mode requires sandboxing to prevent malicious scripts.

**Implementation:**
- **Offline Mode**: Local server compiles and executes scripts with full trust.
- **Online Mode**: Remote server compiles scripts and validates all actions.

#### 3.4.2 Hook-Based Status Effects

**Decision:** Status effects don't execute immediately—they register hooks that trigger at specific events.

**19 Hook Types:**

**Basic Lifecycle Hooks:**
1. **OnTurnStart** - Start of each turn
2. **OnTurnEnd** - End of each turn
3. **OnQueue** - When owner queues a card
4. **OnPlay** - When owner plays a card
5. **OnDiscard** - When owner discards a card
6. **OnCardResolve** - When any card finishes resolving (yours or opponent's)

**Combat Hooks:**
7. **OnTakeDamage** - When owner takes damage
8. **OnDealDamage** - When owner deals damage
9. **OnHeal** - When owner heals
10. **OnDeath** / **BeforeDeath** - When character HP reaches 0, before battle end

**Stat Calculation Hooks:**
#11. **OnCalculateAttack** - When Attack stat is read (for damage calculation)
#12. **OnCalculateDefense** - When Defense stat is read (for damage reduction)
#13. **OnCalculateSpeed** - When Speed stat is read (for turn order)

**Phase Hooks:**
11. **OnDrawPhase** - During draw phase, before cards are drawn (allows modifying hand size)
12. **OnQueuePhaseStart** - At start of queue phase (allows modifying queue size)
13. **BeforeQueueResolve** - After both players queue cards, before resolution phase

**Interaction Hooks:**
17. **OnOpponentPlay** - When opponent plays a card (before it resolves)
18. **OnBuffExpire** - When a buff is about to expire or be removed
19. **OnBattleEnd** - When battle concludes, before returning to main menu

**Benefits:**
- Modders can create complex interactions in JSON
- Effects automatically work with all existing cards
- No hardcoded effect logic in game engine
- Comprehensive coverage for advanced card mechanics

**Example:**
```json
{
  "name": "Shield",
  "OnTakeDamage": "Amount = Amount / 2; // Halve damage"
}
```

---

## 2.10 Status Effect Hook Implementation

### 2.10.1 Core Architecture

Status effects in SuperDeck are implemented using a hook-based system with compiled C# delegates. This allows unlimited moddability without game recompilation while maintaining performance through runtime compilation.

**StatusEffect Class:**
```csharp
public class StatusEffect
{
    public string Name { get; set; }
    public int Duration { get; set; }  // Decrements each turn, removed at 0
    public Dictionary<string, Action<HookContext>> CompiledHooks { get; set; }
    public Dictionary<string, object> CustomState { get; set; }
    public DateTime AppliedAt { get; set; }
    
    public void Remove() 
    { 
        // Self-destruct - removes this status from the character
        Player.Statuses.Remove(this);
    }
}
```

**HookContext:**
```csharp
public class HookContext
{
    public Character Player { get; set; }      // Status owner (the character with this status)
    public Character Opponent { get; set; }    // The enemy character
    public BattleState Battle { get; set; }    // Full battle state with all collections
    public StatusEffect Status { get; set; }   // Reference to this status instance
    public Card TriggeringCard { get; set; }   // The card that triggered this (if applicable)
    
    // Scripts have full Game State access through these properties
    // No special modifiable variables - scripts directly manipulate game objects
}
```

### 2.10.2 The 19 Hook Types

Status effects can register any combination of these 19 hooks:

**Basic Lifecycle Hooks:**
| Hook | Trigger Timing | Common Use Cases |
|------|---------------|------------------|
| `onTurnStart` | Beginning of each round, before draw phase | Regeneration, recurring damage |
| `onTurnEnd` | End of each round, after cleanup | Duration-based effects |
| `onQueue` | When owner queues a card for play | Queue manipulation |
| `onPlay` | When owner plays a card (before it resolves) | Reaction triggers |
| `onDiscard` | When owner discards a card | Discard synergies |
| `onCardResolve` | When any card finishes resolving | Chain reactions |

**Combat Hooks:**
| Hook | Trigger Timing | Common Use Cases |
|------|---------------|------------------|
| `onTakeDamage` | When owner takes damage | Damage reduction, thorns |
| `onDealDamage` | When owner deals damage | Lifesteal, damage boosts |
| `onHeal` | When owner heals | Overheal, heal amplification |
| `onDeath` | When character HP reaches 0 | Death prevention, last stand |

**Stat Calculation Hooks:**
| Hook | Trigger Timing | Common Use Cases |
|------|---------------|------------------|
| `onCalculateAttack` | When Attack stat is read | Attack buffs/debuffs |
| `onCalculateDefense` | When Defense stat is read | Defense modifications |
| `onCalculateSpeed` | When Speed stat is read | Speed adjustments for turn order |

**Phase Hooks:**
| Hook | Trigger Timing | Common Use Cases |
|------|---------------|------------------|
| `onDrawPhase` | During draw phase, before cards drawn | Hand size modifications |
| `onQueuePhaseStart` | At start of queue phase | Queue slot modifications |
| `beforeQueueResolve` | After both players queue, before resolution | Queue manipulation |

**Interaction Hooks:**
| Hook | Trigger Timing | Common Use Cases |
|------|---------------|------------------|
| `onOpponentPlay` | When opponent plays a card | Counterspells, reactions |
| `onBuffExpire` | When status duration reaches 0 | Death throes, final effects |
| `onBattleEnd` | When battle concludes | Rewards, cleanup |

### 2.10.3 Hook Execution Rules

**1. Execution Order (FIFO):**
- Status effects execute their hooks in order of application
- First applied status executes its hook first
- Example: If Shield (applied turn 1) and Armor (applied turn 2) both have `onTakeDamage`, Shield executes first

**2. All Hooks Execute:**
- No hook can prevent subsequent hooks from executing
- Every registered hook for the event will fire
- Scripts must handle their own conflict resolution

**3. Full Game State Access:**
- Hooks have complete access to `Player`, `Opponent`, `Battle` objects
- Can directly modify HP, stats, queues, hands, discard piles
- Can add/remove other status effects
- No artificial restrictions

**4. Automatic Expiration:**
- Duration decrements at end of Cleanup Phase (after `onTurnEnd`)
- When Duration reaches 0:
  - Fire `onBuffExpire` hook
  - Remove status from character's status list
  - Status is garbage collected (unless referenced elsewhere)

**5. "Permanent" Statuses:**
- Set Duration to 9999 (or `int.MaxValue` for truly permanent)
- Or use `beforeQueueResolve` hook to reset Duration each round
- No special "permanent" flag needed

### 2.10.4 Status Application Process

**From Card Script:**
```csharp
// Card creates and applies a status
var burnStatus = new StatusEffect
{
    Name = "Burn",
    Duration = 3,
    CompiledHooks = new Dictionary<string, Action<HookContext>>
    {
        ["onTurnStart"] = context => {
            context.Player.CurrentHP -= 3;
            Log($"{context.Player.Name} takes 3 burn damage!");
        }
    },
    CustomState = new Dictionary<string, object>()
};

Target.Statuses.Add(burnStatus);
```

**Script Compilation:**
1. Card JSON defines hook scripts as strings
2. Roslyn compiles these strings into `Action<HookContext>` delegates
3. Delegates stored in `StatusEffect.CompiledHooks` dictionary
4. Compilation happens once when card/status is first loaded
5. Compiled delegates are cached for performance

### 2.10.5 Custom State Persistence

Status effects can store data that persists across hook executions:

**Example - Battery (Accumulating Damage):**
```csharp
// When status is created:
Status.CustomState["charge"] = 0;

// In onPlay hook:
if (TriggeringCard.Suit == "Electricity")
{
    Status.CustomState["charge"] = (int)Status.CustomState["charge"] + 10;
}

// In onBuffExpire hook:
int totalDamage = (int)Status.CustomState["charge"];
Opponent.CurrentHP -= totalDamage;
Log($"Battery releases {totalDamage} damage!");
```

**State Data Types:**
- `CustomState` is `Dictionary<string, object>`
- Can store any serializable data: ints, strings, lists, custom objects
- State persists for the lifetime of the status
- State is lost when status expires or is removed

### 2.10.6 Hook Execution Flow Examples

**Example 1: Shield + Armor (Damage Reduction Stack)**
```csharp
// Shield applied first (Duration 2)
Shield.Hooks["onTakeDamage"] = ctx => {
    ctx.Player.CurrentHP += 5; // Heal back 5 damage
    Log("Shield blocked 5 damage!");
};

// Armor applied second (Duration 3)
Armor.Hooks["onTakeDamage"] = ctx => {
    ctx.Player.CurrentHP += 3; // Heal back 3 damage
    Log("Armor blocked 3 damage!");
};

// When player takes 20 damage:
// 1. Shield executes first: Player takes 20, then heals 5 (net 15)
// 2. Armor executes second: Player heals 3 more (net 12)
// Total damage taken: 12 instead of 20
```

**Example 2: Counterspell (onOpponentPlay)**
```csharp
Counterspell.Hooks["onOpponentPlay"] = ctx => {
    if (ctx.TriggeringCard.Type == CardType.Attack)
    {
        // Cancel the attack by removing it from queue
        var opponentQueue = ctx.Battle.OpponentQueue;
        if (opponentQueue.Contains(ctx.TriggeringCard))
        {
            opponentQueue.Remove(ctx.TriggeringCard);
            ctx.Battle.OpponentDiscard.Add(ctx.TriggeringCard);
            Log($"Counterspell cancelled {ctx.TriggeringCard.Name}!");
        }
    }
};
```

**Example 3: Rage (Stat Calculation + Damage Modifier)**
```csharp
Rage.Hooks["onCalculateAttack"] = ctx => {
    // Direct stat modification (will be read by damage calculation)
    ctx.Player.BattleStats.Attack = (int)(ctx.Player.BattleStats.Attack * 1.3);
};

Rage.Hooks["onTakeDamage"] = ctx => {
    // Take 30% more damage
    int extraDamage = (int)(ctx.Player.LastDamageTaken * 0.3);
    ctx.Player.CurrentHP -= extraDamage;
    Log($"Rage amplified damage by {extraDamage}!");
};
```

### 2.10.7 Performance Considerations

**Compilation Caching:**
- Hook scripts compiled once and cached as delegates
- No runtime compilation during battle
- Cache keyed by status name + hook type

**Execution Efficiency:**
- Direct delegate invocation (no reflection)
- Minimal overhead per hook
- Fast dictionary lookups for hook registration

**Memory Management:**
- Status effects automatically cleaned up on expiration
- CustomState should avoid storing large objects
- Consider using value types for simple state

### 2.10.8 Common Implementation Patterns

**1. Damage Over Time (DoT):**
```csharp
// Burn status
Hooks["onTurnStart"] = ctx => {
    ctx.Player.CurrentHP -= 3;
    Log($"{ctx.Player.Name} burns for 3 damage!");
};
Duration = 3;
```

**2. Stat Modifier:**
```csharp
// Power Up status
Hooks["onCalculateAttack"] = ctx => {
    ctx.Player.BattleStats.Attack += 5;
};
Duration = 2;
```

**3. Damage Shield:**
```csharp
// Block status with charges
CustomState["charges"] = 3;
Hooks["onTakeDamage"] = ctx => {
    if ((int)Status.CustomState["charges"] > 0)
    {
        ctx.Player.CurrentHP += 5; // Block 5 damage
        Status.CustomState["charges"] = (int)Status.CustomState["charges"] - 1;
        Log("Blocked 5 damage! Charges remaining: " + Status.CustomState["charges"]);
        
        if ((int)Status.CustomState["charges"] == 0)
        {
            Status.Remove(); // Remove when charges depleted
        }
    }
};
```

**4. Reaction Trigger:**
```csharp
// Vengeance status
Hooks["onTakeDamage"] = ctx => {
    int damageTaken = ctx.Player.LastDamageTaken;
    // Store for use on next attack
    CustomState["storedDamage"] = damageTaken;
};

Hooks["onCalculateAttack"] = ctx => {
    if (CustomState.ContainsKey("storedDamage"))
    {
        ctx.Player.BattleStats.Attack += (int)CustomState["storedDamage"];
        CustomState.Remove("storedDamage");
        Log("Vengeance adds stored damage to attack!");
    }
};
Duration = 2;
```

---

## 2.11 Matchmaking and MMR System

### 2.11.1 Overview

SuperDeck uses an Elo-based matchmaking rating (MMR) system with a two-tier architecture designed for the unique character progression system where characters level from 0 to 10 and then retire.

**Two-Tier MMR System:**
1. **Account MMR** - Persistent across all characters, represents overall player skill
2. **Character MMR** - Individual character performance tracker, evolves through battles
3. **Ghost MMR** - Independent rating for AI-controlled ghost characters

**Design Goals:**
- Fair matchmaking based on character skill level
- Players face ghosts with similar MMR for balanced battles
- Fast character progression (~20 battles to reach level 10)
- Every level-up creates a new ghost snapshot
- Level 10 characters retire and become permanent ghosts

### 2.11.2 Elo System Configuration

**Recommended Parameters:**
```json
{
  "EloKFactor": 30,
  "StartingMMR": 1000,
  "MMRFloor": 100,
  "MMRCeiling": null,
  "MMRMatchRange": 200,
  "XPPerWin": 50,
  "XPPerLoss": 25
}
```

**Elo Formula:**
```
ExpectedResult = 1 / (1 + 10^((OpponentMMR - PlayerMMR) / 400))
MMRChange = K * (ActualResult - ExpectedResult)
```

**Examples:**
- Player (1000 MMR) beats Ghost (1200 MMR): Gains ~24 points
- Player (1000 MMR) loses to Ghost (800 MMR): Loses ~20 points
- Player (1000 MMR) beats Ghost (1000 MMR): Gains ~15 points

**MMR Boundaries:**
- **Floor:** 100 MMR minimum (cannot drop below)
- **Ceiling:** Uncapped (infinite climb possible)
- **Starting:** All new characters begin at 1000 MMR

### 2.11.3 Character Lifecycle and Ghost Generation

**Fast Progression System (~20 battles to level 10):**

| Level | XP Required | Total XP | Estimated Battles |
|-------|-------------|----------|-------------------|
| 1 | 50 | 50 | 1 win |
| 2 | 75 | 125 | 2-3 battles |
| 3 | 100 | 225 | 4-5 battles |
| 4 | 125 | 350 | 6-8 battles |
| 5 | 150 | 500 | 9-12 battles |
| 6 | 175 | 675 | 13-16 battles |
| 7 | 200 | 875 | 17-20 battles |
| 8 | 225 | 1100 | 21-25 battles |
| 9 | 250 | 1350 | 26-30 battles |
| 10 | 275 | 1625 | 31-35 battles |

**Ghost Creation Events:**
- **Level-Up Ghosts:** Every level 1-9, snapshot saved as new ghost
- **Retirement Ghost:** Level 10 final snapshot, character retires

**Ghost MMR Evolution:**
- Starts at character's MMR at creation
- Changes independently through battles
- May differ significantly from original character

### 2.11.4 Matchmaking Algorithm

**Ghost Selection:**
```csharp
public Ghost SelectGhost(Character player, int range = 200)
{
    var candidates = Ghosts.Where(g => 
        Math.Abs(g.GhostMMR - player.MMR) <= range);
    
    // Weight by MMR proximity (closer = higher weight)
    return WeightedRandom(candidates, 
        g => 1.0 / (Math.Abs(g.GhostMMR - player.MMR) + 1));
}
```

**Selection Criteria:**
1. MMR proximity (±200 range, weighted)
2. Variety (avoid recent repeats)
3. Suit diversity

### 2.11.5 Account MMR

**Calculation:**
```
AccountMMR = Average(AllCharacterMMRs) + WinRateBonus
WinRateBonus = (WinRate - 0.5) * 100
```

**Purpose:**
- Player profile display
- Historical skill tracking
- New character difficulty suggestion

### 2.11.6 MMR Display

**Character Stats:**
- Current MMR
- Win/Loss record
- Skill tier (Bronze/Silver/Gold/Platinum/Diamond)

**Post-Battle:**
- MMR change (+/-)
- Opponent ghost MMR
- Expected win probability

**Level 10 Retirement:**
- Full career stats
- Ghost pool contributions
- Most used cards
- Final MMR

### 2.11.7 Ghost Pool Management

- **Creation:** Automatic on every level-up
- **Cleanup:** Optional archiving for low-performing old ghosts
- **Minimum Pool:** 1000 ghosts (configurable)
- **Distribution:** Bell curve around 1000 MMR

### 2.11.8 Implementation

**Recording Battle Result:**
```csharp
public async Task RecordBattle(Character player, Ghost ghost, bool playerWon)
{
    // Calculate Elo
    double expected = 1.0 / (1.0 + Math.Pow(10, (ghost.MMR - player.MMR) / 400.0));
    int change = (int)(30 * ((playerWon ? 1.0 : 0.0) - expected));
    
    // Update player
    player.MMR = Math.Max(100, player.MMR + change);
    player.XP += playerWon ? 50 : 25;
    
    // Update ghost
    ghost.MMR = Math.Max(100, ghost.MMR - change);
    ghost.TimesUsed++;
    
    // Check level up
    if (player.XP >= XPForNextLevel(player.Level))
        await LevelUp(player);
    
    await SaveChanges();
}
```

---

## 2.12 Booster Pack Generation

### 2.12.1 Overview

Booster packs are the primary way players acquire new cards for their deck. Packs are generated dynamically based on rarity weights, suit rarity tiers, and the player's current deck composition to encourage thematic deck building.

**Key Features:**
- 10 cards per pack, player selects up to 3
- Weighted rarity distribution (Common to Legendary)
- Suit rarity system (Common, Uncommon, Rare suits)
- Deck composition influences suit appearance (+2% per owned card)
- Money suit and other rare suits can appear at very low rates
- Card removal is permanent (deck only)

### 2.12.2 Suit Configuration System

**Suits Defined in JSON:**
```json
{
  "suits": [
    {
      "id": "fire",
      "name": "Fire",
      "baseWeight": 20,
      "rarityTier": "common",
      "description": "Fire-based attacks and burns"
    },
    {
      "id": "martial",
      "name": "Martial Arts",
      "baseWeight": 20,
      "rarityTier": "common"
    },
    {
      "id": "magic",
      "name": "Magic",
      "baseWeight": 20,
      "rarityTier": "common"
    },
    {
      "id": "money",
      "name": "Money",
      "baseWeight": 0.01,
      "rarityTier": "rare",
      "description": "The elusive Money suit"
    }
  ]
}
```

**Suit Rarity Tiers:**
- **Common:** Base weight 20 (Fire, Martial, Magic, Berserker, Electricity, etc.)
- **Uncommon:** Base weight 10 (Espionage, Mental, Radiation, etc.)
- **Rare:** Base weight 0.01 (Money, and future rare suits)

### 2.12.3 Rarity Distribution

**Card Rarity Weights:**
```
Common:     60% (600/1000)
Uncommon:   30% (300/1000)
Rare:        9% (90/1000)
Legendary:   1% (10/1000)
```

### 2.12.4 Pack Generation Algorithm

**Step-by-Step Process:**

```csharp
public List<Card> GenerateBoosterPack(Character character)
{
    var pack = new List<Card>();
    var suits = LoadSuitConfiguration(); // From suits.json
    
    // Calculate suit weights based on deck composition
    var suitWeights = CalculateSuitWeights(character.Deck, suits);
    
    for (int i = 0; i < 10; i++)
    {
        // 1. Roll rarity
        var rarity = RollRarity();
        
        // 2. Roll suit based on weights
        var suit = RollSuit(suitWeights);
        
        // 3. Select random card of that rarity and suit
        var card = GetRandomCard(suit, rarity);
        
        pack.Add(card);
    }
    
    return pack;
}

private Dictionary<string, double> CalculateSuitWeights(
    List<Card> deck, 
    List<SuitConfig> suits)
{
    var weights = new Dictionary<string, double>();
    
    foreach (var suit in suits)
    {
        // Count cards of this suit in deck
        int ownedCards = deck.Count(c => c.Suit == suit.Id);
        
        // Calculate weight: Base + (OwnedCards × 2%)
        double baseWeight = suit.BaseWeight;
        double bonusWeight = ownedCards * 2.0;
        
        weights[suit.Id] = baseWeight + bonusWeight;
    }
    
    // Normalize weights to sum to 100%
    double totalWeight = weights.Values.Sum();
    foreach (var key in weights.Keys.ToList())
    {
        weights[key] = (weights[key] / totalWeight) * 100.0;
    }
    
    return weights;
}
```

### 2.12.5 "Random" Starter Pack Option

When creating a character, if player selects "Random":

- Weighted by rarity tier (rarer suits less likely)
- Common: 100 weight, Uncommon: 50, Rare: 1
- Very unlikely to get Money suit as starter but possible

### 2.12.6 Card Removal Mechanic

- Remove 1 card per sacrificed pick
- Removal is immediate and permanent
- Card cannot be recovered
- Can reduce deck below minimum (Wait cards added for battle)

### 2.12.7 Money Suit Handling

- Base appearance: 0.01% per card slot
- In 10-card pack: ~0.1% chance of Money card
- Over 100 packs: ~9.5% cumulative chance
- No special packs or events required

### 2.12.8 Implementation

```csharp
public class BoosterPackService
{
    public async Task<BoosterPack> GeneratePackAsync(Character character)
    {
        var pack = new BoosterPack();
        var suits = await LoadSuitConfigAsync();
        var allCards = await _cardRepo.GetAllAsync();
        
        var suitWeights = CalculateSuitWeights(character.Deck, suits);
        
        for (int i = 0; i < 10; i++)
        {
            var card = await GenerateCardAsync(suitWeights, allCards);
            pack.Cards.Add(card);
        }
        
        return pack;
    }
}
```

---

## 2.13 Networking Protocol and REST API

### 2.13.1 Overview

SuperDeck uses a turn-based REST API architecture where the server maintains authoritative control over all game logic. The client acts as a thin interface, sending player decisions and receiving battle state updates.

**Key Design Principles:**
1. **Server-Authoritative**: Server validates every action, client is display only
2. **Turn-Based**: No real-time requirements, HTTP polling is sufficient
3. **Session Persistence**: Players can disconnect and resume battles later
4. **Unified API**: Same endpoints work for offline (localhost) and online modes
5. **Full Validation**: Every player action is validated before state changes

### 2.13.2 Communication Pattern

**REST API with Session Persistence:**
- Client sends player decisions as HTTP POST requests
- Server responds with full battle state after each valid action
- JWT tokens maintain authentication across sessions
- Battles persist for 24 hours of inactivity

**No Real-Time Required:**
- Battle proceeds at player's pace (turn-based)
- Client polls for state when needed
- No WebSockets or Server-Sent Events needed
- Simpler implementation, easier to debug

### 2.13.3 Authentication

**JWT Token System:**
```csharp
// Login flow
POST /api/auth/login
Request: { "username": "player1", "password": "secret" }
Response: { "token": "eyJhbGciOiJIUzI1NiIs...", "expires": "2026-02-07T12:00:00Z" }
```

**Token Details:**
- **Type**: JWT (JSON Web Token)
- **Expiry**: 7 days (configurable)
- **Storage**: Client-side (local storage or secure storage)
- **Offline Mode**: No token required (middleware bypasses auth for localhost)
- **Header**: `Authorization: Bearer <token>`

**Token Refresh:**
- Tokens can be refreshed before expiry
- Refresh extends validity by another 7 days
- Offline mode doesn't need refresh (no expiry)

### 2.13.4 REST API Endpoints

#### Authentication Endpoints

**POST /api/auth/login**
- Authenticate player credentials
- Returns JWT token for subsequent requests

**POST /api/auth/logout**
- Invalidate current token
- Clear session server-side

**POST /api/auth/refresh**
- Refresh expiring token
- Returns new token with extended expiry

#### Character Endpoints

**GET /api/characters**
- List all characters for authenticated player
- Returns: Array of character summaries

**POST /api/characters**
- Create new character
- Body: `{ "name": "Hero", "suitChoice": "fire" }`
- Returns: New character with starter pack

**GET /api/characters/{id}**
- Get full character details
- Returns: Character stats, deck, MMR, history

**PUT /api/characters/{id}**
- Update character (stats allocation after level up)
- Body: `{ "attack": 8, "defense": 6, "speed": 6 }`
- Validation: Must be valid stat allocation

**DELETE /api/characters/{id}**
- Delete character permanently
- Confirmation required (irreversible)

**GET /api/characters/{id}/history**
- Get battle history for character
- Returns: List of past battles with results

#### Card Endpoints

**GET /api/cards**
- List all available cards in card library
- Returns: Array of card definitions

**GET /api/cards/{id}**
- Get specific card details
- Returns: Full card JSON with scripts

#### Battle System Endpoints

**POST /api/battle/start**
- Start new battle with selected character
- Body: `{ "characterId": "abc-123" }`
- Server: Selects ghost opponent by MMR matchmaking
- Returns: `{ "battleId": "xyz-789", "opponentGhost": {...}, "battleState": {...} }`

**POST /api/battle/{id}/action**
- Submit player action
- Body depends on action type:
  ```json
  // Queue phase
  { "action": "queue_card", "handIndex": 2, "queueSlot": 1 }
  
  // Confirm queue
  { "action": "confirm_queue" }
  
  // Forfeit
  { "action": "forfeit" }
  ```
- Server validates action fully
- Returns: `{ "valid": true/false, "message": "error or null", "battleState": {...} }`

**GET /api/battle/{id}/state**
- Get current battle state
- Used for: Reconnecting after disconnect, checking opponent's turn
- Returns: Full BattleState object

**POST /api/battle/{id}/forfeit**
- Surrender the battle
- Immediate loss, opponent wins
- Returns: Final battle results

**GET /api/battle/{id}/result**
- Get final battle results (after battle ends)
- Returns: Winner, MMR changes, XP gained, ghost snapshot created

#### Booster Pack Endpoints

**POST /api/packs/generate**
- Generate new booster pack (called on level up)
- Server: Uses pack generation algorithm with suit weights
- Returns: `{ "packId": "pack-456", "cards": [10 cards] }`

**POST /api/packs/{id}/select**
- Select cards to add to deck
- Body: `{ "selectedIndices": [0, 3, 7] }` (up to 3 cards)
- Cards added to character's deck
- Remaining cards discarded

**POST /api/packs/{id}/remove**
- Sacrifice a pick to remove card from deck
- Body: `{ "cardIdToRemove": "card-123" }`
- Card permanently removed from deck
- Player loses one selection from pack

#### Ghost Endpoints

**GET /api/ghosts/download**
- Download ALL ghost snapshots for offline play
- **No authentication required**
- Returns: Array of ghost snapshots with serialized character data
- Used by: Offline mode clients to populate local ghost pool

**GET /api/ghosts**
- List available ghosts (online mode)
- Query params: `?minMMR=800&maxMMR=1200&count=10`
- Returns: Ghost candidates for matchmaking

#### Matchmaking Endpoints

**POST /api/matchmaking/find**
- Request ghost opponent for battle
- Body: `{ "characterId": "abc-123" }`
- Server: Selects ghost by MMR proximity algorithm
- Returns: Ghost snapshot ready for battle

**GET /api/matchmaking/queue**
- Check matchmaking status (if implementing queued matchmaking)
- Returns: Queue position or matched opponent

### 2.13.5 Request/Response Examples

#### Start Battle Flow

**1. Start Battle:**
```http
POST /api/battle/start
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "characterId": "char-abc-123"
}

Response: 200 OK
{
  "battleId": "battle-xyz-789",
  "opponentGhost": {
    "id": "ghost-456",
    "name": "FireMage_L5",
    "mmr": 1150,
    "characterSnapshot": {
      "name": "FireMage",
      "level": 5,
      "hp": 150,
      "attack": 8,
      "defense": 5,
      "speed": 7,
      "deck": ["fireball", "ignite", "fireball", "tinder", ...]
    }
  },
  "battleState": {
    "battleId": "battle-xyz-789",
    "round": 1,
    "phase": "draw",
    "playerHand": ["punch", "kick", "block", "fireball", "heal"],
    "playerQueue": [],
    "playerDiscard": [],
    "opponentQueue": [],
    "playerHP": 110,
    "opponentHP": 150,
    "playerStatuses": [],
    "opponentStatuses": [],
    "awaitingPlayerAction": true
  }
}
```

**2. Queue Cards:**
```http
POST /api/battle/battle-xyz-789/action
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "action": "queue_card",
  "handIndex": 3,
  "queueSlot": 0
}

Response: 200 OK
{
  "valid": true,
  "message": null,
  "battleState": {
    "battleId": "battle-xyz-789",
    "phase": "draw",
    "playerHand": ["punch", "kick", "block", "heal"],  // fireball removed
    "playerQueue": ["fireball"],  // queued
    // ... rest of state
  }
}
```

**3. Invalid Action:**
```http
POST /api/battle/battle-xyz-789/action
Content-Type: application/json

{
  "action": "queue_card",
  "handIndex": 10,  // Invalid index
  "queueSlot": 0
}

Response: 400 Bad Request
{
  "valid": false,
  "message": "Invalid hand index: 10. Hand only has 5 cards.",
  "battleState": null
}
```

### 2.13.6 Server Validation

**Every Action Validated:**

1. **Authentication Check**
   - Valid JWT token?
   - Token not expired?
   - Token belongs to this player?

2. **Battle State Check**
   - Battle exists?
   - Battle not already ended?
   - Is it player's turn?
   - Correct phase for this action?

3. **Action Validity Check**
   - Is card in hand? (for queue actions)
   - Is queue slot available?
   - Any status effects preventing this?
   - Does player have required resources?

4. **Script Execution**
   - Execute card/status scripts
   - Validate scripts complete within timeout
   - Check win conditions
   - Update battle state

**Invalid Actions:**
- Return 400 Bad Request with error message
- Battle state unchanged
- Client displays error to player
- No penalty for invalid attempts

### 2.13.7 Session Persistence

**Battle State Storage:**

```csharp
// Server-side battle storage
public class BattleSession
{
    public string BattleId { get; set; }
    public string PlayerId { get; set; }
    public string CharacterId { get; set; }
    public string GhostId { get; set; }
    public BattleState State { get; set; }
    public DateTime LastActivity { get; set; }
    public bool IsActive { get; set; }
}
```

**Persistence Rules:**
- Battles stored in memory (Redis) or database
- LastActivity updated on every action
- Battles expire after 24 hours of inactivity
- Player can have multiple active battles (one per character)
- Reconnect: Call `GET /api/battle/{id}/state` to resume

**Disconnection Handling:**
- Battle continues server-side (opponent ghost AI)
- Player can reconnect and resume
- If battle ended while disconnected, show results on reconnect

### 2.13.8 Offline Mode Implementation

**Embedded Server:**
```csharp
// Offline mode starts local ASP.NET Core server
public class OfflineServerManager
{
    public void Start()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://localhost:5000");
        
        // Configure services same as online server
        builder.Services.AddSingleton<BattleService>();
        builder.Services.AddSingleton<CharacterRepository>();
        
        var app = builder.Build();
        
        // Same endpoints as online
        app.MapPost("/api/battle/start", BattleEndpoints.StartBattle);
        app.MapPost("/api/battle/{id}/action", BattleEndpoints.SubmitAction);
        // ... all other endpoints
        
        app.Run();
    }
}
```

**Offline Mode Differences:**
- Base URL: `http://localhost:5000` instead of remote server
- No JWT authentication (middleware skips auth for localhost)
- SQLite database instead of MariaDB
- Can still download ghosts from remote: `GET https://onlineserver.com/api/ghosts/download`

### 2.13.9 API Security

**CORS Policy:**
```csharp
// Allow client application to call API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins("https://superdeck-client.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

**Rate Limiting:**
```csharp
// Prevent abuse
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("battle", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(1)
            }));
});
```

**Input Validation:**
- All request bodies validated using FluentValidation
- SQL injection prevention (parameterized queries)
- XSS prevention (output encoding)
- Max request size limits

### 2.13.10 Error Handling

**Standard Error Response:**
```json
{
  "error": {
    "code": "INVALID_ACTION",
    "message": "Cannot queue card: not in queue phase",
    "details": {
      "currentPhase": "resolution",
      "expectedPhase": "queue"
    }
  }
}
```

**HTTP Status Codes:**
- **200 OK**: Success
- **400 Bad Request**: Invalid input or action
- **401 Unauthorized**: Missing or invalid token
- **403 Forbidden**: Valid token but not allowed (e.g., accessing another player's character)
- **404 Not Found**: Resource doesn't exist
- **409 Conflict**: Valid request but conflicts with state (e.g., battle already ended)
- **500 Internal Server Error**: Server error (shouldn't happen)

### 2.13.11 Client Implementation Notes

**HTTP Client Setup:**
```csharp
public class SuperDeckApiClient
{
    private readonly HttpClient _httpClient;
    private string _token;
    
    public SuperDeckApiClient(string baseUrl)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }
    
    public void SetToken(string token)
    {
        _token = token;
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
    }
    
    public async Task<BattleState> StartBattleAsync(string characterId)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/battle/start", 
            new { characterId });
        
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<StartBattleResponse>();
        return result.BattleState;
    }
    
    public async Task<(bool Valid, string Message, BattleState State)> SubmitActionAsync(
        string battleId, 
        PlayerAction action)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/battle/{battleId}/action", 
            action);
        
        var result = await response.Content.ReadFromJsonAsync<ActionResponse>();
        return (result.Valid, result.Message, result.BattleState);
    }
}
```

**Offline/Online Mode Switching:**
```csharp
public class GameClient
{
    private SuperDeckApiClient _client;
    
    public void SetMode(GameMode mode)
    {
        _client = mode switch
        {
            GameMode.Offline => new SuperDeckApiClient("http://localhost:5000"),
            GameMode.Online => new SuperDeckApiClient("https://superdeck-server.com"),
            _ => throw new ArgumentException()
        };
    }
}
```

---


## 2.14 Graphical Client Support (Future)

**Note:** This section outlines design considerations for a future graphical client to ensure current implementation supports it without refactoring.

### 2.14.1 Costume System

**Overview:**
Characters can equip purely cosmetic costume pieces at creation. Costumes persist with the character and are freely available (no unlocking required).

**Costume Slots (6 total):**
- Head (helmet, mask, hat, etc.)
- Chest (suit, armor, shirt, etc.)
- Legs (pants, shorts, etc.)
- Hands (gloves, gauntlets, etc.)
- Feet (boots, shoes, etc.)
- Cape (cape, cloak, or "none")

**Costume Rules:**
- **Purely cosmetic** - No stat changes or gameplay effects
- **Mix and match** - Players can combine pieces from any theme/style
- **Freely available** - All costume pieces unlocked from start
- **Persist with character** - Saved to database with character data

**Character Costume Data:**
```json
{
  "costume": {
    "head": "helmet_dragon",
    "chest": "suit_armored",
    "legs": "pants_dark",
    "hands": "gloves_leather",
    "feet": "boots_steel",
    "cape": "cape_red"
  }
}
```

**Asset Naming Convention:**
- Format: `{slot}_{style}_{variant}.png`
- Examples: `head_helmet_dragon.png`, `chest_suit_fire_01.png`, `cape_none.png`
- All costumes are PNG files with transparency

### 2.14.2 Character Pose System

**Overview:**
Characters display different poses based on game state. Cards can optionally override poses for special effects.

**Standard Poses:**
1. **idle** - Default standing pose
2. **attack_01**, **attack_02**, **attack_03...** - Various attack animations
3. **takedamage_01**, **takedamage_02...** - Different damage reactions
4. **block** - Defensive/blocking pose
5. **win** - Victory celebration
6. **lose** - Defeat pose

**Pose Selection Logic:**
```csharp
// Priority order:
1. Card-specified pose (if card has visualEffects.casterPose)
2. Game state inference:
   - If queuing attack card → attack pose
   - If took damage this turn → takedamage pose
   - If battle won → win pose
   - If battle lost → lose pose
   - Otherwise → idle pose
```

**Pose Override in Card Scripts:**
Cards can force a specific pose during their execution:
```csharp
// In card immediateEffect script
Player.Pose = "attack_02";  // Override pose for this action
// Execute attack logic...
Player.Pose = null;  // Return to automatic pose selection
```

**Asset Organization:**
```
/Assets/Characters/Poses/
  idle.png
  attack_01.png, attack_02.png, attack_03.png...
  takedamage_01.png, takedamage_02.png...
  block.png
  win.png
  lose.png
```

### 2.14.3 Card Visual Effects

**Overview:**
Cards specify visual sequences to play when executed. The graphical client renders these effects while the console client ignores them.

**Card Visual Effects Schema:**
```json
{
  "visualEffects": {
    "casterPose": "attack_01",
    "sequences": [
      {
        "name": "fireball_travel",
        "sprites": [
          "fireball_01.png",
          "fireball_02.png", 
          "fireball_03.png"
        ],
        "target": "opponent",
        "startPosition": "caster",
        "endPosition": "target",
        "speed": 0.1,
        "sound": "fireball_whoosh.wav"
      },
      {
        "name": "explosion_impact",
        "sprites": [
          "explosion_01.png",
          "explosion_02.png",
          "explosion_03.png"
        ],
        "target": "opponent",
        "position": "target",
        "speed": 0.15,
        "sound": "explosion_boom.wav",
        "screenShake": 0.5
      }
    ]
  }
}
```

**Visual Effect Properties:**

**Sequence Object:**
- **name** - Identifier for the effect sequence
- **sprites** - Array of PNG files to animate in order
- **target** - Who the effect targets ("self", "opponent", "both")
- **startPosition** - Where animation starts ("caster", "target", "center")
- **endPosition** - Where animation ends ("caster", "target", "center")
- **position** - Static position ("caster", "target", "center") for non-moving effects
- **speed** - Seconds per frame (0.1 = 10 FPS)
- **sound** - Optional WAV file to play
- **screenShake** - Optional screen shake intensity (0-1)

**Multiple Sequences:**
Cards can have multiple sequences that play:
- Sequentially (one after another)
- Simultaneously (overlapping)
- Example: Fireball travels (sequence 1), then explosion at target (sequence 2)

**Simple Card Example:**
```json
{
  "id": "punch_basic",
  "name": "Punch",
  "suit": "martial",
  "type": "attack",
  "rarity": 1,
  "description": "Deal 10 damage",
  "immediateEffect": {
    "target": "opponent",
    "script": "Opponent.CurrentHP -= 10;"
  },
  "visualEffects": {
    "casterPose": "attack_01",
    "sequences": [
      {
        "name": "punch_lunge",
        "sprites": ["punch_effect_01.png"],
        "target": "opponent",
        "startPosition": "caster",
        "endPosition": "target",
        "speed": 0.05,
        "sound": "punch_swing.wav"
      }
    ]
  }
}
```

**Complex Card Example:**
```json
{
  "id": "fireball",
  "name": "Fireball",
  "visualEffects": {
    "casterPose": "attack_02",
    "sequences": [
      {
        "name": "charge_up",
        "sprites": ["fire_charge_01.png", "fire_charge_02.png"],
        "target": "self",
        "position": "caster",
        "speed": 0.1,
        "sound": "fire_charge.wav"
      },
      {
        "name": "fireball_flight",
        "sprites": ["fireball_01.png", "fireball_02.png", "fireball_03.png"],
        "target": "opponent",
        "startPosition": "caster",
        "endPosition": "target",
        "speed": 0.08,
        "sound": "fireball_fly.wav"
      },
      {
        "name": "impact_explosion",
        "sprites": ["explosion_01.png", "explosion_02.png", "explosion_03.png"],
        "target": "opponent",
        "position": "target",
        "speed": 0.12,
        "sound": "explosion_hit.wav",
        "screenShake": 0.3
      }
    ]
  }
}
```

### 2.14.4 Asset Organization

**Folder Structure:**
```
/Assets/
  /Characters/
    /Poses/
      idle.png
      attack_*.png
      takedamage_*.png
      block.png
      win.png
      lose.png
    /Costumes/
      /Head/
        helmet_*.png
        mask_*.png
        hat_*.png
      /Chest/
        suit_*.png
        armor_*.png
        shirt_*.png
      /Legs/
        pants_*.png
        shorts_*.png
      /Hands/
        gloves_*.png
        gauntlets_*.png
      /Feet/
        boots_*.png
        shoes_*.png
      /Cape/
        cape_*.png
        cloak_*.png
  /Cards/
    /Effects/
      fireball_*.png
      explosion_*.png
      punch_effect_*.png
      heal_effect_*.png
      // Organized by effect type, not by card
  /UI/
    /Buttons/
    /Icons/
    /Backgrounds/
  /Sounds/
    /Effects/
      fireball_whoosh.wav
      explosion_boom.wav
      punch_swing.wav
    /Music/
```

**Asset Guidelines:**
- All images: PNG format with transparency
- Recommended size: 256x256 or 512x512 for characters
- Effects: 128x128 or 256x256
- Sounds: WAV or OGG format
- Naming: lowercase with underscores

### 2.14.5 Console vs Graphical Client Compatibility

**Backward Compatibility:**
- All visual fields are **optional** in card/character JSON
- Console client **ignores** visualEffects, costume, and pose fields
- Graphical client **uses** these fields when present
- Same BattleState works for both clients
- Same API endpoints serve both clients

**Data Flow:**
```
Server BattleState → API → Console Client (ignores visuals)
                              ↓
                        Graphical Client (renders visuals)
```

**Implementation Strategy:**
1. Add optional VisualEffects and Costume properties to models now
2. Console client works without them (null checks)
3. Future graphical client reads and renders them
4. No breaking changes to existing code

### 2.14.6 Pose State in BattleState

**Optional Pose Tracking:**
```csharp
public class BattleState
{
    // ... existing properties ...
    
    // Graphical client only (null for console)
    public string PlayerCurrentPose { get; set; }
    public string OpponentCurrentPose { get; set; }
    
    // For pose overrides from card scripts
    public string PlayerForcedPose { get; set; }
    public string OpponentForcedPose { get; set; }
}
```

**Pose Reset:**
- Forced poses automatically clear after card resolution
- If ForcedPose is null, graphical client infers from game state
- If ForcedPose is set, graphical client uses it

---


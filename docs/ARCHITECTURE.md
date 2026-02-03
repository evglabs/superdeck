# SuperDeck Architecture

Technical architecture documentation for SuperDeck.

## Table of Contents

- [System Overview](#system-overview)
- [Project Structure](#project-structure)
- [Core Concepts](#core-concepts)
- [Data Flow](#data-flow)
- [Scripting Engine](#scripting-engine)
- [Database Layer](#database-layer)
- [API Design](#api-design)
- [Client Architecture](#client-architecture)
- [Security Model](#security-model)

---

## System Overview

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         Client Layer                             │
│  ┌───────────────────┐  ┌───────────────────────────────────┐  │
│  │   Console UI      │  │        API Client                  │  │
│  │   (Spectre)       │◄─┤  ┌─────────────────────────────┐  │  │
│  └───────────────────┘  │  │  Embedded Server Manager    │  │  │
│                         │  │  (Offline Mode)             │  │  │
│                         │  └─────────────────────────────┘  │  │
│                         └───────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    │ HTTP/JSON
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                         Server Layer                             │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    ASP.NET Core API                        │  │
│  │  ┌─────────────────────────────────────────────────────┐  │  │
│  │  │                   Services                           │  │  │
│  │  │  ┌──────────┐ ┌──────────┐ ┌──────────────────────┐ │  │  │
│  │  │  │ Battle   │ │ Card     │ │ Character            │ │  │  │
│  │  │  │ Service  │ │ Service  │ │ Service              │ │  │  │
│  │  │  └──────────┘ └──────────┘ └──────────────────────┘ │  │  │
│  │  │  ┌──────────┐ ┌──────────┐ ┌──────────────────────┐ │  │  │
│  │  │  │ Auth     │ │ AI       │ │ Booster Pack         │ │  │  │
│  │  │  │ Service  │ │ Behavior │ │ Service              │ │  │  │
│  │  │  └──────────┘ └──────────┘ └──────────────────────┘ │  │  │
│  │  └─────────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                         Core Layer                               │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────────────┐ │
│  │   Models     │  │   Settings   │  │   Scripting Engine    │ │
│  │              │  │              │  │   (Roslyn)            │ │
│  │  Character   │  │  GameSettings│  │                       │ │
│  │  Card        │  │  BattleConf  │  │  ScriptRunner         │ │
│  │  BattleState │  │  AuthConf    │  │  HookRegistry         │ │
│  │  StatusEffect│  │              │  │  HookExecutor         │ │
│  └──────────────┘  └──────────────┘  └───────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                         Data Layer                               │
│  ┌────────────────────────┐  ┌────────────────────────────────┐│
│  │   SQLite Repository    │  │   MariaDB Repository           ││
│  │   (Offline)            │  │   (Online)                     ││
│  └────────────────────────┘  └────────────────────────────────┘│
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐│
│  │                    Card JSON Files                          ││
│  │                    (Data/ServerCards/)                      ││
│  └────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

### Key Design Principles

1. **Server-Authoritative:** All game logic runs on the server
2. **No Offline Code Path:** Offline mode is localhost server
3. **Moddable Scripts:** Card effects are runtime-compiled C#
4. **Stateless API:** All state stored in database
5. **Shared Core:** Domain models shared between client and server

---

## Project Structure

### Solution Layout

```
SuperDeck.slnx
├── src/
│   ├── Core/                    # Shared domain layer
│   │   ├── Models/              # Domain entities
│   │   ├── Scripting/           # Roslyn engine
│   │   ├── Settings/            # Configuration classes
│   │   └── Data/Repositories/   # Repository interfaces
│   │
│   ├── Server/                  # ASP.NET Core server
│   │   ├── Services/            # Business logic
│   │   ├── Data/                # Repository implementations
│   │   │   ├── Repositories/    # SQLite repositories
│   │   │   └── ServerCards/     # Card JSON files
│   │   └── Program.cs           # API endpoints
│   │
│   ├── Client/                  # Console client
│   │   ├── UI/                  # Spectre.Console UI
│   │   └── Networking/          # API client + embedded server
│   │
│   └── Tools/                   # Utility projects
│
└── tests/
    └── Tests/                   # Unit/integration tests
```

### Project Dependencies

```
SuperDeck.Core
    └── Microsoft.CodeAnalysis.CSharp.Scripting (Roslyn)

SuperDeck.Server
    ├── SuperDeck.Core
    ├── Dapper
    ├── Microsoft.Data.Sqlite
    └── MySqlConnector

SuperDeck.Client
    ├── SuperDeck.Core
    └── Spectre.Console
```

---

## Core Concepts

### Domain Models

#### Character

```csharp
public class Character
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public int XP { get; set; }

    // Stats
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }

    // Deck
    public List<string> DeckCardIds { get; set; }

    // Progression
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int MMR { get; set; }

    // Ownership
    public string? OwnerPlayerId { get; set; }
}
```

#### Card

```csharp
public class Card
{
    public string Id { get; set; }
    public string Name { get; set; }
    public Suit Suit { get; set; }
    public CardType Type { get; set; }
    public Rarity Rarity { get; set; }
    public string Description { get; set; }

    // Effects
    public CardEffect? ImmediateEffect { get; set; }
    public StatusGrant? GrantsStatusTo { get; set; }
}

public class CardEffect
{
    public TargetType Target { get; set; }
    public string Script { get; set; }
}
```

#### BattleState

```csharp
public class BattleState
{
    public string BattleId { get; set; }
    public Character Player { get; set; }
    public Character Opponent { get; set; }

    // Phase management
    public BattlePhase Phase { get; set; }
    public int Round { get; set; }

    // Cards
    public List<Card> PlayerHand { get; set; }
    public List<Card> PlayerQueue { get; set; }
    public List<Card> PlayerDeck { get; set; }
    public List<Card> PlayerDiscard { get; set; }

    // Status effects
    public List<StatusEffect> PlayerStatuses { get; set; }
    public List<StatusEffect> OpponentStatuses { get; set; }

    // Battle log
    public List<string> BattleLog { get; set; }
}
```

### Enumerations

```csharp
public enum Suit
{
    Basic, Fire, MartialArts, Magic, Electricity,
    Mental, Espionage, Nature, Tech, Berserker,
    Military, Radiation, Showbiz, Speedster, Money
}

public enum CardType
{
    Attack, Defense, Buff, Debuff, Utility
}

public enum Rarity
{
    Common = 1, Uncommon = 2, Rare = 3,
    Epic = 4, Legendary = 5
}

public enum BattlePhase
{
    NotStarted, DrawPhase, QueuePhase,
    ResolutionPhase, Cleanup, Ended
}

public enum HookType
{
    // Lifecycle
    OnTurnStart, OnTurnEnd, OnQueue, OnPlay,
    OnDiscard, OnCardResolve,

    // Combat
    OnTakeDamage, OnDealDamage, OnHeal, OnDeath,

    // Stat calculation
    OnCalculateAttack, OnCalculateDefense, OnCalculateSpeed,

    // Battle phases
    OnDrawPhase, OnQueuePhaseStart, BeforeQueueResolve,
    OnBattleEnd, OnOpponentPlay, OnBuffExpire
}
```

---

## Data Flow

### Battle Flow

```
1. Client: POST /api/battle/start
   └── Server: CreateBattle()
       ├── Load character
       ├── Find or create opponent (ghost)
       ├── Initialize battle state
       ├── Draw starting hands
       └── Return battle state

2. Client: POST /api/battle/{id}/action (queue cards)
   └── Server: ProcessAction()
       ├── Validate action
       ├── Add cards to queue
       └── Return updated state

3. Client: POST /api/battle/{id}/action (end queue)
   └── Server: ResolveRound()
       ├── Determine turn order (speed)
       ├── For each queued card:
       │   ├── Execute immediate effect
       │   ├── Apply status effects
       │   └── Execute hooks
       ├── Process status tick
       ├── Check win condition
       └── Return updated state

4. Loop until battle ends

5. Client: POST /api/battle/{id}/finalize
   └── Server: FinalizeBattle()
       ├── Calculate XP/MMR
       ├── Update character
       ├── Save ghost snapshot
       └── Return results
```

### Card Effect Execution

```
Card Played
    │
    ▼
┌───────────────────────────┐
│ Compile Script (cached)   │
│ "DealDamage(Opponent, 15)"│
└───────────────────────────┘
    │
    ▼
┌───────────────────────────┐
│ Create HookContext        │
│ - Player reference        │
│ - Opponent reference      │
│ - Battle state            │
│ - Random instance         │
└───────────────────────────┘
    │
    ▼
┌───────────────────────────┐
│ Execute in Sandbox        │
│ - 500ms timeout           │
│ - 50MB memory limit       │
│ - Restricted permissions  │
└───────────────────────────┘
    │
    ▼
┌───────────────────────────┐
│ Apply Results             │
│ - Damage dealt            │
│ - Status applied          │
│ - Log messages            │
└───────────────────────────┘
```

---

## Scripting Engine

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Scripting System                          │
│                                                              │
│  ┌───────────────────┐  ┌────────────────────────────────┐ │
│  │  ScriptCompiler   │  │  SandboxedScriptRunner         │ │
│  │                   │  │                                 │ │
│  │  - Parse C#       │  │  - Timeout enforcement         │ │
│  │  - Compile        │  │  - Memory monitoring           │ │
│  │  - Cache result   │  │  - Exception handling          │ │
│  └───────────────────┘  └────────────────────────────────┘ │
│           │                          │                      │
│           └──────────┬───────────────┘                      │
│                      ▼                                       │
│  ┌───────────────────────────────────────────────────────┐ │
│  │                   HookExecutor                         │ │
│  │                                                        │ │
│  │  - Find applicable hooks                               │ │
│  │  - Execute in order                                    │ │
│  │  - Handle failures gracefully                          │ │
│  └───────────────────────────────────────────────────────┘ │
│                      │                                       │
│                      ▼                                       │
│  ┌───────────────────────────────────────────────────────┐ │
│  │                   HookRegistry                         │ │
│  │                                                        │ │
│  │  - Register hooks from status effects                  │ │
│  │  - Track hook ownership                                │ │
│  │  - Clean up expired hooks                              │ │
│  └───────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Script Globals

```csharp
public class ScriptGlobals
{
    // Character references
    public Character Player { get; set; }
    public Character Opponent { get; set; }

    // Battle state
    public BattleState Battle { get; set; }

    // Card context
    public Card? SourceCard { get; set; }

    // Utilities
    public Random Random { get; set; }

    // API Methods
    public void DealDamage(Character target, int amount) { }
    public void Heal(Character target, int amount) { }
    public void ApplyStatus(Character target, string name, int duration) { }
    public void RemoveStatus(string name) { }
    public void DrawCards(Character target, int count) { }
    public void Log(string message) { }
}
```

### Safety Measures

| Protection | Implementation |
|------------|----------------|
| Timeout | CancellationToken after 500ms |
| Memory | Memory limit monitoring |
| No I/O | Sandboxed execution |
| No reflection | Restricted assemblies |
| Error isolation | Try-catch wrapper |

---

## Database Layer

### Repository Pattern

```
┌─────────────────────────────────────────────────────────────┐
│                    Repository Interfaces                     │
│                    (SuperDeck.Core)                          │
│                                                              │
│  ┌────────────────────────┐  ┌──────────────────────────┐  │
│  │ ICharacterRepository   │  │ IPlayerRepository        │  │
│  │                        │  │                          │  │
│  │ GetAsync(id)           │  │ GetAsync(id)             │  │
│  │ GetAllAsync(playerId)  │  │ GetByUsernameAsync()     │  │
│  │ CreateAsync(char)      │  │ CreateAsync(player)      │  │
│  │ UpdateAsync(char)      │  │ UpdateAsync(player)      │  │
│  │ DeleteAsync(id)        │  │                          │  │
│  └────────────────────────┘  └──────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┴───────────────┐
              ▼                               ▼
┌─────────────────────────┐    ┌─────────────────────────┐
│ SQLite Implementation    │    │ MariaDB Implementation   │
│ (SuperDeck.Server)       │    │ (SuperDeck.Server)       │
│                          │    │                          │
│ SQLiteCharacterRepository│    │ MariaDBCharacterRepository│
│ SQLitePlayerRepository   │    │ MariaDBPlayerRepository  │
│ SQLiteGhostRepository    │    │ MariaDBGhostRepository   │
└─────────────────────────┘    └─────────────────────────┘
```

### Schema Design

```sql
-- Characters table
CREATE TABLE Characters (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Level INTEGER DEFAULT 1,
    XP INTEGER DEFAULT 0,
    Attack INTEGER DEFAULT 0,
    Defense INTEGER DEFAULT 0,
    Speed INTEGER DEFAULT 0,
    DeckCardIds TEXT,          -- JSON array
    Wins INTEGER DEFAULT 0,
    Losses INTEGER DEFAULT 0,
    MMR INTEGER DEFAULT 1000,
    IsGhost INTEGER DEFAULT 0,
    IsPublished INTEGER DEFAULT 0,
    OwnerPlayerId TEXT,
    CreatedAt TEXT,
    LastModified TEXT
);

-- Ghost snapshots for AI opponents
CREATE TABLE GhostSnapshots (
    Id TEXT PRIMARY KEY,
    CharacterId TEXT NOT NULL,
    SnapshotData TEXT NOT NULL,  -- JSON serialized Character
    MMR INTEGER NOT NULL,
    CreatedAt TEXT
);

-- Indexes for performance
CREATE INDEX idx_characters_mmr ON Characters(MMR);
CREATE INDEX idx_characters_owner ON Characters(OwnerPlayerId);
CREATE INDEX idx_ghosts_mmr ON GhostSnapshots(MMR);
```

---

## API Design

### Endpoint Structure

```
/api
├── /auth
│   ├── POST /register      # Create account
│   ├── POST /login         # Login
│   ├── POST /logout        # Logout
│   └── GET  /me            # Current player info
│
├── /characters
│   ├── GET  /              # List characters
│   ├── POST /              # Create character
│   ├── GET  /{id}          # Get character
│   ├── PUT  /{id}/stats    # Allocate stats
│   ├── DELETE /{id}        # Delete character
│   ├── POST /{id}/cards    # Add cards to deck
│   └── DELETE /{id}/cards  # Remove cards
│
├── /cards
│   ├── GET  /              # List all cards
│   ├── GET  /{id}          # Get card
│   ├── GET  /suit/{suit}   # Cards by suit
│   └── GET  /starterpack/{suit}  # Starter cards
│
├── /battle
│   ├── POST /start         # Start battle
│   ├── POST /{id}/action   # Submit action
│   ├── GET  /{id}/state    # Get state
│   ├── POST /{id}/forfeit  # Forfeit
│   └── POST /{id}/finalize # End battle
│
├── /packs
│   └── POST /generate      # Generate booster
│
└── /health                 # Health check
```

### Response Format

**Success:**
```json
{
  "data": { ... },
  "success": true
}
```

**Error:**
```json
{
  "error": "Error message",
  "success": false
}
```

### Authentication

Two modes supported:

**Session-based (default):**
- Token stored in memory
- `Authorization: Bearer {token}`

**JWT (production):**
- Signed JWT token
- Configurable expiration
- `Authorization: Bearer {jwt}`

---

## Client Architecture

### Component Structure

```
┌─────────────────────────────────────────────────────────────┐
│                    Client Application                        │
│                                                              │
│  ┌───────────────────────────────────────────────────────┐ │
│  │                    Program.cs                          │ │
│  │                    (Entry Point)                       │ │
│  └───────────────────────────────────────────────────────┘ │
│                          │                                   │
│                          ▼                                   │
│  ┌───────────────────────────────────────────────────────┐ │
│  │                    GameRunner                          │ │
│  │                    (Main Loop)                         │ │
│  │                                                        │ │
│  │  - Menu navigation                                     │ │
│  │  - Character management                                │ │
│  │  - Battle orchestration                                │ │
│  └───────────────────────────────────────────────────────┘ │
│              │                               │               │
│              ▼                               ▼               │
│  ┌───────────────────────┐    ┌────────────────────────┐  │
│  │       BattleUI        │    │       ApiClient        │  │
│  │                       │    │                        │  │
│  │  - Render battle      │    │  - HTTP requests       │  │
│  │  - Display cards      │    │  - JSON serialization  │  │
│  │  - Show status        │    │  - Error handling      │  │
│  └───────────────────────┘    └────────────────────────┘  │
│                                          │                  │
│                                          ▼                  │
│                         ┌────────────────────────────────┐ │
│                         │  EmbeddedServerManager         │ │
│                         │                                │ │
│                         │  - Launch local server         │ │
│                         │  - Process management          │ │
│                         │  - Output capture              │ │
│                         └────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Offline Mode Flow

```
1. User selects "Offline Mode"
        │
        ▼
2. EmbeddedServerManager.StartAsync()
   ├── Find server DLL/project
   ├── Launch as subprocess
   └── Wait for server ready
        │
        ▼
3. ApiClient connects to localhost:5000
        │
        ▼
4. Normal gameplay via HTTP API
        │
        ▼
5. On exit: EmbeddedServerManager.StopAsync()
   └── Graceful server shutdown
```

---

## Security Model

### Threat Model

| Threat | Mitigation |
|--------|------------|
| Cheating | Server-authoritative design |
| Script injection | Sandboxed execution |
| DoS | Rate limiting |
| Data tampering | Input validation |
| Brute force | Auth rate limits |

### Input Validation

All API inputs validated:
- Character names: 3-20 chars, alphanumeric
- Passwords: Min 6 chars
- Card IDs: Must exist in card library
- Battle actions: Must be valid for phase

### Script Sandboxing

Card scripts run in isolation:
- No file system access
- No network access
- No reflection
- Memory limited
- Time limited

### Rate Limiting

When enabled:
- Global: 100 requests/minute
- Auth: 5 attempts/minute
- Battle: Token bucket algorithm

---

## Performance Considerations

### Script Caching

Compiled scripts are cached:
```csharp
// First execution: compile and cache
// Subsequent: use cached delegate
private static ConcurrentDictionary<string, Func<...>> _scriptCache;
```

### Database Optimization

- Indexed columns for common queries
- Connection pooling
- Prepared statements via Dapper
- Lazy loading where appropriate

### Memory Management

- Battle states cleaned after completion
- Script memory monitored
- Disposed resources tracked

---

## Extensibility Points

### Adding New Card Types

1. Add to `CardType` enum
2. Update `CardService` validation
3. Add UI handling in `BattleUI`

### Adding New Hooks

1. Add to `HookType` enum
2. Add trigger point in `BattleService`
3. Update card schema documentation

### Adding New Suits

1. Add to `Suit` enum
2. Add weight in `suitweights.json`
3. Create cards in `ServerCards/`

### Custom Repositories

1. Implement repository interface
2. Register in DI container
3. Configure in `appsettings.json`

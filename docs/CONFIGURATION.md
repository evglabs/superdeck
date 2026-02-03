# SuperDeck Configuration Reference

Complete reference for all configuration options in SuperDeck.

## Table of Contents

- [Configuration Files](#configuration-files)
- [Game Settings](#game-settings)
- [Server Settings](#server-settings)
- [Database Settings](#database-settings)
- [Authentication Settings](#authentication-settings)
- [Rate Limiting](#rate-limiting)
- [Environment Variables](#environment-variables)
- [Configuration Examples](#configuration-examples)

---

## Configuration Files

### File Locations

| File | Location | Purpose |
|------|----------|---------|
| `appsettings.json` | `src/Server/` | Main configuration |
| `appsettings.Development.json` | `src/Server/` | Development overrides |
| `appsettings.Production.json` | `src/Server/` | Production overrides |
| `serversettings.json` | `src/Server/` | Server-specific settings |
| `suitweights.json` | `src/Server/` | Card suit drop rates |

### Configuration Hierarchy

Settings are loaded in this order (later overrides earlier):

1. `appsettings.json` (base)
2. `appsettings.{Environment}.json` (environment-specific)
3. Environment variables
4. Command-line arguments

---

## Game Settings

### Character Settings

Location: `appsettings.json` → `GameSettings.Character`

```json
{
  "GameSettings": {
    "Character": {
      "BaseHP": 100,
      "HPPerLevel": 10,
      "StartingAttack": 0,
      "StartingDefense": 0,
      "StartingSpeed": 0,
      "StatPointsPerLevel": 1,
      "MaxLevel": 10,
      "MinSpeed": 0
    }
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `BaseHP` | 100 | Starting HP at level 1 |
| `HPPerLevel` | 10 | HP gained per level |
| `StartingAttack` | 0 | Initial attack stat |
| `StartingDefense` | 0 | Initial defense stat |
| `StartingSpeed` | 0 | Initial speed stat |
| `StatPointsPerLevel` | 1 | Points to allocate per level |
| `MaxLevel` | 10 | Maximum character level |
| `MinSpeed` | 0 | Minimum speed value |

**HP Formula:** `MaxHP = BaseHP + (Level × HPPerLevel)`

### Battle Settings

Location: `appsettings.json` → `GameSettings.Battle`

```json
{
  "GameSettings": {
    "Battle": {
      "BaseQueueSlots": 3,
      "MaxQueueSlots": 5,
      "StartingHandSize": 5,
      "CardsDrawnPerTurn": 3,
      "MinDeckSize": 9,
      "SystemDamageStartRound": 10,
      "SystemDamageBase": 2,
      "DefaultOpponentDeckSize": 9,
      "GhostSearchRange": 200,
      "GhostCandidateCount": 10
    }
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `BaseQueueSlots` | 3 | Cards queued per round |
| `MaxQueueSlots` | 5 | Maximum queue with buffs |
| `StartingHandSize` | 5 | Cards drawn on round 1 |
| `CardsDrawnPerTurn` | 3 | Cards drawn per turn after round 1 |
| `MinDeckSize` | 9 | Minimum deck size |
| `SystemDamageStartRound` | 10 | Round when system damage begins |
| `SystemDamageBase` | 2 | Starting system damage |
| `DefaultOpponentDeckSize` | 9 | AI opponent deck size |
| `GhostSearchRange` | 200 | MMR range for ghost matching |
| `GhostCandidateCount` | 10 | Ghost candidates to consider |

**System Damage Formula:** Damage doubles each round after start (2, 4, 8, 16...)

### XP Settings

Location: `appsettings.json` → `GameSettings.XP`

```json
{
  "GameSettings": {
    "XP": {
      "BaseXPForLevelUp": 50,
      "XPIncreasePerLevel": 25,
      "XPForWin": 50,
      "XPForLoss": 25
    }
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `BaseXPForLevelUp` | 50 | XP needed for level 2 |
| `XPIncreasePerLevel` | 25 | Additional XP per level |
| `XPForWin` | 50 | XP earned on victory |
| `XPForLoss` | 25 | XP earned on defeat |

**Level XP Formula:** `XPNeeded = BaseXP + (CurrentLevel × XPIncreasePerLevel)`

### MMR Settings

Location: `appsettings.json` → `GameSettings.MMR`

```json
{
  "GameSettings": {
    "MMR": {
      "StartingMMR": 1000,
      "MMRGainOnWin": 25,
      "MMRLossOnLoss": 25,
      "MinimumMMR": 100
    }
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `StartingMMR` | 1000 | Initial MMR rating |
| `MMRGainOnWin` | 25 | MMR gained on win |
| `MMRLossOnLoss` | 25 | MMR lost on defeat |
| `MinimumMMR` | 100 | Lowest possible MMR |

### Card Pack Settings

Location: `appsettings.json` → `GameSettings.CardPack`

```json
{
  "GameSettings": {
    "CardPack": {
      "BoosterPackSize": 10,
      "StarterPackSize": 10,
      "StarterDeckPunchCount": 3,
      "StarterDeckBlockCount": 3,
      "RarityRollMax": 1000,
      "SuitBonusPerOwnedCard": 2.0
    }
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `BoosterPackSize` | 10 | Cards shown in booster |
| `StarterPackSize` | 10 | Cards in starter deck |
| `StarterDeckPunchCount` | 3 | Basic punch cards |
| `StarterDeckBlockCount` | 3 | Basic block cards |
| `RarityRollMax` | 1000 | Max value for rarity roll |
| `SuitBonusPerOwnedCard` | 2.0 | Weight bonus per owned card |

### Rarity Weights

Location: `appsettings.json` → `GameSettings.RarityWeights`

```json
{
  "GameSettings": {
    "RarityWeights": {
      "CommonThreshold": 600,
      "UncommonThreshold": 900,
      "RareThreshold": 990,
      "StarterCommonWeight": 50,
      "StarterUncommonWeight": 30,
      "StarterRareWeight": 15,
      "StarterEpicWeight": 4,
      "StarterLegendaryWeight": 1
    }
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `CommonThreshold` | 600 | Roll < 600 = Common (60%) |
| `UncommonThreshold` | 900 | Roll < 900 = Uncommon (30%) |
| `RareThreshold` | 990 | Roll < 990 = Rare (9%) |
| `StarterCommonWeight` | 50 | Starter pack common weight |
| `StarterUncommonWeight` | 30 | Starter pack uncommon weight |
| `StarterRareWeight` | 15 | Starter pack rare weight |
| `StarterEpicWeight` | 4 | Starter pack epic weight |
| `StarterLegendaryWeight` | 1 | Starter pack legendary weight |

**Rarity Distribution (booster):**
- Common: 60% (roll 0-599)
- Uncommon: 30% (roll 600-899)
- Rare: 9% (roll 900-989)
- Epic: 0.9% (roll 990-998)
- Legendary: 0.1% (roll 999)

---

## Server Settings

### Basic Server Settings

Location: `serversettings.json`

```json
{
  "BaseHP": 100,
  "HPPerLevel": 10,
  "ScriptTimeoutMs": 500,
  "InitialHandSize": 5,
  "DatabasePath": "superdeck.db",
  "CardLibraryPath": "Data/ServerCards"
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `ScriptTimeoutMs` | 500 | Script execution timeout |
| `DatabasePath` | superdeck.db | SQLite database path |
| `CardLibraryPath` | Data/ServerCards | Card JSON directory |

### Suit Weights

Location: `suitweights.json`

Controls card drop rates by suit:

```json
{
  "Basic": 20,
  "Fire": 20,
  "MartialArts": 20,
  "Magic": 20,
  "Electricity": 20,
  "Mental": 15,
  "Espionage": 15,
  "Nature": 15,
  "Tech": 15,
  "Berserker": 15,
  "Military": 15,
  "Radiation": 10,
  "Showbiz": 10,
  "Speedster": 10,
  "Money": 0.01
}
```

Higher weight = more likely to appear in packs.

---

## Database Settings

### Database Provider

Location: `appsettings.json`

```json
{
  "DatabaseProvider": "SQLite"
}
```

| Value | Description |
|-------|-------------|
| `SQLite` | Local file database (default) |
| `MariaDB` | Remote MySQL/MariaDB server |

### SQLite Configuration

```json
{
  "DatabaseProvider": "SQLite"
}
```

Database file: `src/Server/superdeck.db` (auto-created)

### MariaDB Configuration

```json
{
  "DatabaseProvider": "MariaDB",
  "ConnectionStrings": {
    "MariaDB": "Server=localhost;Database=superdeck;User=superdeck_user;Password=yourpassword"
  }
}
```

**Connection String Parameters:**

| Parameter | Description |
|-----------|-------------|
| `Server` | Database host |
| `Database` | Database name |
| `User` | Username |
| `Password` | Password |
| `Port` | Port (default: 3306) |
| `SslMode` | SSL mode (None, Preferred, Required) |

---

## Authentication Settings

Location: `appsettings.json` → `GameSettings.Auth`

```json
{
  "GameSettings": {
    "Auth": {
      "UsernameMinLength": 3,
      "UsernameMaxLength": 20,
      "PasswordMinLength": 6,
      "SessionTimeoutHours": 24,
      "SaltSizeBytes": 32,
      "TokenSizeBytes": 32,
      "UseJwt": false,
      "Jwt": {
        "Secret": "",
        "Issuer": "SuperDeck",
        "Audience": "SuperDeck",
        "ExpirationMinutes": 1440
      }
    }
  }
}
```

### Basic Auth Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `UsernameMinLength` | 3 | Minimum username length |
| `UsernameMaxLength` | 20 | Maximum username length |
| `PasswordMinLength` | 6 | Minimum password length |
| `SessionTimeoutHours` | 24 | Session expiry time |
| `SaltSizeBytes` | 32 | Password salt size |
| `TokenSizeBytes` | 32 | Session token size |

### JWT Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `UseJwt` | false | Enable JWT authentication |
| `Jwt.Secret` | "" | JWT signing key (min 64 chars) |
| `Jwt.Issuer` | SuperDeck | Token issuer |
| `Jwt.Audience` | SuperDeck | Token audience |
| `Jwt.ExpirationMinutes` | 1440 | Token lifetime (24 hours) |

**Production JWT Secret:**
```bash
# Generate a secure secret
openssl rand -base64 64
```

---

## Rate Limiting

Location: `appsettings.json` → `GameSettings.RateLimit`

```json
{
  "GameSettings": {
    "RateLimit": {
      "Enabled": false,
      "GlobalPermitLimit": 100,
      "GlobalWindowSeconds": 60,
      "Auth": {
        "PermitLimit": 5,
        "WindowSeconds": 60
      },
      "Battle": {
        "TokenLimit": 30,
        "TokensPerPeriod": 10,
        "ReplenishmentPeriodSeconds": 10
      }
    }
  }
}
```

### Global Rate Limit

| Setting | Default | Description |
|---------|---------|-------------|
| `Enabled` | false | Enable rate limiting |
| `GlobalPermitLimit` | 100 | Max requests per window |
| `GlobalWindowSeconds` | 60 | Window duration |

### Auth Rate Limit

| Setting | Default | Description |
|---------|---------|-------------|
| `PermitLimit` | 5 | Login attempts per window |
| `WindowSeconds` | 60 | Window duration |

### Battle Rate Limit (Token Bucket)

| Setting | Default | Description |
|---------|---------|-------------|
| `TokenLimit` | 30 | Maximum tokens |
| `TokensPerPeriod` | 10 | Tokens added per period |
| `ReplenishmentPeriodSeconds` | 10 | Token refill interval |

---

## Environment Variables

Override any setting via environment variable:

### Naming Convention

Replace `.` with `__` and `:` with `__`:

```bash
# appsettings.json: GameSettings.Character.BaseHP = 100
export GameSettings__Character__BaseHP=150

# ConnectionStrings.MariaDB
export ConnectionStrings__MariaDB="Server=..."
```

### Common Variables

| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Environment name |
| `ASPNETCORE_URLS` | Server listening URLs |
| `DatabaseProvider` | Database type |
| `ConnectionStrings__MariaDB` | DB connection string |

### ASP.NET Core URLs

```bash
# Single URL
export ASPNETCORE_URLS=http://localhost:5000

# Multiple URLs
export ASPNETCORE_URLS="http://localhost:5000;https://localhost:5001"

# All interfaces
export ASPNETCORE_URLS=http://0.0.0.0:5000
```

---

## Configuration Examples

### Development Configuration

`appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "DatabaseProvider": "SQLite",
  "GameSettings": {
    "RateLimit": {
      "Enabled": false
    }
  }
}
```

### Production Configuration

`appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "DatabaseProvider": "MariaDB",
  "ConnectionStrings": {
    "MariaDB": "Server=db.example.com;Database=superdeck;User=superdeck;Password=SECURE_PASSWORD"
  },
  "GameSettings": {
    "Auth": {
      "UseJwt": true,
      "Jwt": {
        "Secret": "YOUR_64_CHARACTER_SECRET_KEY_GENERATED_WITH_OPENSSL_RAND_BASE64",
        "ExpirationMinutes": 1440
      }
    },
    "RateLimit": {
      "Enabled": true,
      "GlobalPermitLimit": 100,
      "GlobalWindowSeconds": 60
    }
  }
}
```

### Easy Mode Configuration

Easier game for new players:

```json
{
  "GameSettings": {
    "Character": {
      "BaseHP": 150,
      "HPPerLevel": 15,
      "StatPointsPerLevel": 2
    },
    "XP": {
      "XPForWin": 100,
      "XPForLoss": 50
    },
    "Battle": {
      "StartingHandSize": 7,
      "CardsDrawnPerTurn": 4
    }
  }
}
```

### Hard Mode Configuration

Challenging game for experienced players:

```json
{
  "GameSettings": {
    "Character": {
      "BaseHP": 80,
      "HPPerLevel": 5,
      "StatPointsPerLevel": 1
    },
    "XP": {
      "XPForWin": 30,
      "XPForLoss": 10,
      "XPIncreasePerLevel": 50
    },
    "Battle": {
      "StartingHandSize": 4,
      "CardsDrawnPerTurn": 2,
      "SystemDamageStartRound": 8
    }
  }
}
```

### Minimum Viable Configuration

For testing with defaults:

```json
{
  "DatabaseProvider": "SQLite"
}
```

All other settings use sensible defaults.

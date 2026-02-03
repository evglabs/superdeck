# Character Seeder

Command-line tool that generates ghost characters (AI opponents) and seeds them into the database. Ghost characters provide offline opponents at various difficulty levels with suit-appropriate decks and plausible stats.

Supports both SQLite (offline mode) and MariaDB (online mode).

## Table of Contents

- [Usage](#usage)
- [Options](#options)
- [Examples](#examples)
- [Stat Profiles](#stat-profiles)
- [Deck Configuration](#deck-configuration)
- [How It Works](#how-it-works)

---

## Usage

```bash
dotnet run --project src/Tools/SuperDeck.Tools.CharacterSeeder -- --name <name> --suit <suit> [options]
```

---

## Options

| Option | Required | Default | Description |
|--------|----------|---------|-------------|
| `--name` | Yes | | Base character name (e.g., "Blaze") |
| `--suit` | Yes | | Character suit (case-insensitive) |
| `--provider` | No | `sqlite` | Database provider: `sqlite` or `mariadb` |
| `--database` | No | `superdeck.db` | SQLite database path (used when provider is sqlite) |
| `--connection-string` | No | | MariaDB connection string (required when provider is mariadb) |
| `--level` | No | All (1-10) | Specific level to generate |
| `--cards-path` | No | `Data/ServerCards` | Path to card JSON directory |
| `--verbose` | No | `false` | Show detailed output |

### Valid Suits

Berserker, Electricity, Espionage, Fire, Magic, MartialArts, Mental, Military, Money, Nature, Radiation, Showbiz, Speedster, Tech

Basic suit is not allowed.

---

## Examples

### SQLite (default)

Generate all levels (1-10) for a Fire character:

```bash
dotnet run --project src/Tools/SuperDeck.Tools.CharacterSeeder -- \
  --name Blaze --suit Fire --database src/Server/superdeck.db
```

Generate a single level 5 Berserker:

```bash
dotnet run --project src/Tools/SuperDeck.Tools.CharacterSeeder -- \
  --name Warrior --suit Berserker --level 5 --database src/Server/superdeck.db
```

### MariaDB

Seed into a production MariaDB database:

```bash
dotnet run --project src/Tools/SuperDeck.Tools.CharacterSeeder -- \
  --name Blaze --suit Fire \
  --provider mariadb \
  --connection-string "Server=localhost;Database=superdeck;User=superdeck_user;Password=yourpassword"
```

Single level with verbose output:

```bash
dotnet run --project src/Tools/SuperDeck.Tools.CharacterSeeder -- \
  --name Shadow --suit Espionage --level 7 --verbose \
  --provider mariadb \
  --connection-string "Server=db.example.com;Database=superdeck;User=superdeck;Password=secret"
```

### Batch Seeding

Seed a full roster of opponents:

```bash
for suit in Fire Berserker Magic Speedster Mental Electricity Military; do
  dotnet run --project src/Tools/SuperDeck.Tools.CharacterSeeder -- \
    --name "$suit" --suit "$suit" --database src/Server/superdeck.db
done
```

---

## Stat Profiles

Each suit has a stat distribution profile that determines how points are allocated across ATK, DEF, and SPD.

| Profile | Suits | ATK | DEF | SPD |
|---------|-------|-----|-----|-----|
| Aggressive | Berserker, Fire, Military | 50% | 10% | 40% |
| Defensive | Magic, Mental | 20% | 50% | 30% |
| Speedster | Speedster, Electricity | 30% | 10% | 60% |
| Balanced | All others | 35% | 30% | 35% |

Total stat points per level: `(level x 2) + 5`

---

## Deck Configuration

Deck size and rarity distribution scale with level:

| Levels | Deck Size | Common | Uncommon | Rare | Epic | Legendary |
|--------|-----------|--------|----------|------|------|-----------|
| 1-3 | 10 | 80% | 20% | - | - | - |
| 4-6 | 12 | 50% | 35% | 15% | - | - |
| 7-9 | 13 | 30% | 35% | 25% | 10% | - |
| 10 | 15 | 20% | 30% | 30% | 15% | 5% |

Cards are selected from the specified suit plus Basic suit.

---

## How It Works

### Character ID Format

```
ghost_{name}_{suit}_lv{level}
```

All lowercase. Example: `ghost_blaze_fire_lv5`

Running the tool again with the same name/suit/level updates the existing character (upsert).

### MMR Calculation

```
MMR = 700 + (level x 80) +/- random(30)
```

| Level | Approximate MMR |
|-------|----------------|
| 1 | ~780 |
| 5 | ~1100 |
| 10 | ~1500 |

### Win/Loss Record

Generated to look plausible based on MMR:
- Total games: `(level x 10) + random(5-20)`
- Win rate: 30%-80%, correlated with MMR

### Database

- Supports SQLite (`--provider sqlite`) and MariaDB (`--provider mariadb`)
- Auto-creates the Characters table and indexes if missing
- Uses upsert: re-running with the same name/suit safely updates existing records
- Deck is stored as a JSON array of card IDs
- MariaDB schema matches the server's schema (VARCHAR types, TINYINT booleans, DATETIME timestamps)

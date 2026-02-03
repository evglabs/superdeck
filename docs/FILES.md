# SuperDeck File Locations

Complete reference for where SuperDeck stores files in both offline and online modes.

## Table of Contents

- [Project Structure](#project-structure)
- [Offline Mode Files](#offline-mode-files)
- [Online Mode Files](#online-mode-files)
- [Configuration Files](#configuration-files)
- [Card Data Files](#card-data-files)
- [Database Files](#database-files)
- [Log Files](#log-files)
- [Build Artifacts](#build-artifacts)

---

## Project Structure

### Complete Directory Layout

```
superdeck/
├── SuperDeck.slnx                    # Solution file
├── run_client.sh                     # Quick start script
├── design_doc_v2.md                  # Game design document
├── implementation_plan.md            # Development roadmap
│
├── docs/                             # Documentation
│   ├── DEPLOYMENT.md                 # Deployment guide
│   ├── PLAYING.md                    # How to play
│   ├── MODDING.md                    # Modding guide
│   ├── CONFIGURATION.md              # Configuration reference
│   ├── ARCHITECTURE.md               # Technical architecture
│   ├── API.md                        # API reference
│   └── FILES.md                      # This file
│
├── src/
│   ├── Core/                         # Shared domain library
│   │   ├── Models/                   # Domain entities
│   │   │   ├── Enums/                # Game enumerations
│   │   │   ├── Card.cs
│   │   │   ├── Character.cs
│   │   │   ├── BattleState.cs
│   │   │   └── StatusEffect.cs
│   │   ├── Scripting/                # Roslyn scripting engine
│   │   ├── Settings/                 # Configuration classes
│   │   └── Data/Repositories/        # Repository interfaces
│   │
│   ├── Server/                       # ASP.NET Core server
│   │   ├── Program.cs                # API endpoints
│   │   ├── appsettings.json          # Main configuration
│   │   ├── appsettings.Development.json
│   │   ├── serversettings.json       # Server-specific config
│   │   ├── suitweights.json          # Card drop rates
│   │   ├── superdeck.db              # SQLite database (created at runtime)
│   │   │
│   │   ├── Services/                 # Business logic
│   │   │   ├── BattleService.cs
│   │   │   ├── CardService.cs
│   │   │   ├── CharacterService.cs
│   │   │   ├── BoosterPackService.cs
│   │   │   ├── AuthService.cs
│   │   │   └── AIBehaviorService.cs
│   │   │
│   │   └── Data/
│   │       ├── schema.sql            # Database schema
│   │       ├── Repositories/         # SQLite implementations
│   │       ├── Repositories/MariaDB/ # MariaDB implementations
│   │       └── ServerCards/          # Card JSON files (94 cards)
│   │
│   ├── Client/                       # Console client
│   │   ├── Program.cs                # Entry point
│   │   ├── UI/
│   │   │   ├── GameRunner.cs         # Main game loop
│   │   │   └── BattleUI.cs           # Battle display
│   │   └── Networking/
│   │       ├── ApiClient.cs          # HTTP client
│   │       └── EmbeddedServerManager.cs  # Local server launcher
│   │
│   └── Tools/                        # Utility projects
│       └── SuperDeck.Tools.CharacterSeeder/
│
└── tests/
    └── Tests/                        # Unit tests
        ├── Tools/
        └── SuperDeck.Tests.csproj
```

---

## Offline Mode Files

When running in offline mode, all files are stored locally.

### Runtime Files

| File | Location | Description |
|------|----------|-------------|
| Database | `src/Server/superdeck.db` | SQLite database (auto-created) |
| Server DLL | `src/Server/bin/Debug/net10.0/` | Compiled server |
| Client DLL | `src/Client/bin/Debug/net10.0/` | Compiled client |

### Data Flow (Offline)

```
superdeck/
├── src/Server/
│   ├── superdeck.db              ← All game data stored here
│   │   ├── Characters table
│   │   ├── Players table
│   │   ├── GhostSnapshots table
│   │   └── AIProfiles table
│   │
│   └── Data/ServerCards/         ← Card definitions loaded from here
│       └── *.json
│
└── src/Client/
    └── (no persistent storage)   ← Client is stateless
```

### Database Location

**Default:** `src/Server/superdeck.db`

**Custom location:** Set in `serversettings.json`:
```json
{
  "DatabasePath": "/custom/path/superdeck.db"
}
```

### Card Data Location

**Default:** `src/Server/Data/ServerCards/`

**Custom location:** Set in `serversettings.json`:
```json
{
  "CardLibraryPath": "/custom/path/cards"
}
```

---

## Online Mode Files

When running against a remote server, files are split between client and server.

### Client-Side (Local)

| File | Location | Description |
|------|----------|-------------|
| Client binary | `src/Client/bin/` | Compiled client |
| (no database) | - | All data on server |

The client stores nothing persistently - all state comes from the server.

### Server-Side (Remote)

| File | Location | Description |
|------|----------|-------------|
| Server binary | `/opt/superdeck/` | Published server |
| Configuration | `/opt/superdeck/appsettings.json` | Server config |
| Card data | `/opt/superdeck/Data/ServerCards/` | Card definitions |
| Database | MariaDB server | All game data |

### Data Flow (Online)

```
┌─────────────────────────────────────────────────────────┐
│                    Client Machine                        │
│                                                          │
│  src/Client/                                             │
│  └── bin/Debug/net10.0/SuperDeck.Client.dll             │
│                                                          │
│  (No persistent data - all fetched from server)         │
└─────────────────────────────────────────────────────────┘
                            │
                            │ HTTP/JSON
                            ▼
┌─────────────────────────────────────────────────────────┐
│                    Server Machine                        │
│                                                          │
│  /opt/superdeck/                                         │
│  ├── SuperDeck.Server.dll                               │
│  ├── appsettings.json                                   │
│  ├── appsettings.Production.json                        │
│  ├── serversettings.json                                │
│  ├── suitweights.json                                   │
│  └── Data/ServerCards/*.json                            │
│                                                          │
│  MariaDB Database                                        │
│  └── superdeck database                                  │
│      ├── Characters                                      │
│      ├── Players                                         │
│      ├── GhostSnapshots                                  │
│      └── AIProfiles                                      │
└─────────────────────────────────────────────────────────┘
```

---

## Configuration Files

### Primary Configuration

| File | Location | Purpose |
|------|----------|---------|
| `appsettings.json` | `src/Server/` | Main game configuration |
| `appsettings.Development.json` | `src/Server/` | Development overrides |
| `appsettings.Production.json` | `src/Server/` | Production overrides |
| `serversettings.json` | `src/Server/` | Server-specific settings |
| `suitweights.json` | `src/Server/` | Card drop rate weights |

### Configuration Loading Order

1. `appsettings.json` (base)
2. `appsettings.{Environment}.json` (environment-specific)
3. Environment variables
4. Command-line arguments

### Environment-Specific Files

**Development:**
```
src/Server/appsettings.Development.json
```

**Production:**
```
src/Server/appsettings.Production.json
# Or deployed location:
/opt/superdeck/appsettings.Production.json
```

---

## Card Data Files

### Location

```
src/Server/Data/ServerCards/
├── basic_block.json
├── basic_kick.json
├── basic_punch.json
├── fire_ember.json
├── fire_fireball.json
├── fire_firebolt.json
├── fire_phoenix.json
├── magic_arcane_bolt.json
├── ... (94 total cards)
```

### Naming Convention

```
{suit}_{cardname}.json

Examples:
- fire_fireball.json
- tech_laserbeam.json
- mental_mindswap.json
```

### Card File Format

```json
{
  "id": "fire_fireball",
  "name": "Fireball",
  "suit": "Fire",
  "type": "Attack",
  "rarity": "Common",
  "description": "Deal 15 damage",
  "immediateEffect": {
    "target": "Opponent",
    "script": "DealDamage(Opponent, 15);"
  }
}
```

### Adding New Cards

1. Create new JSON file in `src/Server/Data/ServerCards/`
2. Follow naming convention: `{suit}_{name}.json`
3. Restart server to load new cards

---

## Database Files

### SQLite (Offline)

**Location:** `src/Server/superdeck.db`

**Contents:**
- Characters table
- Players table
- GhostSnapshots table
- AIProfiles table

**Backup:**
```bash
cp src/Server/superdeck.db backups/superdeck_$(date +%Y%m%d).db
```

**Reset:**
```bash
rm src/Server/superdeck.db
# Restart server - database will be recreated
```

### MariaDB (Online)

**Location:** Configured MariaDB server

**Connection:** Specified in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "MariaDB": "Server=localhost;Database=superdeck;User=user;Password=pass"
  }
}
```

### Schema File

**Location:** `src/Server/Data/schema.sql`

Used to initialize database on first run or for manual setup.

---

## Log Files

### Console Output

By default, logs go to console (stdout).

### File Logging

Configure in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Systemd Logs (Production)

```bash
# View logs
sudo journalctl -u superdeck -f

# Export logs
sudo journalctl -u superdeck --since "1 hour ago" > superdeck.log
```

### Debug Files

| File | Location | Created When |
|------|----------|--------------|
| `battle_debug.json` | `src/Server/` | JSON parse errors |
| `battle_response_debug.json` | `src/Server/` | Battle debugging |

---

## Build Artifacts

### Debug Build

```
src/Core/bin/Debug/net10.0/
├── SuperDeck.Core.dll
└── SuperDeck.Core.pdb

src/Server/bin/Debug/net10.0/
├── SuperDeck.Server.dll
├── SuperDeck.Server.pdb
├── appsettings.json
├── Data/
│   └── ServerCards/
└── [dependencies]

src/Client/bin/Debug/net10.0/
├── SuperDeck.Client.dll
├── SuperDeck.Client.pdb
└── [dependencies]
```

### Release Build

```
src/Server/bin/Release/net10.0/
├── SuperDeck.Server.dll
├── appsettings.json
└── Data/ServerCards/
```

### Published Output

```bash
dotnet publish src/Server -c Release -o /opt/superdeck/publish
```

Creates:
```
/opt/superdeck/publish/
├── SuperDeck.Server.dll
├── SuperDeck.Core.dll
├── appsettings.json
├── Data/
│   ├── schema.sql
│   └── ServerCards/*.json
└── [all dependencies]
```

---

## File Permissions

### Required Permissions

| Path | Permission | Purpose |
|------|------------|---------|
| Server directory | Read + Execute | Run application |
| `superdeck.db` | Read + Write | SQLite database |
| `Data/ServerCards/` | Read | Load cards |
| `appsettings*.json` | Read | Configuration |
| Log directory | Write | If file logging enabled |

### Production Setup

```bash
# Set ownership
sudo chown -R www-data:www-data /opt/superdeck

# Set permissions
sudo chmod 755 /opt/superdeck
sudo chmod 644 /opt/superdeck/*.json
sudo chmod 755 /opt/superdeck/SuperDeck.Server.dll
```

---

## Backup Strategy

### What to Backup

| Priority | Files | Frequency |
|----------|-------|-----------|
| Critical | Database (SQLite/MariaDB) | Daily |
| Important | `appsettings*.json` | After changes |
| Important | Custom cards in `ServerCards/` | After changes |
| Optional | Build artifacts | Never (rebuild) |

### Backup Script

```bash
#!/bin/bash
BACKUP_DIR=/backups/superdeck
DATE=$(date +%Y%m%d_%H%M%S)

mkdir -p $BACKUP_DIR

# SQLite backup
if [ -f src/Server/superdeck.db ]; then
    cp src/Server/superdeck.db $BACKUP_DIR/superdeck_$DATE.db
fi

# Configuration backup
tar -czf $BACKUP_DIR/config_$DATE.tar.gz \
    src/Server/appsettings*.json \
    src/Server/serversettings.json \
    src/Server/suitweights.json

# Custom cards backup
tar -czf $BACKUP_DIR/cards_$DATE.tar.gz \
    src/Server/Data/ServerCards/

# Keep last 7 days
find $BACKUP_DIR -mtime +7 -delete
```

---

## Troubleshooting File Issues

### Database not found

```
Error: Could not open database file
```

**Solution:** Ensure the server has write access to create `superdeck.db`:
```bash
touch src/Server/superdeck.db
chmod 664 src/Server/superdeck.db
```

### Cards not loading

```
Warning: No cards loaded
```

**Solution:** Verify card path:
```bash
ls src/Server/Data/ServerCards/
# Should show .json files
```

### Permission denied

```
Error: Access to the path is denied
```

**Solution:** Fix ownership:
```bash
sudo chown -R $(whoami) src/Server/
```

### Configuration not found

```
Error: Could not find appsettings.json
```

**Solution:** Run from correct directory or specify path:
```bash
cd src/Server
dotnet run
```

# SuperDeck

A deck-building superhero card game with fully moddable cards powered by runtime C# scripting.

## Features

- **94 unique cards** across 15 themed suits
- **Runtime scripting** - Card effects compile at runtime via Roslyn
- **Fully moddable** - Create custom cards with JSON + C# scripts
- **Offline & Online** - Same codebase supports both modes
- **Server-authoritative** - All game logic runs server-side
- **Progression system** - Level up, earn XP, climb MMR rankings

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/) (for web client only)

### Run the Game

#### Console Client

```bash
# Clone the repository
git clone <repository-url> superdeck
cd superdeck

# Build and run
./run_client.sh
```

Select **"Offline (Local Server)"** to start playing immediately.

#### Web Client

```bash
# Terminal 1: Start the server
dotnet run --project src/Server

# Terminal 2: Start the web client
./run_webclient.sh
```

Open `http://localhost:5173` in your browser.

## Game Overview

SuperDeck is a strategic card battle game where you:

1. **Create a character** and choose a starting suit
2. **Build your deck** from 94 unique cards
3. **Battle opponents** using a queue-based combat system
4. **Level up** to gain stats and new cards
5. **Climb the ranks** with the MMR matchmaking system

### Battle System

Each battle round consists of:
- **Draw Phase** - Draw cards from your deck
- **Queue Phase** - Select 3 cards to play
- **Resolution Phase** - Cards resolve based on speed
- **Win** by reducing opponent HP to 0

### Suits

| Suit | Theme | Playstyle |
|------|-------|-----------|
| Fire | Destruction | Burst damage, DoT |
| MartialArts | Combat | Combos, counters |
| Magic | Mystical | Versatile effects |
| Electricity | Energy | Speed, disruption |
| Mental | Psychic | Control, manipulation |
| + 10 more suits | | |

## Documentation

| Document | Description |
|----------|-------------|
| [How to Play](docs/PLAYING.md) | Game mechanics, controls, strategies |
| [Deployment Guide](docs/DEPLOYMENT.md) | Server setup for dev and production |
| [Modding Guide](docs/MODDING.md) | Create custom cards and effects |
| [Configuration](docs/CONFIGURATION.md) | All configuration options |
| [Architecture](docs/ARCHITECTURE.md) | Technical system design |
| [API Reference](docs/API.md) | REST API documentation |
| [File Locations](docs/FILES.md) | Where data is stored |
| [Web Client](docs/WEBCLIENT.md) | Web client setup, deployment, architecture |
| [Client Setup](docs/CLIENT-SETUP.md) | Dedicated console client machine setup |

## Project Structure

```
superdeck/
├── src/
│   ├── Core/           # Shared domain models & scripting engine
│   ├── Server/         # ASP.NET Core API server
│   ├── Client/         # Spectre.Console CLI client
│   ├── WebClient/      # React browser client
│   └── Tools/          # Utility projects
├── tests/              # Unit tests
├── docs/               # Documentation
└── design_doc_v2.md    # Game design document
```

## Technology Stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 10 |
| Server | ASP.NET Core Minimal API |
| Console Client | Spectre.Console |
| Web Client | React 19 + TypeScript + Vite |
| Scripting | Roslyn C# Scripting |
| Database | SQLite (offline) / MariaDB (online) |
| ORM | Dapper |

## Configuration

Game settings are in `src/Server/appsettings.json`:

```json
{
  "GameSettings": {
    "Character": {
      "BaseHP": 100,
      "StatPointsPerLevel": 1
    },
    "Battle": {
      "StartingHandSize": 5,
      "CardsDrawnPerTurn": 3
    }
  }
}
```

See [Configuration Reference](docs/CONFIGURATION.md) for all options.

## Modding

Create custom cards by adding JSON files to `src/Server/Data/ServerCards/`:

```json
{
  "id": "custom_blast",
  "name": "Custom Blast",
  "suit": "Fire",
  "type": "Attack",
  "rarity": "Rare",
  "description": "Deal 25 damage",
  "immediateEffect": {
    "target": "Opponent",
    "script": "DealDamage(Opponent, 25);"
  }
}
```

See [Modding Guide](docs/MODDING.md) for the complete scripting API.

## Development

### Building

```bash
dotnet build SuperDeck.slnx
```

### Running Tests

```bash
dotnet test
```

### Running Server Separately

```bash
# Terminal 1: Server
dotnet run --project src/Server

# Terminal 2: Client
dotnet run --project src/Client
# Choose "Online" mode, enter http://localhost:5000
```

## Deployment

### Docker

```bash
docker-compose up -d
```

### Manual

```bash
dotnet publish src/Server -c Release -o /opt/superdeck
```

See [Deployment Guide](docs/DEPLOYMENT.md) for complete instructions.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests
5. Submit a pull request

## License

[Add your license here]

## Acknowledgments

- Roslyn team for the C# scripting engine
- Spectre.Console for the terminal UI framework

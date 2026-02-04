# SuperDeck Web Client

A browser-based client for SuperDeck with full feature parity to the console client. Built with React 19, TypeScript, and Vite.

## Table of Contents

- [Quick Start](#quick-start)
- [Features](#features)
- [Architecture](#architecture)
- [Development](#development)
- [Deployment](#deployment)
- [Project Structure](#project-structure)
- [Troubleshooting](#troubleshooting)

---

## Quick Start

### Prerequisites

| Software | Version | Purpose |
|----------|---------|---------|
| Node.js | 18+ | JavaScript runtime |
| npm | 9+ | Package manager (bundled with Node.js) |

The SuperDeck server must be running separately (see [DEPLOYMENT.md](DEPLOYMENT.md)).

### Run in Development

```bash
# Terminal 1: Start the server
dotnet run --project src/Server

# Terminal 2: Start the web client
./run_webclient.sh

# Or manually:
cd src/WebClient
npm install
npm run dev
```

Open `http://localhost:5173` in your browser. The Vite dev server proxies `/api/*` requests to the .NET server at `localhost:5000`.

---

## Features

The web client provides the same gameplay experience as the console client:

- **Authentication** — Register, login, logout with JWT or session tokens
- **Character Management** — Create characters, choose suits, pick starter cards
- **Deck Building** — View deck, inspect card details, remove cards (min 6 enforced)
- **Stat Allocation** — Distribute points across HP, Attack, Defense, Speed
- **Battle System** — Full queue-based combat with card selection, auto-battle, forfeit
- **Battle Resolution** — Live polling during resolution phases with animated HP bars
- **Level Up Flow** — Booster pack selection (3-action budget), stat point allocation
- **Revealed Info** — Opponent queue/hand visibility from Snoop/Mind Read effects
- **Status Effects** — Buff/debuff badges with duration tracking

### Visual Design

Dark card-game UI theme:

- Dark navy backgrounds with colored card borders by type (red=attack, blue=defense, green=buff, purple=debuff)
- Card names colored by rarity (grey=common, green=uncommon, blue=rare, purple=epic, gold=legendary)
- Smooth HP bar transitions with green/yellow/red color thresholds
- Color-coded battle log entries
- Hover effects on cards with subtle elevation

---

## Architecture

The web client is a standalone SPA (Single Page Application) that communicates with the SuperDeck server via its REST API. It is entirely separate from the server — the server has no knowledge of the web client.

```
Browser (localhost:5173)  ──── /api/* ────>  Server (localhost:5000)
       React SPA                              ASP.NET Core API
```

### Tech Stack

| Component | Technology | Rationale |
|-----------|------------|-----------|
| Framework | React 19 | Stateful interactive UI |
| Language | TypeScript | Type safety matching C# models |
| Build Tool | Vite | Fast dev server, proxy config |
| Routing | react-router-dom | Client-side SPA routing |
| State | React Context + useReducer | Two domains (auth + game) |
| Styling | CSS custom properties | Scoped color system |

### State Management

Two React Contexts manage application state:

- **AuthContext** — Token, playerId, username stored in localStorage
- **GameContext** — Server settings, current character reference

Battle state uses a dedicated `useBattle` hook with `useReducer` for the complex state machine (polling, auto-battle, queue management).

---

## Development

### Install Dependencies

```bash
cd src/WebClient
npm install
```

### Dev Server

```bash
npm run dev
```

Starts at `http://localhost:5173` with hot module replacement. API calls are proxied to `http://localhost:5000`.

### Type Check

```bash
npx tsc -b
```

### Production Build

```bash
npm run build
```

Output goes to `src/WebClient/dist/`.

---

## Deployment

The web client builds to static files (HTML, CSS, JS) that can be served by any web server or CDN.

### Option A: Build and Serve Locally

```bash
# Build
./deploy_webclient.sh

# Build and preview
./deploy_webclient.sh --serve
```

The preview server runs at `http://localhost:4173`.

### Option B: Deploy to a Web Server

```bash
# Build and copy to a target directory
./deploy_webclient.sh --target /var/www/superdeck

# Or manually
cd src/WebClient
npm run build
cp -r dist/* /var/www/superdeck/
```

#### Nginx Configuration

The web client needs two things from the web server:

1. Serve static files from the build output
2. Proxy `/api/*` requests to the SuperDeck server
3. Fall back to `index.html` for SPA client-side routing

```nginx
server {
    listen 80;
    server_name superdeck.example.com;

    root /var/www/superdeck;
    index index.html;

    # SPA fallback
    location / {
        try_files $uri $uri/ /index.html;
    }

    # API proxy to SuperDeck server
    location /api/ {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Option C: Docker

Add to an existing `docker-compose.yml`:

```yaml
services:
  webclient:
    image: nginx:alpine
    ports:
      - "8080:80"
    volumes:
      - ./src/WebClient/dist:/usr/share/nginx/html:ro
      - ./nginx-webclient.conf:/etc/nginx/conf.d/default.conf:ro
    depends_on:
      - superdeck
```

### Configuring the API Server URL

The web client can connect to a SuperDeck server running on any machine. Configuration is done via `config.json`, which is loaded at startup before React mounts.

#### Development

Edit `src/WebClient/public/config.json`:

```json
{
  "apiUrl": "http://192.168.1.100:5000"
}
```

When `apiUrl` is empty (`""`), the client uses relative URLs — which means requests go through the Vite dev proxy (configured in `vite.config.ts` to forward `/api/*` to `localhost:5000`).

To point the Vite proxy at a different server, edit `vite.config.ts`:

```typescript
proxy: {
  '/api': {
    target: 'http://your-server:5000',
    changeOrigin: true,
  },
},
```

#### Production

After building (`npm run build`), edit `dist/config.json`:

```json
{
  "apiUrl": "https://superdeck.example.com"
}
```

This file is served as a static asset and fetched at runtime — no rebuild is required to change the server URL. If you're using a reverse proxy (Nginx) that forwards `/api/*` to the server, you can leave `apiUrl` empty.

| Scenario | `apiUrl` value |
|----------|---------------|
| Dev with Vite proxy | `""` (empty) |
| Dev with remote server | `"http://192.168.1.100:5000"` |
| Production with Nginx proxy | `""` (empty) |
| Production direct to server | `"https://superdeck.example.com"` |

---

## Project Structure

```
src/WebClient/
├── index.html              # Entry HTML
├── package.json            # Dependencies and scripts
├── tsconfig.json           # TypeScript configuration
├── vite.config.ts          # Vite dev server + build config
├── public/
│   └── config.json         # Runtime API URL configuration
├── src/
│   ├── main.tsx            # React entry point
│   ├── App.tsx             # Route definitions
│   ├── types/
│   │   └── index.ts        # TypeScript types (mirrors C# models)
│   ├── api/
│   │   └── client.ts       # API client (all 28 endpoints)
│   ├── context/
│   │   ├── AuthContext.tsx  # Authentication state
│   │   └── GameContext.tsx  # Server settings + character
│   ├── hooks/
│   │   ├── useBattle.ts    # Battle state machine + polling
│   │   └── useCharacter.ts # Character CRUD
│   ├── pages/
│   │   ├── MainMenu.tsx
│   │   ├── CharacterCreation.tsx
│   │   ├── CharacterSelection.tsx
│   │   ├── CharacterHub.tsx
│   │   ├── DeckView.tsx
│   │   ├── StatAllocation.tsx
│   │   ├── BattlePage.tsx
│   │   ├── BattleResultPage.tsx
│   │   └── LevelUpPage.tsx
│   ├── components/
│   │   ├── Layout.tsx          # App shell + header
│   │   ├── CardDisplay.tsx     # Card rendering (full + compact)
│   │   ├── CardDetailModal.tsx # Card info modal
│   │   ├── CardTable.tsx       # Sortable card table
│   │   ├── HpBar.tsx           # Animated HP bar
│   │   ├── StatusEffectBadge.tsx
│   │   ├── SuitSelector.tsx
│   │   ├── StatStepper.tsx     # +/- stat controls
│   │   ├── BattleLog.tsx       # Scrollable color-coded log
│   │   ├── HandDisplay.tsx     # Card hand row
│   │   ├── QueueDisplay.tsx    # Queued cards with arrows
│   │   ├── ConfirmDialog.tsx   # Destructive action confirmation
│   │   └── LoadingSpinner.tsx
│   └── styles/
│       ├── global.css          # CSS custom properties + reset
│       └── colors.ts           # JS color maps for type/rarity
```

### Routes

| Route | Page | Purpose |
|-------|------|---------|
| `/menu` | MainMenu | Login/register, new/load character |
| `/characters/new` | CharacterCreation | Name, suit, starter cards |
| `/characters` | CharacterSelection | Pick existing character |
| `/character/:id` | CharacterHub | Stats, actions |
| `/character/:id/deck` | DeckView | View/remove cards |
| `/character/:id/stats` | StatAllocation | Distribute stat points |
| `/battle/:id` | BattlePage | Live battle |
| `/battle/:id/result` | BattleResultPage | XP, MMR, level up |
| `/battle/:id/levelup` | LevelUpPage | Booster pack + stats |

---

## Troubleshooting

### "Failed to fetch" / API errors

The server isn't running or the proxy isn't configured:

```bash
# Check server is running
curl http://localhost:5000/api/health

# Check Vite proxy in vite.config.ts
```

### Blank page after build

For production deployments, ensure your web server has SPA fallback routing configured (serve `index.html` for all non-file routes). See the Nginx configuration above.

### CORS errors

The SuperDeck server allows all origins by default. If you see CORS errors, verify the server's CORS configuration in `src/Server/Program.cs`.

### Battle state not updating

The battle page polls `/api/battle/{id}/state` every 300ms during non-Queue phases. If the battle appears stuck:

1. Check the browser dev tools Network tab for failing requests
2. Verify the server is processing the battle (check server logs)
3. Try refreshing the page — battle state is fetched fresh on load

### Cards not loading in deck view

The deck view fetches all cards via `GET /api/cards` and matches by ID. If cards show as missing, the server's card library may not be fully loaded. Restart the server and check for card loading errors in the console.

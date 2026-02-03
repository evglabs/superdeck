# SuperDeck – Revised **Detailed** Implementation Plan (Design-Doc Aligned)

**Authoritative Source:** Master Design Document v2
**Target Framework:** **.NET 10 (LTS)**
**Architecture:** Server-authoritative; offline = embedded localhost server; online = remote server

---

## Core Principle (Non‑Negotiable)

There is **no offline-only code path**.

* Offline mode runs the **same ASP.NET Core server** on `localhost`
* Online mode runs the **same server binary** remotely
* Client communicates **only via HTTP API**
* All rules, validation, scripting, and persistence live server-side

Any step that violates this principle is incorrect by definition.

---

# PHASE 0 — Solution & Environment Setup

### Chunk 0.0 — Repository Initialization

* Create git repository
* Enforce `.editorconfig` and nullable reference types

### Chunk 0.1 — Solution Creation

```bash
dotnet new sln -n SuperDeck
```

### Chunk 0.2 — Project Creation (ALL net10.0)

```bash
dotnet new classlib -n Core -f net10.0
dotnet new webapi   -n Server -f net10.0
dotnet new console  -n Client -f net10.0
dotnet new xunit    -n Tests -f net10.0
```

### Chunk 0.3 — Project References

* Server → Core
* Client → Core
* Tests → Core, Server

### Chunk 0.4 — Verification

```bash
dotnet build
```

**STOP:** Solution must build cleanly.

---

# PHASE 1 — Core Domain Model (Pure, Headless)

### Chunk 1.0 — Domain Model Rules

* No HTTP references
* No database references
* No UI assumptions

### Chunk 1.1 — Core Models

* `Character`
* `Card`
* `Suit`
* `StatusEffect`
* `BattleState`
* `BattleSession`
* `PlayerAction`

### Chunk 1.2 — Value Objects & Enums

* BattlePhase
* CardType
* TargetType
* HookType

### Chunk 1.3 — Deterministic Invariants

* No randomness without injected RNG
* No mutation outside controlled services

**STOP:** Unit tests validate pure model behavior.

---

# PHASE 2 — Card Scripting & Hook Engine

### Chunk 2.0 — Roslyn Compiler Wrapper

* Compile once, cache delegates
* Script timeout enforcement
* Shared globals (`Player`, `Opponent`, `Battle`, `Rng`)

### Chunk 2.1 — Hook Registration System

* Lifecycle hook registry
* Ordered execution
* Hook removal on expiration

### Chunk 2.2 — Script Failure Handling

* Exception isolation
* Script abort without corrupting battle state

**STOP:** Cards mutate battle state exclusively through scripts.

---

# PHASE 3 — Battle Engine (Server Core)

### Chunk 3.0 — BattleService

* Start battle
* Initialize decks
* Seed RNG

### Chunk 3.1 — Phase Controller

* Draw phase
* Queue phase
* Resolution phase

### Chunk 3.2 — Resolution Engine

* Speed-based ordering
* Alternating execution
* Win-condition checks after each action

### Chunk 3.3 — Session Store

* In-memory `BattleSession` tracking
* Activity timestamps
* Expiry logic

**STOP:** Battles run fully headless via tests.

---

# PHASE 4 — Persistence Layer (Unified Schema)

### Chunk 4.0 — Repository Interfaces (Core)

* `ICharacterRepository`
* `IGhostRepository`
* `IPlayerAccountRepository`

### Chunk 4.1 — Schema Definition

* SQLite-compatible SQL
* MariaDB-compatible SQL
* JSON fields for card IDs

### Chunk 4.2 — SQLite Implementations

* Offline persistence
* Auto schema initialization

### Chunk 4.3 — MariaDB Implementations

* Online persistence
* Same interfaces, same behavior

**STOP:** Persistence backend is swappable by configuration.

---

# PHASE 5 — HTTP API (Authoritative Boundary)

### Chunk 5.0 — Minimal API Setup

* ASP.NET Core minimal hosting
* Dependency injection

### Chunk 5.1 — Battle Endpoints

* `POST /api/battle/start`
* `POST /api/battle/{id}/action`
* `GET  /api/battle/{id}/state`

### Chunk 5.2 — Validation Middleware

* Phase correctness
* Ownership checks
* Script safety

### Chunk 5.3 — Error Contracts

* Deterministic error responses
* No silent failures

**STOP:** Entire game playable via HTTP alone.

---

# PHASE 6 — Embedded Server (Offline Mode)

### Chunk 6.0 — EmbeddedServerManager

* Launch ASP.NET Core server from client
* Fixed localhost port

### Chunk 6.1 — Offline Configuration

* SQLite repositories
* Auth middleware bypass for localhost

### Chunk 6.2 — Shutdown & Restart Safety

* Graceful server lifecycle

**STOP:** Offline play is literally client + localhost API.

---

# PHASE 7 — Client Console Prototype

### Chunk 7.0 — API Client

* HTTP wrapper
* Error handling

### Chunk 7.1 — Spectre.Console UI

* Render battle state
* Queue actions

### Chunk 7.2 — Zero‑Trust Rule

* Client never modifies state locally

**STOP:** Offline game fully playable.

---

# PHASE 8 — Ghosts & AI

### Chunk 8.0 — Ghost Snapshot Model

* Serialize full character state

### Chunk 8.1 — AIProfiles

* Behavior rules

### Chunk 8.2 — Matchmaking Service

* Local selection
* Online ghost compatibility

**STOP:** AI indistinguishable from humans.

---

# PHASE 9 — Online Multiplayer Enablement

### Chunk 9.0 — Authentication

* JWT issuance
* Token refresh

### Chunk 9.1 — Remote Hosting

* Same server binary
* Different config

### Chunk 9.2 — Security Hardening

* Rate limiting
* CORS

**STOP:** Online/offline share identical API paths.

---

# PHASE 10 — Hardening & Expansion

* Load testing
* Script sandboxing
* GUI client
* Mod distribution

---

## Explicit Anti‑Goals

* No client-side game rules
* No offline shortcuts
* No duplicated logic paths

---

**If the server is correct, everything else becomes simple.**

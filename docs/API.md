# SuperDeck API Reference

Complete REST API documentation for SuperDeck server.

## Table of Contents

- [Overview](#overview)
- [Authentication](#authentication)
- [Characters](#characters)
- [Cards](#cards)
- [Battles](#battles)
- [Booster Packs](#booster-packs)
- [Server Info](#server-info)
- [Error Handling](#error-handling)
- [Models](#models)

---

## Overview

### Base URL

```
http://localhost:5000/api
```

### Request Format

- Content-Type: `application/json`
- All request bodies should be JSON

### Response Format

**Success:**
```json
{
  "field1": "value1",
  "field2": "value2"
}
```

**Error:**
```json
{
  "error": "Error message description"
}
```

### HTTP Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 201 | Created |
| 400 | Bad Request |
| 401 | Unauthorized |
| 404 | Not Found |
| 429 | Rate Limited |
| 500 | Server Error |

---

## Authentication

### Register

Create a new player account.

```http
POST /api/auth/register
```

**Request Body:**
```json
{
  "username": "player1",
  "password": "secret123"
}
```

**Response (201):**
```json
{
  "token": "abc123...",
  "playerId": "uuid-here",
  "username": "player1"
}
```

**Errors:**
- 400: Username already exists
- 400: Username too short/long (3-20 chars)
- 400: Password too short (min 6 chars)

---

### Login

Authenticate and receive a token.

```http
POST /api/auth/login
```

**Request Body:**
```json
{
  "username": "player1",
  "password": "secret123"
}
```

**Response (200):**
```json
{
  "token": "abc123...",
  "playerId": "uuid-here",
  "username": "player1"
}
```

**Errors:**
- 401: Invalid username or password

---

### Logout

End the current session.

```http
POST /api/auth/logout
Authorization: Bearer {token}
```

**Response (200):**
```json
{
  "message": "Logged out"
}
```

---

### Get Current Player

Get information about the authenticated player.

```http
GET /api/auth/me
Authorization: Bearer {token}
```

**Response (200):**
```json
{
  "playerId": "uuid-here",
  "username": "player1",
  "totalWins": 15,
  "totalLosses": 8,
  "highestMMR": 1250,
  "totalBattles": 23
}
```

---

## Characters

### List Characters

Get all characters for the current player.

```http
GET /api/characters
Authorization: Bearer {token}
```

**Query Parameters:**
| Param | Type | Description |
|-------|------|-------------|
| playerId | string | Filter by player (optional) |

**Response (200):**
```json
[
  {
    "id": "char-uuid",
    "name": "FireMage",
    "level": 5,
    "xp": 200,
    "attack": 3,
    "defense": 1,
    "speed": 1,
    "deckCardIds": ["fire_fireball", "basic_kick"],
    "wins": 10,
    "losses": 5,
    "mmr": 1150
  }
]
```

---

### Create Character

Create a new character.

```http
POST /api/characters
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "name": "FireMage",
  "suitChoice": "Fire",
  "playerId": "optional-player-id"
}
```

**Response (201):**
```json
{
  "id": "new-char-uuid",
  "name": "FireMage",
  "level": 1,
  "xp": 0,
  "attack": 0,
  "defense": 0,
  "speed": 0,
  "deckCardIds": ["basic_punch", "basic_punch", "basic_punch", "basic_block", "basic_block", "basic_block", "fire_fireball", "fire_firebolt", "fire_spark", "fire_ember"],
  "wins": 0,
  "losses": 0,
  "mmr": 1000
}
```

**Suit Options:**
```
Basic, Fire, MartialArts, Magic, Electricity,
Mental, Espionage, Nature, Tech, Berserker,
Military, Radiation, Showbiz, Speedster, Money
```

---

### Get Character

Get a specific character by ID.

```http
GET /api/characters/{id}
Authorization: Bearer {token}
```

**Response (200):**
```json
{
  "id": "char-uuid",
  "name": "FireMage",
  "level": 5,
  "xp": 200,
  "attack": 3,
  "defense": 1,
  "speed": 1,
  "deckCardIds": ["fire_fireball", "basic_kick"],
  "wins": 10,
  "losses": 5,
  "mmr": 1150,
  "ownerPlayerId": "player-uuid"
}
```

**Errors:**
- 404: Character not found

---

### Update Character Stats

Allocate stat points to a character.

```http
PUT /api/characters/{id}/stats
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "attack": 2,
  "defense": 1,
  "speed": 1
}
```

**Response (200):**
```json
{
  "id": "char-uuid",
  "name": "FireMage",
  "attack": 2,
  "defense": 1,
  "speed": 1
}
```

**Errors:**
- 400: Not enough stat points available
- 404: Character not found

---

### Delete Character

Delete a character permanently.

```http
DELETE /api/characters/{id}
Authorization: Bearer {token}
```

**Response (204):** No content

**Errors:**
- 404: Character not found

---

### Add Cards to Deck

Add cards to a character's deck.

```http
POST /api/characters/{id}/cards
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "cardIds": ["fire_fireball", "fire_phoenix"]
}
```

**Response (200):**
```json
{
  "id": "char-uuid",
  "deckCardIds": ["basic_punch", "fire_fireball", "fire_phoenix"]
}
```

**Errors:**
- 400: Invalid card ID
- 404: Character not found

---

### Remove Cards from Deck

Remove cards from a character's deck.

```http
DELETE /api/characters/{id}/cards
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "cardIds": ["basic_punch"]
}
```

**Response (200):**
```json
{
  "id": "char-uuid",
  "deckCardIds": ["fire_fireball", "fire_phoenix"]
}
```

---

## Cards

### List All Cards

Get all available cards in the game.

```http
GET /api/cards
```

**Response (200):**
```json
[
  {
    "id": "fire_fireball",
    "name": "Fireball",
    "suit": "Fire",
    "type": "Attack",
    "rarity": "Common",
    "description": "Deal 15 damage"
  },
  {
    "id": "basic_kick",
    "name": "Kick",
    "suit": "Basic",
    "type": "Attack",
    "rarity": "Uncommon",
    "description": "Deal 15 damage"
  }
]
```

---

### Get Card

Get a specific card by ID.

```http
GET /api/cards/{id}
```

**Response (200):**
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

**Errors:**
- 404: Card not found

---

### Get Cards by Suit

Get all cards for a specific suit.

```http
GET /api/cards/suit/{suit}
```

**Response (200):**
```json
[
  {
    "id": "fire_fireball",
    "name": "Fireball",
    "suit": "Fire",
    "type": "Attack",
    "rarity": "Common"
  }
]
```

---

### Get Starter Pack

Get the starter pack cards for a suit.

```http
GET /api/cards/starterpack/{suit}
```

**Response (200):**
```json
[
  {
    "id": "basic_punch",
    "name": "Punch",
    "suit": "Basic",
    "type": "Attack",
    "rarity": "Common"
  },
  {
    "id": "fire_fireball",
    "name": "Fireball",
    "suit": "Fire",
    "type": "Attack",
    "rarity": "Common"
  }
]
```

---

## Battles

### Start Battle

Start a new battle with a character.

```http
POST /api/battle/start
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "characterId": "char-uuid",
  "autoBattle": false,
  "autoBattleMode": "Watch",
  "aiProfileId": null
}
```

**Response (200):**
```json
{
  "battleId": "battle-uuid",
  "battleState": {
    "battleId": "battle-uuid",
    "player": {
      "id": "char-uuid",
      "name": "FireMage",
      "currentHP": 150,
      "maxHP": 150,
      "attack": 3,
      "defense": 1,
      "speed": 1
    },
    "opponent": {
      "id": "ghost-uuid",
      "name": "Shadow",
      "currentHP": 140,
      "maxHP": 140,
      "attack": 2,
      "defense": 2,
      "speed": 2
    },
    "phase": "DrawPhase",
    "round": 1,
    "playerHand": [
      {"id": "fire_fireball", "name": "Fireball"},
      {"id": "basic_punch", "name": "Punch"}
    ],
    "playerQueue": [],
    "opponentQueueCount": 0,
    "playerStatuses": [],
    "opponentStatuses": [],
    "battleLog": ["Battle started!"]
  }
}
```

**Auto Battle Modes:**
| Mode | Description |
|------|-------------|
| `Watch` | AI plays, shows each turn |
| `Instant` | AI plays, returns final result |

---

### Submit Action

Submit a player action during battle.

```http
POST /api/battle/{battleId}/action
Authorization: Bearer {token}
```

**Request Body (Queue Card):**
```json
{
  "type": "QueueCard",
  "cardId": "fire_fireball"
}
```

**Request Body (End Queue):**
```json
{
  "type": "EndQueue"
}
```

**Response (200):**
```json
{
  "valid": true,
  "message": "Card queued",
  "battleState": {
    "phase": "QueuePhase",
    "playerQueue": [
      {"id": "fire_fireball", "name": "Fireball"}
    ]
  }
}
```

**Action Types:**
| Type | Description |
|------|-------------|
| `QueueCard` | Add card to queue |
| `EndQueue` | Finish queuing, resolve round |
| `RemoveFromQueue` | Remove card from queue |

**Errors:**
- 400: Invalid action for current phase
- 400: Card not in hand
- 400: Queue is full
- 404: Battle not found

---

### Get Battle State

Get the current state of a battle.

```http
GET /api/battle/{battleId}/state
Authorization: Bearer {token}
```

**Response (200):**
```json
{
  "battleId": "battle-uuid",
  "phase": "QueuePhase",
  "round": 3,
  "player": {
    "currentHP": 120,
    "maxHP": 150
  },
  "opponent": {
    "currentHP": 80,
    "maxHP": 140
  },
  "playerHand": [],
  "playerQueue": [],
  "battleLog": []
}
```

---

### Forfeit Battle

Forfeit the current battle.

```http
POST /api/battle/{battleId}/forfeit
Authorization: Bearer {token}
```

**Response (200):**
```json
{
  "battleId": "battle-uuid",
  "phase": "Ended",
  "winnerId": "opponent-uuid",
  "battleLog": ["Player forfeited!"]
}
```

---

### Finalize Battle

End the battle and receive rewards.

```http
POST /api/battle/{battleId}/finalize
Authorization: Bearer {token}
```

**Response (200):**
```json
{
  "battleId": "battle-uuid",
  "winnerId": "char-uuid",
  "playerWon": true,
  "xpGained": 50,
  "mmrChange": 25,
  "levelsGained": 1,
  "newLevel": 6,
  "battleLog": [
    "Battle ended!",
    "FireMage wins!",
    "Gained 50 XP",
    "Level up! Now level 6"
  ]
}
```

---

### Toggle Auto Battle

Enable or disable auto-battle during a fight.

```http
POST /api/battle/{battleId}/auto-battle
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "enabled": true,
  "aiProfileId": null
}
```

**Response (200):**
```json
{
  "battleState": { }
}
```

---

### Auto Queue Cards

Let AI choose cards for current turn.

```http
POST /api/battle/{battleId}/auto-queue
Authorization: Bearer {token}
```

**Response (200):**
```json
{
  "valid": true,
  "message": "Auto-queued 3 cards",
  "battleState": {
    "playerQueue": [
      {"id": "fire_fireball"},
      {"id": "basic_punch"},
      {"id": "basic_block"}
    ]
  }
}
```

---

### Run Instant Battle

Run a complete battle instantly with AI.

```http
POST /api/battle/instant
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "characterId": "char-uuid",
  "aiProfileId": null
}
```

**Response (200):**
```json
{
  "battleId": "battle-uuid",
  "result": {
    "winnerId": "char-uuid",
    "playerWon": true,
    "xpGained": 50,
    "mmrChange": 25
  },
  "battleLog": [
    "Round 1: Player plays Fireball",
    "Round 1: Opponent plays Punch"
  ],
  "totalRounds": 7
}
```

---

## Booster Packs

### Generate Pack

Generate a booster pack for a character (usually on level up).

```http
POST /api/packs/generate
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "characterId": "char-uuid"
}
```

**Response (200):**
```json
{
  "id": "pack-uuid",
  "characterId": "char-uuid",
  "cards": [
    {"id": "fire_inferno", "name": "Inferno", "rarity": "Rare"},
    {"id": "basic_dodge", "name": "Dodge", "rarity": "Common"},
    {"id": "tech_laser", "name": "Laser", "rarity": "Uncommon"}
  ],
  "createdAt": "2024-01-15T10:30:00Z"
}
```

---

## Server Info

### Health Check

Check if the server is running.

```http
GET /api/health
```

**Response (200):**
```
OK
```

---

### Get Server Info

Get server version and settings.

```http
GET /api/info
```

**Response (200):**
```json
{
  "version": "1.0.0",
  "cardCount": 94,
  "settings": {
    "baseHP": 100,
    "hpPerLevel": 10,
    "maxLevel": 10,
    "baseQueueSlots": 3,
    "statPointsPerLevel": 1
  }
}
```

---

## Error Handling

### Error Response Format

All errors return this format:

```json
{
  "error": "Human-readable error message"
}
```

### Common Error Codes

| Status | Error | Cause |
|--------|-------|-------|
| 400 | "Invalid request" | Malformed JSON |
| 400 | "Character name required" | Missing field |
| 400 | "Not enough stat points" | Validation failed |
| 401 | "Unauthorized" | Missing/invalid token |
| 404 | "Character not found" | Invalid ID |
| 404 | "Battle not found" | Invalid battle ID |
| 429 | "Rate limit exceeded" | Too many requests |

### Rate Limit Headers

When rate limiting is enabled:

```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1705312800
```

---

## Models

### Character

```typescript
interface Character {
  id: string;
  name: string;
  level: number;
  xp: number;
  attack: number;
  defense: number;
  speed: number;
  deckCardIds: string[];
  wins: number;
  losses: number;
  mmr: number;
  ownerPlayerId?: string;
  createdAt: string;
  lastModified: string;
}
```

### Card

```typescript
interface Card {
  id: string;
  name: string;
  suit: Suit;
  type: CardType;
  rarity: Rarity;
  description: string;
  immediateEffect?: CardEffect;
  grantsStatusTo?: StatusGrant;
}

interface CardEffect {
  target: TargetType;
  script: string;
}

type Suit = "Basic" | "Fire" | "MartialArts" | "Magic" |
            "Electricity" | "Mental" | "Espionage" | "Nature" |
            "Tech" | "Berserker" | "Military" | "Radiation" |
            "Showbiz" | "Speedster" | "Money";

type CardType = "Attack" | "Defense" | "Buff" | "Debuff" | "Utility";

type Rarity = "Common" | "Uncommon" | "Rare" | "Epic" | "Legendary";

type TargetType = "Self" | "Opponent" | "Both" | "None";
```

### BattleState

```typescript
interface BattleState {
  battleId: string;
  player: CharacterInBattle;
  opponent: CharacterInBattle;
  phase: BattlePhase;
  round: number;
  playerHand: Card[];
  playerQueue: Card[];
  playerDeck: Card[];
  playerDiscard: Card[];
  opponentQueueCount: number;
  playerStatuses: StatusEffect[];
  opponentStatuses: StatusEffect[];
  battleLog: string[];
  isPlayerTurn: boolean;
  winnerId?: string;
}

type BattlePhase = "NotStarted" | "DrawPhase" | "QueuePhase" |
                   "ResolutionPhase" | "Cleanup" | "Ended";
```

### StatusEffect

```typescript
interface StatusEffect {
  name: string;
  duration: number;
  stacks: number;
  sourceCardId?: string;
}
```

### PlayerAction

```typescript
interface PlayerAction {
  type: ActionType;
  cardId?: string;
  targetId?: string;
}

type ActionType = "QueueCard" | "EndQueue" | "RemoveFromQueue";
```

### BattleResult

```typescript
interface BattleResult {
  battleId: string;
  winnerId: string;
  playerWon: boolean;
  xpGained: number;
  mmrChange: number;
  levelsGained: number;
  newLevel: number;
  battleLog: string[];
}
```

---

## Example Workflow

### Complete Battle Flow

```bash
# 1. Login
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"player1","password":"secret"}' | jq -r '.token')

# 2. Get characters
curl -s http://localhost:5000/api/characters \
  -H "Authorization: Bearer $TOKEN" | jq

# 3. Start battle
BATTLE=$(curl -s -X POST http://localhost:5000/api/battle/start \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"characterId":"char-uuid"}')
BATTLE_ID=$(echo $BATTLE | jq -r '.battleId')

# 4. Queue cards
curl -s -X POST "http://localhost:5000/api/battle/$BATTLE_ID/action" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"type":"QueueCard","cardId":"fire_fireball"}'

# 5. End queue (resolve round)
curl -s -X POST "http://localhost:5000/api/battle/$BATTLE_ID/action" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"type":"EndQueue"}'

# 6. Repeat until battle ends, then finalize
curl -s -X POST "http://localhost:5000/api/battle/$BATTLE_ID/finalize" \
  -H "Authorization: Bearer $TOKEN" | jq
```

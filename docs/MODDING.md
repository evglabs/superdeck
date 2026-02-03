# SuperDeck Modding Guide

SuperDeck features a powerful modding system that allows you to create custom cards, status effects, and game mechanics using embedded C# scripts.

## Table of Contents

- [Overview](#overview)
- [Card Creation](#card-creation)
- [Status Effects](#status-effects)
- [Hook System](#hook-system)
- [Script API Reference](#script-api-reference)
- [Advanced Techniques](#advanced-techniques)
- [Testing Your Mods](#testing-your-mods)
- [Examples](#examples)

---

## Overview

### How Modding Works

SuperDeck uses the Roslyn C# scripting engine to compile and execute card effects at runtime. This means:

1. **No recompilation needed** - Add JSON card files and restart the server
2. **Full C# power** - Use any C# syntax in your scripts
3. **Sandboxed execution** - Scripts run in isolation with timeouts
4. **Hot-loadable** - Cards load when the server starts

### Card File Location

```
src/Server/Data/ServerCards/
├── basic_kick.json
├── fire_fireball.json
├── fire_phoenix.json
└── [your_cards_here.json]
```

### Naming Convention

Files should follow: `{suit}_{cardname}.json`

Examples:
- `fire_inferno.json`
- `tech_laserbeam.json`
- `basic_block.json`

---

## Card Creation

### Basic Card Structure

```json
{
  "id": "suit_cardname",
  "name": "Display Name",
  "suit": "Suit",
  "type": "Attack",
  "rarity": "Common",
  "description": "What the card does",
  "immediateEffect": {
    "target": "Opponent",
    "script": "DealDamage(Opponent, 10);"
  }
}
```

### Required Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique identifier (lowercase, underscores) |
| `name` | string | Display name |
| `suit` | string | Card suit (see below) |
| `type` | string | Card type (see below) |
| `rarity` | string | Rarity level (see below) |
| `description` | string | Human-readable effect |

### Card Types

| Type | Typical Use |
|------|-------------|
| `Attack` | Deal damage |
| `Defense` | Block/heal |
| `Buff` | Positive effects on self |
| `Debuff` | Negative effects on enemy |
| `Utility` | Card manipulation, special effects |

### Suits

```
Basic, Fire, MartialArts, Magic, Electricity,
Mental, Espionage, Nature, Tech, Berserker,
Military, Radiation, Showbiz, Speedster, Money
```

### Rarities

| Rarity | Numeric Value | Drop Rate |
|--------|---------------|-----------|
| `Common` | 1 | 60% |
| `Uncommon` | 2 | 30% |
| `Rare` | 3 | 9% |
| `Epic` | 4 | 0.9% |
| `Legendary` | 5 | 0.1% |

---

## Immediate Effects

### Structure

```json
"immediateEffect": {
  "target": "TargetType",
  "script": "C# code here;"
}
```

### Target Types

| Target | Description |
|--------|-------------|
| `Self` | The player using the card |
| `Opponent` | The enemy |
| `Both` | Both players |
| `None` | No target (utility effects) |

### Simple Examples

**Deal damage:**
```json
"immediateEffect": {
  "target": "Opponent",
  "script": "DealDamage(Opponent, 15);"
}
```

**Heal self:**
```json
"immediateEffect": {
  "target": "Self",
  "script": "Heal(Player, 10);"
}
```

**Draw cards:**
```json
"immediateEffect": {
  "target": "Self",
  "script": "DrawCards(Player, 2);"
}
```

### Complex Examples

**Conditional damage:**
```json
"script": "var damage = Player.CurrentHP < 50 ? 30 : 15; DealDamage(Opponent, damage);"
```

**Multiple effects:**
```json
"script": "DealDamage(Opponent, 10); Heal(Player, 5);"
```

**Random effect:**
```json
"script": "var damage = Random.Next(5, 20); DealDamage(Opponent, damage);"
```

---

## Status Effects

### Adding Status Effects to Cards

```json
{
  "id": "fire_burn",
  "name": "Burn",
  "suit": "Fire",
  "type": "Debuff",
  "rarity": "Common",
  "description": "Apply burning for 3 turns",
  "immediateEffect": {
    "target": "Opponent",
    "script": ""
  },
  "grantsStatusTo": {
    "target": "Opponent",
    "status": {
      "name": "Burning",
      "duration": 3,
      "hooks": {
        "OnTurnEnd": "DealDamage(Target, 5); Log($\"{Target.Name} takes 5 burn damage!\");"
      }
    }
  }
}
```

### Status Structure

```json
"grantsStatusTo": {
  "target": "Self|Opponent",
  "status": {
    "name": "Status Name",
    "duration": 3,
    "stacks": false,
    "maxStacks": 1,
    "hooks": {
      "HookType": "C# script"
    }
  }
}
```

### Status Fields

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Status display name |
| `duration` | int | Turns until expiry (-1 = permanent) |
| `stacks` | bool | Can multiple instances stack? |
| `maxStacks` | int | Maximum stack count |
| `hooks` | object | Hook scripts (see below) |

---

## Hook System

### Available Hooks

#### Lifecycle Hooks

| Hook | Trigger |
|------|---------|
| `OnTurnStart` | Beginning of turn |
| `OnTurnEnd` | End of turn |
| `OnQueue` | Card added to queue |
| `OnPlay` | Card is played |
| `OnDiscard` | Card is discarded |
| `OnCardResolve` | Card effect completes |

#### Combat Hooks

| Hook | Trigger |
|------|---------|
| `OnTakeDamage` | Taking damage |
| `OnDealDamage` | Dealing damage |
| `OnHeal` | Being healed |
| `OnDeath` | HP reaches 0 |

#### Stat Calculation Hooks

| Hook | Trigger |
|------|---------|
| `OnCalculateAttack` | When attack is calculated |
| `OnCalculateDefense` | When defense is calculated |
| `OnCalculateSpeed` | When speed is calculated |

#### Battle Phase Hooks

| Hook | Trigger |
|------|---------|
| `OnDrawPhase` | Draw phase begins |
| `OnQueuePhaseStart` | Queue phase begins |
| `BeforeQueueResolve` | Before queued cards resolve |
| `OnBattleEnd` | Battle ends |
| `OnOpponentPlay` | Opponent plays a card |
| `OnBuffExpire` | Status effect expires |

### Hook Script Context

Inside hooks, these variables are available:

| Variable | Type | Description |
|----------|------|-------------|
| `Player` | Character | Card owner |
| `Opponent` | Character | Enemy character |
| `Target` | Character | Status effect target |
| `Battle` | BattleState | Current battle state |
| `SourceCard` | Card | Card that created status |
| `Random` | Random | Random number generator |

### Hook Examples

**Damage over time:**
```json
"OnTurnEnd": "DealDamage(Target, 5);"
```

**Heal on turn start:**
```json
"OnTurnStart": "Heal(Target, 3);"
```

**Trigger on taking damage:**
```json
"OnTakeDamage": "if (DamageAmount > 10) { DealDamage(Opponent, 5); }"
```

**Death prevention:**
```json
"OnDeath": "if (Player.CurrentHP <= 0) { Player.CurrentHP = 1; RemoveStatus(\"Phoenix Rising\"); }"
```

**Stat modification:**
```json
"OnCalculateAttack": "ModifyAttack(5);"
```

---

## Script API Reference

### Core Functions

#### Damage and Healing

```csharp
// Deal damage to target
DealDamage(Character target, int amount)

// Heal target
Heal(Character target, int amount)

// Deal damage to both players
DealDamageToAll(int amount)
```

#### Status Effects

```csharp
// Apply a named status
ApplyStatus(Character target, string statusName, int duration)

// Remove a status by name
RemoveStatus(string statusName)

// Check if has status
HasStatus(Character target, string statusName)

// Get status stack count
GetStatusStacks(Character target, string statusName)
```

#### Card Manipulation

```csharp
// Draw cards
DrawCards(Character target, int count)

// Discard random cards
DiscardRandom(Character target, int count)

// Shuffle deck
ShuffleDeck(Character target)
```

#### Stat Modification

```csharp
// Temporary stat changes (use in OnCalculate hooks)
ModifyAttack(int amount)
ModifyDefense(int amount)
ModifySpeed(int amount)

// Permanent stat changes
AddAttack(Character target, int amount)
AddDefense(Character target, int amount)
AddSpeed(Character target, int amount)
```

#### Utility

```csharp
// Log message to battle log
Log(string message)

// Random number (0 to max-1)
Random.Next(int max)

// Random number (min to max-1)
Random.Next(int min, int max)
```

### Available Properties

#### Character Properties

```csharp
Player.Name           // Character name
Player.CurrentHP      // Current HP
Player.MaxHP          // Maximum HP
Player.Attack         // Attack stat
Player.Defense        // Defense stat
Player.Speed          // Speed stat
Player.Level          // Character level
```

#### Battle Properties

```csharp
Battle.Round          // Current round number
Battle.Phase          // Current battle phase
Battle.IsPlayerTurn   // True if player's turn
Battle.TurnNumber     // Total turns elapsed
```

#### Hand/Deck Properties

```csharp
PlayerHand.Count      // Cards in hand
PlayerDeck.Count      // Cards in deck
OpponentHand.Count    // Enemy hand size
```

---

## Advanced Techniques

### Conditional Logic

```json
"script": "if (Player.CurrentHP < Player.MaxHP / 2) { DealDamage(Opponent, 30); } else { DealDamage(Opponent, 15); }"
```

### Loops

```json
"script": "for (int i = 0; i < 3; i++) { DealDamage(Opponent, 5); }"
```

### Variables

```json
"script": "var bonus = Player.Attack * 2; DealDamage(Opponent, 10 + bonus);"
```

### LINQ Operations

```json
"script": "var lowHPCards = PlayerHand.Where(c => c.Type == \"Attack\").Count(); DealDamage(Opponent, lowHPCards * 5);"
```

### Multi-line Scripts

For complex logic, use semicolons to separate statements:

```json
"script": "var damage = 10; if (HasStatus(Player, \"Enraged\")) { damage *= 2; } DealDamage(Opponent, damage); Log($\"Dealt {damage} damage!\");"
```

### String Interpolation

```json
"script": "Log($\"{Player.Name} attacks for {Player.Attack + 10} damage!\"); DealDamage(Opponent, Player.Attack + 10);"
```

---

## Testing Your Mods

### 1. Validate JSON Syntax

Use a JSON validator or editor with syntax highlighting.

### 2. Check Card Loading

Start the server and look for errors in the console:

```bash
dotnet run --project src/Server
# Watch for "Loaded X cards" or error messages
```

### 3. Test in Battle

Create a character, add your card to the deck, and battle.

### 4. Debug with Logging

Add `Log()` statements to track execution:

```json
"script": "Log(\"Card effect starting...\"); DealDamage(Opponent, 10); Log(\"Damage dealt!\");"
```

### 5. Check Script Errors

Script errors appear in the battle log. Common issues:

| Error | Cause | Fix |
|-------|-------|-----|
| `Timeout` | Script took too long | Simplify logic |
| `NullReference` | Accessing missing data | Check conditions |
| `SyntaxError` | Invalid C# | Fix script syntax |

---

## Examples

### Basic Attack Card

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

### Scaling Attack

```json
{
  "id": "berserker_rage",
  "name": "Berserker Rage",
  "suit": "Berserker",
  "type": "Attack",
  "rarity": "Rare",
  "description": "Deal damage equal to missing HP",
  "immediateEffect": {
    "target": "Opponent",
    "script": "var missingHP = Player.MaxHP - Player.CurrentHP; DealDamage(Opponent, missingHP);"
  }
}
```

### Heal with Condition

```json
{
  "id": "nature_regrowth",
  "name": "Regrowth",
  "suit": "Nature",
  "type": "Buff",
  "rarity": "Uncommon",
  "description": "Heal 20 HP. If below 50% HP, heal 40 instead",
  "immediateEffect": {
    "target": "Self",
    "script": "var amount = Player.CurrentHP < Player.MaxHP / 2 ? 40 : 20; Heal(Player, amount);"
  }
}
```

### Status Effect - Burn

```json
{
  "id": "fire_ignite",
  "name": "Ignite",
  "suit": "Fire",
  "type": "Debuff",
  "rarity": "Common",
  "description": "Enemy burns for 5 damage per turn for 3 turns",
  "grantsStatusTo": {
    "target": "Opponent",
    "status": {
      "name": "Burning",
      "duration": 3,
      "hooks": {
        "OnTurnEnd": "DealDamage(Target, 5); Log($\"{Target.Name} burns for 5 damage!\");"
      }
    }
  }
}
```

### Status Effect - Shield

```json
{
  "id": "tech_shield",
  "name": "Energy Shield",
  "suit": "Tech",
  "type": "Defense",
  "rarity": "Uncommon",
  "description": "Block the next 20 damage",
  "grantsStatusTo": {
    "target": "Self",
    "status": {
      "name": "Shielded",
      "duration": -1,
      "hooks": {
        "OnTakeDamage": "if (DamageAmount > 0) { var blocked = Math.Min(20, DamageAmount); DamageAmount -= blocked; Log($\"Shield blocks {blocked} damage!\"); RemoveStatus(\"Shielded\"); }"
      }
    }
  }
}
```

### Legendary Card - Phoenix

```json
{
  "id": "fire_phoenix",
  "name": "Phoenix",
  "suit": "Fire",
  "type": "Buff",
  "rarity": "Legendary",
  "description": "If you die this turn, revive with 50% HP",
  "grantsStatusTo": {
    "target": "Self",
    "status": {
      "name": "Phoenix Rising",
      "duration": 1,
      "hooks": {
        "OnDeath": "if (Player.CurrentHP <= 0) { Player.CurrentHP = (int)(Player.MaxHP * 0.50); Log($\"{Player.Name} rises from the ashes with {Player.CurrentHP} HP!\"); RemoveStatus(\"Phoenix Rising\"); }"
      }
    }
  }
}
```

### Card Draw Utility

```json
{
  "id": "magic_insight",
  "name": "Mystical Insight",
  "suit": "Magic",
  "type": "Utility",
  "rarity": "Uncommon",
  "description": "Draw 2 cards",
  "immediateEffect": {
    "target": "Self",
    "script": "DrawCards(Player, 2); Log(\"Drew 2 cards!\");"
  }
}
```

### Random Effect

```json
{
  "id": "showbiz_wildcard",
  "name": "Wild Card",
  "suit": "Showbiz",
  "type": "Attack",
  "rarity": "Rare",
  "description": "Deal 5-25 random damage",
  "immediateEffect": {
    "target": "Opponent",
    "script": "var damage = Random.Next(5, 26); DealDamage(Opponent, damage); Log($\"Wild Card deals {damage} damage!\");"
  }
}
```

### Stacking Status

```json
{
  "id": "radiation_exposure",
  "name": "Radiation Exposure",
  "suit": "Radiation",
  "type": "Debuff",
  "rarity": "Uncommon",
  "description": "Apply Radiation (stacks). Each stack deals 2 damage per turn.",
  "grantsStatusTo": {
    "target": "Opponent",
    "status": {
      "name": "Irradiated",
      "duration": 5,
      "stacks": true,
      "maxStacks": 10,
      "hooks": {
        "OnTurnEnd": "var stacks = GetStatusStacks(Target, \"Irradiated\"); var damage = stacks * 2; DealDamage(Target, damage); Log($\"Radiation deals {damage} damage! ({stacks} stacks)\");"
      }
    }
  }
}
```

---

## Best Practices

### Card Design

1. **Clear descriptions** - Players should understand what the card does
2. **Balanced effects** - Match power to rarity
3. **Interesting choices** - Conditional effects are engaging
4. **Synergy potential** - Cards that work together create depth

### Script Writing

1. **Keep it simple** - Complex scripts are harder to debug
2. **Use logging** - Helps players understand what happened
3. **Handle edge cases** - What if HP is already 0?
4. **Test thoroughly** - Play many battles with your card

### Performance

1. **Avoid infinite loops** - Scripts timeout after 500ms
2. **Minimize allocations** - Don't create huge arrays
3. **Simple conditions first** - Check cheap conditions before expensive ones

---

## Troubleshooting

### Card doesn't appear

- Check file is in `src/Server/Data/ServerCards/`
- Verify JSON is valid (no trailing commas, proper quotes)
- Check server logs for loading errors

### Script doesn't execute

- Verify `immediateEffect` or `grantsStatusTo` is present
- Check for syntax errors in script
- Add `Log()` statements to trace execution

### Status effect not triggering

- Verify hook name is correct (case-sensitive)
- Check duration isn't 0
- Ensure target receives the status

### Game crashes

- Script may have infinite loop (timeout after 500ms)
- Null reference - check all objects exist
- Check server console for stack trace

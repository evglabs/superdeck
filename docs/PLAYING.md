# How to Play SuperDeck

SuperDeck is a deck-building superhero card game where you build characters, collect cards, and battle opponents.

## Table of Contents

- [Getting Started](#getting-started)
- [Game Modes](#game-modes)
- [Character Creation](#character-creation)
- [Battle System](#battle-system)
- [Deck Building](#deck-building)
- [Progression System](#progression-system)
- [Card Types and Suits](#card-types-and-suits)
- [Status Effects](#status-effects)
- [Tips and Strategies](#tips-and-strategies)

---

## Getting Started

### Quick Start (Offline Mode)

The fastest way to play:

```bash
cd superdeck
./run_client.sh
```

This starts a local game with everything you need - no server setup required.

### First Launch

1. Run the client
2. Select **"Offline (Local Server)"** mode
3. Wait for the embedded server to start
4. Create your first character
5. Start battling!

---

## Game Modes

### Offline Mode

- **How it works:** Client runs an embedded server locally
- **Database:** SQLite file stored on your machine
- **Best for:** Single-player experience, testing, learning the game
- **Connection:** `localhost:5000` (automatic)

**To play offline:**
```bash
./run_client.sh
# Select "Offline (Local Server)"
```

### Online Mode

- **How it works:** Connect to a remote server
- **Database:** Shared MariaDB database
- **Best for:** Playing with others, persistent progression
- **Connection:** Requires server URL

**To play online:**
```bash
dotnet run --project src/Client
# Select "Online (Connect to Server)"
# Enter server URL (e.g., http://game.example.com:5000)
# Register or login with your account
```

---

## Character Creation

### Creating a Character

1. From the main menu, select **"Create Character"**
2. Enter a name for your character
3. Choose your starting suit (determines starter deck)

### Starting Suits

| Suit | Playstyle | Strengths |
|------|-----------|-----------|
| **Fire** | Aggressive burst damage | High damage, DoT effects |
| **MartialArts** | Balanced physical | Reliable damage, defense |
| **Magic** | Versatile utility | Buffs, debuffs, flexibility |
| **Electricity** | Speed-focused | Fast attacks, disruption |
| **Mental** | Control-oriented | Manipulation, status effects |

### Character Stats

Every character has three stats you can allocate:

| Stat | Effect |
|------|--------|
| **Attack** | Increases damage dealt |
| **Defense** | Reduces damage taken |
| **Speed** | Determines turn order in battle |

**Starting Values:**
- All stats start at 0
- You gain 1 stat point per level
- Allocate points from the character menu

---

## Battle System

### Battle Flow

Each battle consists of multiple rounds:

```
Round Start
    ↓
Draw Phase (draw 5 cards on round 1, then 3 per round)
    ↓
Queue Phase (select 3 cards to play)
    ↓
Resolution Phase (cards execute based on speed)
    ↓
Cleanup Phase (discard, tick status effects)
    ↓
Check Win Condition
    ↓
Next Round (or Battle End)
```

### Draw Phase

- **Round 1:** Draw 5 cards (starting hand)
- **Subsequent rounds:** Draw 3 cards
- If your deck is empty, your discard pile shuffles back

### Queue Phase

1. View your hand
2. Select up to 3 cards to play (queue slots)
3. Cards are played in the order you queue them
4. You can queue fewer than 3 cards if desired

### Resolution Phase

1. Speed determines who acts first each action
2. Players alternate playing queued cards
3. Higher speed = act first
4. Cards resolve immediately when played
5. Status effects trigger at appropriate times

### Win Conditions

- **Primary:** Reduce opponent's HP to 0
- **System Damage:** Starting round 10, both players take increasing damage (2, 4, 8, 16...) each round to prevent stalemates

### Battle Controls

During your turn:

| Key | Action |
|-----|--------|
| `1-9` | Select card from hand |
| `Enter` | Confirm selection |
| `Q` | Clear current queue |
| `Space` | End queue phase |

---

## Deck Building

### Deck Basics

- **Minimum size:** 9 cards
- **No maximum:** Add as many cards as you want
- **No copy limit:** Can have multiple copies of the same card
- **All cards usable:** Every card you own is in your deck

### Managing Your Deck

From the character menu:

1. **View Deck** - See all cards in your deck
2. **Add Cards** - Add cards from your collection
3. **Remove Cards** - Remove cards from your deck

### Deck Strategy

- **Small decks** (9-12 cards): More consistent, see key cards often
- **Large decks** (15+ cards): More variety, harder to predict

### Starter Deck

When you create a character, you receive:
- 3 Punch cards (basic attack)
- 3 Block cards (basic defense)
- 4 suit-specific cards

---

## Progression System

### Experience Points (XP)

Earn XP from battles:

| Result | XP Gained |
|--------|-----------|
| **Win** | 50 XP |
| **Loss** | 25 XP |

### Leveling Up

| Level | Total XP Required |
|-------|-------------------|
| 1 → 2 | 50 XP |
| 2 → 3 | 125 XP (50 + 75) |
| 3 → 4 | 225 XP (125 + 100) |
| ... | +25 XP per level |
| **Max** | Level 10 |

### Level Benefits

Each level grants:
- +10 Max HP
- +1 Stat Point to allocate
- Access to a Booster Pack

### HP Formula

```
Max HP = 100 + (Level × 10)

Level 1:  110 HP
Level 5:  150 HP
Level 10: 200 HP
```

### MMR (Matchmaking Rating)

- **Starting MMR:** 1000
- **Win:** +25 MMR
- **Loss:** -25 MMR
- **Minimum:** 100 MMR

MMR determines opponent difficulty - you face "ghosts" (AI-controlled copies) of characters near your rating.

---

## Booster Packs

### Earning Packs

You receive a booster pack when you level up.

### Opening Packs

1. 10 cards are displayed
2. Choose up to 3 cards to keep
3. Optionally sacrifice a pick to remove an unwanted card from your pool
4. Unchosen cards return to the pool

### Card Rarity

| Rarity | Drop Rate | Power Level |
|--------|-----------|-------------|
| **Common** | 60% | Basic effects |
| **Uncommon** | 30% | Better effects |
| **Rare** | 9% | Strong effects |
| **Epic** | 0.9% | Powerful combos |
| **Legendary** | 0.1% | Game-changing |

### Suit Weighting

Cards from suits you already own appear more frequently, helping you build focused decks.

---

## Card Types and Suits

### Card Types

| Type | Purpose |
|------|---------|
| **Attack** | Deal damage to opponent |
| **Defense** | Reduce incoming damage, heal |
| **Buff** | Apply positive effects to yourself |
| **Debuff** | Apply negative effects to opponent |
| **Utility** | Draw cards, manipulate queue, etc. |

### Suits

SuperDeck features 15 unique suits:

| Suit | Theme | Key Mechanics |
|------|-------|---------------|
| **Basic** | Foundational | Simple damage/defense |
| **Fire** | Destruction | Burn damage, explosions |
| **MartialArts** | Combat | Combos, counters |
| **Magic** | Mystical | Versatile effects |
| **Electricity** | Energy | Speed, stunning |
| **Mental** | Psychic | Mind control, prediction |
| **Espionage** | Stealth | Evasion, information |
| **Nature** | Elements | Healing, growth |
| **Tech** | Gadgets | Mechanical advantages |
| **Berserker** | Rage | High risk, high reward |
| **Military** | Tactics | Defensive, strategic |
| **Radiation** | Toxic | Poison, decay |
| **Showbiz** | Performance | Unpredictable effects |
| **Speedster** | Velocity | Extra actions, priority |
| **Money** | Wealth | Rare, powerful effects |

### Example Cards

**Basic Kick** (Common, Attack)
> Deal 15 damage

**Fire Phoenix** (Legendary, Buff)
> If you die, revive with 50% HP

**Mental Mindswap** (Rare, Utility)
> Exchange hands with opponent

---

## Status Effects

### How Status Effects Work

- Applied by cards
- Last for a set number of turns
- Trigger on specific "hooks" (events)
- Stack or refresh duration

### Common Hooks

| Hook | When It Triggers |
|------|------------------|
| `OnTurnStart` | Beginning of your turn |
| `OnTurnEnd` | End of your turn |
| `OnTakeDamage` | When you take damage |
| `OnDealDamage` | When you deal damage |
| `OnDeath` | When HP reaches 0 |
| `OnHeal` | When you heal |

### Example Status Effects

**Burning**
- Duration: 3 turns
- Effect: Take 5 damage at turn end
- Applied by: Fire cards

**Shield**
- Duration: Until used
- Effect: Block next instance of damage
- Applied by: Defense cards

**Haste**
- Duration: 1 turn
- Effect: +3 Speed
- Applied by: Speedster cards

---

## Tips and Strategies

### Early Game

1. **Focus your suit** - Pick one or two suits to specialize in
2. **Don't skip stat points** - Allocate every point you earn
3. **Speed matters** - Acting first can be decisive

### Deck Building

1. **Balance attacks and defense** - Pure aggro can be countered
2. **Include utility** - Card draw keeps options open
3. **Synergize** - Some cards combo with each other

### Battle Tactics

1. **Read the opponent** - Ghost opponents have predictable decks
2. **Manage your hand** - Don't queue weak cards just to fill slots
3. **Plan for system damage** - After round 10, end fights quickly

### Advanced Tips

1. **Study card scripts** - Understanding exact mechanics helps
2. **Track opponent's discard** - Know what they've already played
3. **Speed thresholds** - Speed ties favor the player who acted second last
4. **Status stacking** - Multiple debuffs can overwhelm opponents

---

## Controls Reference

### Main Menu

| Key | Action |
|-----|--------|
| `↑/↓` | Navigate menu |
| `Enter` | Select option |
| `Q` | Quit game |

### Battle

| Key | Action |
|-----|--------|
| `1-9` | Select card by number |
| `Enter` | Confirm queue |
| `Backspace` | Remove last queued card |
| `Space` | End queue phase |

### Character Management

| Key | Action |
|-----|--------|
| `A/D/S` | Allocate stat point |
| `V` | View deck |
| `B` | Start battle |

---

## Troubleshooting

### "Server connection failed"

**Offline mode:**
- Wait a few seconds for embedded server to start
- Check if port 5000 is available

**Online mode:**
- Verify server URL is correct
- Check your internet connection
- Confirm server is running

### "Not enough cards in deck"

Your deck must have at least 9 cards. Add more cards or the game will pad with "Wait" cards (which do nothing).

### "Invalid action"

- You may be trying to queue more cards than you have slots
- A status effect may be preventing your action
- The battle phase may have already ended

### Cards not working as expected

Some cards have specific conditions:
- Check the card description
- Status effects may modify behavior
- Speed/defense calculations happen before damage

---

## Glossary

| Term | Definition |
|------|------------|
| **Queue** | Cards selected to play this round |
| **Resolve** | When a card's effect actually happens |
| **Hook** | Event trigger point for status effects |
| **Ghost** | AI copy of another player's character |
| **MMR** | Matchmaking Rating (Elo-based) |
| **DoT** | Damage over Time |
| **Buff** | Positive status effect |
| **Debuff** | Negative status effect |

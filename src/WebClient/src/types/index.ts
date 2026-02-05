// Enums
export type Suit =
  | 'Basic' | 'Berserker' | 'Electricity' | 'Espionage' | 'Fire'
  | 'Magic' | 'MartialArts' | 'Mental' | 'Military' | 'Money'
  | 'Nature' | 'Radiation' | 'Showbiz' | 'Speedster' | 'Tech'

export const SELECTABLE_SUITS: Suit[] = [
  'Berserker', 'Electricity', 'Espionage', 'Fire', 'Magic',
  'MartialArts', 'Mental', 'Military', 'Nature', 'Radiation',
  'Showbiz', 'Speedster', 'Tech',
]

export type CardType = 'Attack' | 'Defense' | 'Buff' | 'Debuff' | 'Utility'
export type Rarity = 'Common' | 'Uncommon' | 'Rare' | 'Epic' | 'Legendary'
export type BattlePhase = 'NotStarted' | 'Draw' | 'Queue' | 'Resolution' | 'Cleanup' | 'Ended'
export type TargetType = 'Self' | 'Opponent' | 'Both'

// Models
export interface ImmediateEffect {
  target: TargetType
  script: string
}

export interface StatusDefinition {
  name: string
  duration: number
  hooks: Record<string, string>
}

export interface GrantsStatus {
  target: TargetType
  status: StatusDefinition
}

export interface StatusEffect {
  id: string
  name: string
  duration: number
  remainingDuration: number
  sourceCardId: string
  isBuff: boolean
  hookScripts: Record<string, string>
  appliedAt: string
}

export interface Card {
  id: string
  name: string
  suit: Suit
  type: CardType
  rarity: Rarity
  description: string
  immediateEffect: ImmediateEffect | null
  grantsStatusTo: GrantsStatus | null
  isWaitCard: boolean
  isGhost: boolean
  targetSelf: boolean
}

export interface BattleStats {
  attack: number
  defense: number
  speed: number
}

export interface Character {
  id: string
  name: string
  level: number
  xp: number
  attack: number
  defense: number
  speed: number
  bonusHP: number
  battleStats: BattleStats | null
  currentHP: number
  maxHP: number
  deckCardIds: string[]
  wins: number
  losses: number
  mmr: number
  isGhost: boolean
  isPublished: boolean
  ownerPlayerId: string | null
  activeStatuses: StatusEffect[]
  money: number
  turnsWithoutDamage: number
  hasPriority: boolean
  lastTurnPlayedCards: Card[]
}

export interface EffectiveStats {
  attack: number
  defense: number
  speed: number
}

export interface BattleState {
  battleId: string
  round: number
  phase: BattlePhase
  player: Character
  opponent: Character
  playerGoesFirst: boolean
  currentQueueIndex: number
  playerDeck: Card[]
  playerHand: Card[]
  playerQueue: Card[]
  playerDiscard: Card[]
  opponentDeck: Card[]
  opponentHand: Card[]
  opponentQueue: Card[]
  opponentDiscard: Card[]
  playerStatuses: StatusEffect[]
  opponentStatuses: StatusEffect[]
  playerEffectiveStats: EffectiveStats | null
  opponentEffectiveStats: EffectiveStats | null
  baseQueueSlots: number
  maxQueueSlots: number
  currentPlayerQueueSlots: number
  currentOpponentQueueSlots: number
  startingHandSize: number
  cardsDrawnPerTurn: number
  battleLog: string[]
  events: import('./events').BattleEvent[]
  winnerId: string | null
  isComplete: boolean
  bothWin: boolean
  winner: Character | null
  overkillDamage: number
  cardsPlayedThisTurn: number
  queueSize: number
  opponentQueueSize: number
  allowDeckQueue: boolean
  opponentQueueRevealed: boolean
  opponentHandRevealed: boolean
  echoCard: Card | null
  echoEffectiveness: number
  mirrorNextResolve: boolean
  mirrorEffectiveness: number
  suspendedCards: Card[]
}

export interface PlayerAction {
  action: string
  handIndex?: number
  queueSlot?: number
}

export interface BoosterPack {
  id: string
  characterId: string
  cards: Card[]
  createdAt: string
}

// Request DTOs
export interface CreateCharacterRequest {
  name: string
  suitChoice: Suit
  playerId?: string
}

export interface UpdateStatsRequest {
  attack: number
  defense: number
  speed: number
  bonusHP?: number
}

export interface AddCardsRequest {
  cardIds: string[]
}

export interface RemoveCardsRequest {
  cardIds: string[]
}

export interface StartBattleRequest {
  characterId: string
  seed?: number
  autoBattle?: boolean
  autoBattleMode?: string
  aiProfileId?: string
}

export interface ToggleAutoBattleRequest {
  enabled: boolean
  aiProfileId?: string
}

export interface GeneratePackRequest {
  characterId: string
}

// Response DTOs
export interface AuthResponse {
  token: string
  playerId: string
  username: string
}

export interface PlayerInfoResponse {
  playerId: string
  username: string
  totalWins: number
  totalLosses: number
  highestMMR: number
  totalBattles: number
}

export interface StartBattleResponse {
  battleId: string
  battleState: BattleState
}

export interface ActionResponse {
  valid: boolean
  message: string | null
  battleState: BattleState
}

export interface BattleResult {
  battleId: string
  winnerId: string
  playerWon: boolean
  xpGained: number
  mmrChange: number
  levelsGained: number
  newLevel: number
  battleLog: string[]
}

export interface InstantBattleResponse {
  battleId: string
  result: BattleResult
  battleLog: string[]
  totalRounds: number
}

export interface ServerInfo {
  version: string
  cardCount: number
  settings: ServerSettings
}

export interface ServerSettings {
  baseHP: number
  hpPerLevel: number
  maxLevel: number
  baseQueueSlots: number
  statPointsPerLevel: number
  hpPerStatPoint: number
  attackPercentPerPoint: number
  defensePercentPerPoint: number
  autoBattleWatchDelayMs: number
}

export interface HealthResponse {
  status: string
  timestamp: string
}

import type { Card } from './index'

/**
 * Base type for all battle events.
 * Events are emitted during battle resolution and can be played back by the client.
 */
export interface BattleEventBase {
  id: string
  sequenceNumber: number
  eventType: string
  suggestedDelayMs: number
  /** Number of battle log entries when this event was emitted */
  battleLogLength: number
}

export interface RoundStartEvent extends BattleEventBase {
  eventType: 'round_start'
  roundNumber: number
  playerHP: number
  opponentHP: number
}

export interface SpeedRollEvent extends BattleEventBase {
  eventType: 'speed_roll'
  playerSpeed: number
  opponentSpeed: number
  playerGoesFirst: boolean
  playerName: string
  opponentName: string
}

export interface CardPlayedEvent extends BattleEventBase {
  eventType: 'card_played'
  casterName: string
  casterIsPlayer: boolean
  card: Card
  targetName: string
  targetIsPlayer: boolean
}

export interface DamageDealtEvent extends BattleEventBase {
  eventType: 'damage_dealt'
  amount: number
  baseDamage: number
  finalDamage: number
  targetIsPlayer: boolean
  targetName: string
  sourceName: string
  targetHPBefore: number
  targetHPAfter: number
  isDOT: boolean
  dotSourceName: string | null
}

export interface HealingEvent extends BattleEventBase {
  eventType: 'healing'
  amount: number
  targetIsPlayer: boolean
  targetName: string
  targetHPBefore: number
  targetHPAfter: number
}

export interface StatusGainedEvent extends BattleEventBase {
  eventType: 'status_gained'
  statusName: string
  duration: number
  isBuff: boolean
  targetIsPlayer: boolean
  targetName: string
  sourceCardName: string
}

export interface StatusExpiredEvent extends BattleEventBase {
  eventType: 'status_expired'
  statusName: string
  wasOnPlayer: boolean
  targetName: string
}

export interface StatusTriggeredEvent extends BattleEventBase {
  eventType: 'status_triggered'
  statusName: string
  hookType: string
  ownerIsPlayer: boolean
  ownerName: string
  message: string
}

export interface BattleEndEvent extends BattleEventBase {
  eventType: 'battle_end'
  winnerId: string
  winnerName: string
  playerWon: boolean
  playerFinalHP: number
  opponentFinalHP: number
  overkillDamage: number
  reason: string
}

export interface MessageEvent extends BattleEventBase {
  eventType: 'message'
  message: string
  category: string
}

/**
 * Union type for all battle events
 */
export type BattleEvent =
  | RoundStartEvent
  | SpeedRollEvent
  | CardPlayedEvent
  | DamageDealtEvent
  | HealingEvent
  | StatusGainedEvent
  | StatusExpiredEvent
  | StatusTriggeredEvent
  | BattleEndEvent
  | MessageEvent

/**
 * Type guard to check if an event is a specific type
 */
export function isEventType<T extends BattleEvent>(
  event: BattleEvent,
  type: T['eventType']
): event is T {
  return event.eventType === type
}

/**
 * Animation state derived from events during playback
 */
export interface AnimationState {
  playerHP: number
  opponentHP: number
  currentEventIndex: number
  isPlaying: boolean
  currentEvent: BattleEvent | null
}

/**
 * Default animation delays by event type (in ms)
 */
export const DEFAULT_DELAYS: Record<BattleEvent['eventType'], number> = {
  round_start: 500,
  speed_roll: 600,
  card_played: 500,
  damage_dealt: 400,
  healing: 300,
  status_gained: 300,
  status_expired: 200,
  status_triggered: 300,
  battle_end: 800,
  message: 200,
}

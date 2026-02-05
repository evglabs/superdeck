import { useState, useCallback, useEffect, useRef } from 'react'
import type { BattleState } from '../types'
import type { BattleEvent, AnimationState } from '../types/events'

interface UseAnimationQueueOptions {
  autoPlay?: boolean
  speedMultiplier?: number
}

interface UseAnimationQueueReturn {
  animationState: AnimationState
  isPlaying: boolean
  isPaused: boolean
  isComplete: boolean
  currentEvent: BattleEvent | null
  displayedPlayerHP: number
  displayedOpponentHP: number
  /** Number of log entries to show (synced with current event) */
  visibleLogLength: number
  play: () => void
  pause: () => void
  skipToEnd: () => void
  reset: () => void
}

export function useAnimationQueue(
  events: BattleEvent[],
  finalState: BattleState | null,
  options: UseAnimationQueueOptions = {}
): UseAnimationQueueReturn {
  const { autoPlay = true, speedMultiplier = 1 } = options

  // Use ref to make speedMultiplier reactive without recreating callbacks
  const speedMultiplierRef = useRef(speedMultiplier)
  speedMultiplierRef.current = speedMultiplier

  // Track the index we should start playing from (last fully processed)
  const playbackStartRef = useRef<number>(0)

  // Animation state
  const [currentEventIndex, setCurrentEventIndex] = useState(-1)
  const [isPlaying, setIsPlaying] = useState(false)
  const [isPaused, setIsPaused] = useState(false)

  // Displayed HP values (animated)
  const [displayedPlayerHP, setDisplayedPlayerHP] = useState(
    finalState?.player.currentHP ?? 100
  )
  const [displayedOpponentHP, setDisplayedOpponentHP] = useState(
    finalState?.opponent.currentHP ?? 100
  )

  // Timer ref for cleanup
  const timerRef = useRef<number | null>(null)

  // Check if playback is complete (caught up to all events)
  const isComplete = currentEventIndex >= events.length - 1

  // Get current event
  const currentEvent = currentEventIndex >= 0 && currentEventIndex < events.length
    ? events[currentEventIndex]
    : null

  // Apply event effects to displayed state
  const applyEvent = useCallback((event: BattleEvent) => {
    switch (event.eventType) {
      case 'damage_dealt':
        if (event.targetIsPlayer) {
          setDisplayedPlayerHP(event.targetHPAfter)
        } else {
          setDisplayedOpponentHP(event.targetHPAfter)
        }
        break
      case 'healing':
        if (event.targetIsPlayer) {
          setDisplayedPlayerHP(event.targetHPAfter)
        } else {
          setDisplayedOpponentHP(event.targetHPAfter)
        }
        break
      case 'round_start':
        setDisplayedPlayerHP(event.playerHP)
        setDisplayedOpponentHP(event.opponentHP)
        break
      case 'battle_end':
        setDisplayedPlayerHP(event.playerFinalHP)
        setDisplayedOpponentHP(event.opponentFinalHP)
        break
    }
  }, [])

  // Advance to next event
  const advanceEvent = useCallback(() => {
    setCurrentEventIndex(prev => {
      const nextIndex = prev + 1
      if (nextIndex < events.length) {
        const event = events[nextIndex]
        applyEvent(event)
        console.log(`[AnimationQueue] Playing event ${nextIndex}: ${event.eventType}`)

        // Schedule next advance (use ref for reactive speed changes)
        const delay = event.suggestedDelayMs / speedMultiplierRef.current
        timerRef.current = window.setTimeout(() => {
          advanceEvent()
        }, delay)

        return nextIndex
      } else {
        // Playback complete - update start ref for next batch
        console.log('[AnimationQueue] Playback complete')
        setIsPlaying(false)
        playbackStartRef.current = events.length
        return prev
      }
    })
  }, [events, applyEvent])

  // Play animation from current position (or from playbackStart for new events)
  const play = useCallback(() => {
    if (events.length === 0) return

    setIsPlaying(true)
    setIsPaused(false)

    // If we haven't started yet or we're caught up, start from playbackStart
    setCurrentEventIndex(prev => {
      const startFrom = prev < playbackStartRef.current ? playbackStartRef.current - 1 : prev
      return startFrom
    })

    // Small delay then advance
    setTimeout(() => advanceEvent(), 50)
  }, [events.length, advanceEvent])

  // Pause animation
  const pause = useCallback(() => {
    setIsPlaying(false)
    setIsPaused(true)
    if (timerRef.current) {
      clearTimeout(timerRef.current)
      timerRef.current = null
    }
  }, [])

  // Skip to end - jump to final state
  const skipToEnd = useCallback(() => {
    if (timerRef.current) {
      clearTimeout(timerRef.current)
      timerRef.current = null
    }
    setIsPlaying(false)
    setIsPaused(false)

    // Apply final state
    if (finalState) {
      setDisplayedPlayerHP(finalState.player.currentHP)
      setDisplayedOpponentHP(finalState.opponent.currentHP)
    }
    setCurrentEventIndex(events.length - 1)
    playbackStartRef.current = events.length
  }, [events.length, finalState])

  // Reset to initial state
  const reset = useCallback(() => {
    if (timerRef.current) {
      clearTimeout(timerRef.current)
      timerRef.current = null
    }
    setCurrentEventIndex(-1)
    setIsPlaying(false)
    setIsPaused(false)
    playbackStartRef.current = 0

    // Reset to initial HP (from first round_start event or final state)
    const firstRoundStart = events.find(e => e.eventType === 'round_start')
    if (firstRoundStart && firstRoundStart.eventType === 'round_start') {
      setDisplayedPlayerHP(firstRoundStart.playerHP)
      setDisplayedOpponentHP(firstRoundStart.opponentHP)
    } else if (finalState) {
      setDisplayedPlayerHP(finalState.player.maxHP)
      setDisplayedOpponentHP(finalState.opponent.maxHP)
    }
  }, [events, finalState])

  // Handle new events arriving - auto-play only the NEW events
  useEffect(() => {
    const newCount = events.length
    const lastProcessed = playbackStartRef.current

    if (newCount > lastProcessed && !isPlaying && !isPaused) {
      const newEventCount = newCount - lastProcessed
      console.log(`[AnimationQueue] ${newEventCount} new events (${lastProcessed} -> ${newCount})`)

      if (autoPlay) {
        console.log('[AnimationQueue] Auto-playing new events')
        setTimeout(() => play(), 100)
      }
    }
  }, [events.length, autoPlay, isPlaying, isPaused, play])

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (timerRef.current) {
        clearTimeout(timerRef.current)
      }
    }
  }, [])

  // Initialize HP from final state when battle first loads (before any events)
  useEffect(() => {
    if (finalState && events.length === 0 && playbackStartRef.current === 0) {
      setDisplayedPlayerHP(finalState.player.currentHP)
      setDisplayedOpponentHP(finalState.opponent.currentHP)
    }
  }, [finalState, events.length])

  const animationState: AnimationState = {
    playerHP: displayedPlayerHP,
    opponentHP: displayedOpponentHP,
    currentEventIndex,
    isPlaying,
    currentEvent,
  }

  // Calculate visible log length based on current event
  // If not playing or no current event, show full log
  const visibleLogLength = currentEvent?.battleLogLength ??
    (events.length > 0 ? events[events.length - 1]?.battleLogLength ?? Infinity : Infinity)

  return {
    animationState,
    isPlaying,
    isPaused,
    isComplete,
    currentEvent,
    displayedPlayerHP,
    displayedOpponentHP,
    visibleLogLength,
    play,
    pause,
    skipToEnd,
    reset,
  }
}

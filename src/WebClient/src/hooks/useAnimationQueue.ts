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

  // Track which events we've already processed (by their starting sequence)
  const processedEventsRef = useRef<number>(0)

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

  // Check if playback is complete
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

        // Schedule next advance
        const delay = event.suggestedDelayMs / speedMultiplier
        timerRef.current = window.setTimeout(() => {
          advanceEvent()
        }, delay)

        return nextIndex
      } else {
        // Playback complete
        setIsPlaying(false)
        return prev
      }
    })
  }, [events, applyEvent, speedMultiplier])

  // Play animation
  const play = useCallback(() => {
    if (events.length === 0) return
    if (isComplete) {
      // If complete, restart from beginning
      setCurrentEventIndex(-1)
    }
    setIsPlaying(true)
    setIsPaused(false)
    advanceEvent()
  }, [events.length, isComplete, advanceEvent])

  // Pause animation
  const pause = useCallback(() => {
    setIsPlaying(false)
    setIsPaused(true)
    if (timerRef.current) {
      clearTimeout(timerRef.current)
      timerRef.current = null
    }
  }, [])

  // Skip to end
  const skipToEnd = useCallback(() => {
    if (timerRef.current) {
      clearTimeout(timerRef.current)
      timerRef.current = null
    }
    setIsPlaying(false)
    setIsPaused(false)

    // Apply all events and jump to final state
    if (finalState) {
      setDisplayedPlayerHP(finalState.player.currentHP)
      setDisplayedOpponentHP(finalState.opponent.currentHP)
    }
    setCurrentEventIndex(events.length - 1)
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

  // Handle new events arriving (e.g., after resolution)
  useEffect(() => {
    if (events.length > processedEventsRef.current) {
      // New events have arrived
      const newEventsStart = processedEventsRef.current
      processedEventsRef.current = events.length

      // If we have new events and autoPlay is enabled, start playing
      if (autoPlay && newEventsStart > 0 && !isPlaying && !isPaused) {
        play()
      } else if (autoPlay && newEventsStart === 0 && events.length > 0) {
        // First batch of events
        play()
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

  // Initialize HP from final state when it first arrives
  useEffect(() => {
    if (finalState && events.length === 0) {
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

  return {
    animationState,
    isPlaying,
    isPaused,
    isComplete,
    currentEvent,
    displayedPlayerHP,
    displayedOpponentHP,
    play,
    pause,
    skipToEnd,
    reset,
  }
}

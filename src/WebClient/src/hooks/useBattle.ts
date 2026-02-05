import { useReducer, useEffect, useCallback, useRef, useState } from 'react'
import { api } from '../api/client'
import type { BattleState, Card } from '../types'
import type { BattleEvent } from '../types/events'
import { useAnimationQueue } from './useAnimationQueue'

interface BattleUIState {
  state: BattleState | null
  loading: boolean
  error: string | null
  autoBattle: boolean
  lastLogIndex: number
  detailCard: Card | null
  lastProcessedEventIndex: number
}

type BattleAction =
  | { type: 'SET_STATE'; payload: BattleState }
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'SET_AUTO_BATTLE'; payload: boolean }
  | { type: 'UPDATE_LOG_INDEX'; payload: number }
  | { type: 'SET_DETAIL_CARD'; payload: Card | null }
  | { type: 'SET_LAST_EVENT_INDEX'; payload: number }

function reducer(state: BattleUIState, action: BattleAction): BattleUIState {
  switch (action.type) {
    case 'SET_STATE':
      return { ...state, state: action.payload, loading: false, error: null }
    case 'SET_LOADING':
      return { ...state, loading: action.payload }
    case 'SET_ERROR':
      return { ...state, error: action.payload, loading: false }
    case 'SET_AUTO_BATTLE':
      return { ...state, autoBattle: action.payload }
    case 'UPDATE_LOG_INDEX':
      return { ...state, lastLogIndex: action.payload }
    case 'SET_DETAIL_CARD':
      return { ...state, detailCard: action.payload }
    case 'SET_LAST_EVENT_INDEX':
      return { ...state, lastProcessedEventIndex: action.payload }
    default:
      return state
  }
}

export function useBattle(battleId: string) {
  const [uiState, dispatch] = useReducer(reducer, {
    state: null,
    loading: true,
    error: null,
    autoBattle: false,
    lastLogIndex: 0,
    detailCard: null,
    lastProcessedEventIndex: -1,
  })

  const pollingRef = useRef(false)
  const [speedMultiplier, setSpeedMultiplier] = useState(1)

  // Get events from state, defaulting to empty array
  const events: BattleEvent[] = uiState.state?.events ?? []

  // Debug: log when events change
  useEffect(() => {
    if (uiState.state) {
      console.log(`[useBattle] State updated - Phase: ${uiState.state.phase}, Events: ${events.length}`)
      if (events.length > 0) {
        console.log('[useBattle] First event:', events[0])
      }
    }
  }, [uiState.state, events.length])

  // Animation queue for event playback
  const animation = useAnimationQueue(events, uiState.state, {
    autoPlay: true,
    speedMultiplier,
  })

  // Initial load
  useEffect(() => {
    api.getBattleState(battleId)
      .then(s => dispatch({ type: 'SET_STATE', payload: s }))
      .catch(err => dispatch({ type: 'SET_ERROR', payload: err.message }))
  }, [battleId])

  // Polling for non-Queue, non-complete phases
  useEffect(() => {
    const { state } = uiState
    if (!state) return
    if (state.phase === 'Queue' || state.isComplete || state.phase === 'Ended') return

    pollingRef.current = true
    const interval = setInterval(async () => {
      if (!pollingRef.current) return
      try {
        const s = await api.getBattleState(battleId)
        dispatch({ type: 'SET_STATE', payload: s })
      } catch {
        // ignore polling errors
      }
    }, 300)

    return () => {
      pollingRef.current = false
      clearInterval(interval)
    }
  }, [battleId, uiState.state?.phase, uiState.state?.isComplete])

  // Auto-battle: auto-queue when in Queue phase
  useEffect(() => {
    const { state, autoBattle } = uiState
    if (!state || !autoBattle || state.phase !== 'Queue' || state.isComplete) return

    const timeout = setTimeout(async () => {
      try {
        const res = await api.autoQueue(battleId)
        if (res.valid) {
          dispatch({ type: 'SET_STATE', payload: res.battleState })
        } else {
          dispatch({ type: 'SET_AUTO_BATTLE', payload: false })
          dispatch({ type: 'SET_ERROR', payload: res.message || 'Auto-battle failed' })
        }
      } catch {
        dispatch({ type: 'SET_AUTO_BATTLE', payload: false })
      }
    }, 500)

    return () => clearTimeout(timeout)
  }, [battleId, uiState.autoBattle, uiState.state?.phase, uiState.state?.round])

  const queueCard = useCallback(async (handIndex: number) => {
    dispatch({ type: 'SET_LOADING', payload: true })
    try {
      const res = await api.submitAction(battleId, { action: 'queue_card', handIndex })
      if (res.valid) {
        dispatch({ type: 'SET_STATE', payload: res.battleState })
      } else {
        dispatch({ type: 'SET_ERROR', payload: res.message })
      }
    } catch (err) {
      dispatch({ type: 'SET_ERROR', payload: err instanceof Error ? err.message : 'Failed' })
    }
  }, [battleId])

  const confirmQueue = useCallback(async () => {
    dispatch({ type: 'SET_LOADING', payload: true })
    try {
      const res = await api.submitAction(battleId, { action: 'confirm_queue' })
      if (res.valid) {
        dispatch({ type: 'SET_STATE', payload: res.battleState })
      } else {
        dispatch({ type: 'SET_ERROR', payload: res.message })
      }
    } catch (err) {
      dispatch({ type: 'SET_ERROR', payload: err instanceof Error ? err.message : 'Failed' })
    }
  }, [battleId])

  const forfeit = useCallback(async () => {
    dispatch({ type: 'SET_LOADING', payload: true })
    try {
      const s = await api.forfeitBattle(battleId)
      dispatch({ type: 'SET_STATE', payload: s })
    } catch (err) {
      dispatch({ type: 'SET_ERROR', payload: err instanceof Error ? err.message : 'Failed' })
    }
  }, [battleId])

  const toggleAutoBattle = useCallback(async () => {
    const newEnabled = !uiState.autoBattle
    dispatch({ type: 'SET_AUTO_BATTLE', payload: newEnabled })
    try {
      await api.toggleAutoBattle(battleId, newEnabled)
    } catch {
      // toggle locally even if server call fails
    }
  }, [battleId, uiState.autoBattle])

  const setDetailCard = useCallback((card: Card | null) => {
    dispatch({ type: 'SET_DETAIL_CARD', payload: card })
  }, [])

  const updateLogIndex = useCallback((index: number) => {
    dispatch({ type: 'UPDATE_LOG_INDEX', payload: index })
  }, [])

  return {
    ...uiState,
    queueCard,
    confirmQueue,
    forfeit,
    toggleAutoBattle,
    setDetailCard,
    updateLogIndex,
    // Animation controls
    animation,
    speedMultiplier,
    setSpeedMultiplier,
  }
}

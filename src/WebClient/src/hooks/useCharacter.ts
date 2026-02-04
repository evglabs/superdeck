import { useState, useCallback } from 'react'
import { api } from '../api/client'
import type { Character, Card } from '../types'

export function useCharacter(characterId: string | undefined) {
  const [character, setCharacter] = useState<Character | null>(null)
  const [loading, setLoading] = useState(false)

  const refresh = useCallback(async () => {
    if (!characterId) return null
    setLoading(true)
    try {
      const c = await api.getCharacter(characterId)
      setCharacter(c)
      return c
    } catch {
      return null
    } finally {
      setLoading(false)
    }
  }, [characterId])

  const addCards = useCallback(async (cardIds: string[]) => {
    if (!characterId) return
    const updated = await api.addCards(characterId, cardIds)
    setCharacter(updated)
    return updated
  }, [characterId])

  const removeCards = useCallback(async (cardIds: string[]) => {
    if (!characterId) return
    const updated = await api.removeCards(characterId, cardIds)
    setCharacter(updated)
    return updated
  }, [characterId])

  return { character, loading, refresh, addCards, removeCards, setCharacter }
}

import { createContext, useContext, useState, useEffect, useCallback, type ReactNode } from 'react'
import { api } from '../api/client'
import type { ServerSettings, Character } from '../types'

interface GameContextValue {
  settings: ServerSettings | null
  currentCharacter: Character | null
  setCurrentCharacter: (c: Character | null) => void
  refreshCharacter: (id: string) => Promise<Character | null>
  serverOnline: boolean
}

const defaultSettings: ServerSettings = {
  baseHP: 100,
  hpPerLevel: 10,
  maxLevel: 10,
  baseQueueSlots: 3,
  statPointsPerLevel: 1,
  hpPerStatPoint: 5,
  attackPercentPerPoint: 2.0,
  defensePercentPerPoint: 2.0,
  autoBattleWatchDelayMs: 500,
}

const GameContext = createContext<GameContextValue | null>(null)

export function GameProvider({ children }: { children: ReactNode }) {
  const [settings, setSettings] = useState<ServerSettings | null>(null)
  const [currentCharacter, setCurrentCharacter] = useState<Character | null>(null)
  const [serverOnline, setServerOnline] = useState(false)

  useEffect(() => {
    api.getServerInfo()
      .then(info => {
        setSettings(info.settings)
        setServerOnline(true)
      })
      .catch(() => {
        setSettings(defaultSettings)
        setServerOnline(false)
      })
  }, [])

  const refreshCharacter = useCallback(async (id: string): Promise<Character | null> => {
    try {
      const c = await api.getCharacter(id)
      setCurrentCharacter(c)
      return c
    } catch {
      return null
    }
  }, [])

  return (
    <GameContext.Provider value={{ settings, currentCharacter, setCurrentCharacter, refreshCharacter, serverOnline }}>
      {children}
    </GameContext.Provider>
  )
}

export function useGame(): GameContextValue {
  const ctx = useContext(GameContext)
  if (!ctx) throw new Error('useGame must be used within GameProvider')
  return ctx
}

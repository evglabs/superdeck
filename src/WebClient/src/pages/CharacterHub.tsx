import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useGame } from '../context/GameContext'
import { api } from '../api/client'
import { LoadingSpinner } from '../components/LoadingSpinner'
import { ConfirmDialog } from '../components/ConfirmDialog'
import type { Character } from '../types'

export function CharacterHub() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { settings, currentCharacter, setCurrentCharacter, refreshCharacter } = useGame()
  const [character, setCharacter] = useState<Character | null>(currentCharacter?.id === id ? currentCharacter : null)
  const [loading, setLoading] = useState(!character)
  const [battleLoading, setBattleLoading] = useState(false)
  const [showDelete, setShowDelete] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!character && id) {
      refreshCharacter(id)
        .then(c => { setCharacter(c); setLoading(false) })
        .catch(() => setLoading(false))
    }
  }, [id])

  useEffect(() => {
    if (currentCharacter?.id === id) setCharacter(currentCharacter)
  }, [currentCharacter])

  if (loading || !character || !settings) return <div className="page"><LoadingSpinner message="Loading character..." /></div>

  const availablePoints = getAvailablePoints(character, settings.statPointsPerLevel, settings.hpPerStatPoint)
  const atkPercent = character.attack * settings.attackPercentPerPoint
  const defPercent = character.defense * settings.defensePercentPerPoint

  const handleStartBattle = async () => {
    setBattleLoading(true)
    setError(null)
    try {
      const res = await api.startBattle(character.id)
      navigate(`/battle/${res.battleId}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to start battle')
      setBattleLoading(false)
    }
  }

  const handleDelete = async () => {
    try {
      await api.deleteCharacter(character.id)
      setCurrentCharacter(null)
      navigate('/menu')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete')
    }
    setShowDelete(false)
  }

  return (
    <div className="page">
      <h1 className="page-title">{character.name}</h1>

      {error && <div className="panel" style={{ borderColor: 'var(--color-danger)', color: 'var(--color-danger)' }}>{error}</div>}

      <div className="panel">
        <div className="stats-grid" style={{ fontSize: '0.95rem' }}>
          <StatRow label="Level" value={`${character.level} / ${settings.maxLevel}`} />
          <StatRow label="XP" value={`${character.xp}`} />
          <StatRow
            label="HP"
            value={`${character.maxHP}`}
            detail={character.bonusHP > 0 ? `(base ${settings.baseHP + character.level * settings.hpPerLevel} + ${character.bonusHP} bonus)` : undefined}
            color="#22c55e"
          />
          <StatRow
            label="Attack"
            value={`${character.attack}`}
            detail={atkPercent > 0 ? `(+${atkPercent.toFixed(1)}% dmg)` : undefined}
            color="#ef4444"
          />
          <StatRow
            label="Defense"
            value={`${character.defense}`}
            detail={defPercent > 0 ? `(-${defPercent.toFixed(1)}% dmg taken)` : undefined}
            color="#3b82f6"
          />
          <StatRow label="Speed" value={`${character.speed}`} color="#eab308" />
          <StatRow label="Wins" value={`${character.wins}`} color="#22c55e" />
          <StatRow label="Losses" value={`${character.losses}`} color="#ef4444" />
          <StatRow label="MMR" value={`${character.mmr}`} color="#eab308" />
          <StatRow label="Deck Size" value={`${character.deckCardIds.length} cards`} />
        </div>
      </div>

      {availablePoints > 0 && (
        <div className="panel" style={{ borderColor: 'var(--color-success)', background: 'rgba(34,197,94,0.05)' }}>
          <span style={{ color: 'var(--color-success)', fontWeight: 600 }}>+{availablePoints} stat point(s) available!</span>
        </div>
      )}

      <div className="flex flex-col gap-2 mt-4">
        <button className="btn-primary" style={{ padding: 12 }} onClick={handleStartBattle} disabled={battleLoading}>
          {battleLoading ? 'Starting Battle...' : 'Start Battle'}
        </button>
        <button className="btn-secondary" style={{ padding: 12 }} onClick={() => navigate(`/character/${character.id}/deck`)}>
          View Deck
        </button>
        {availablePoints > 0 && (
          <button className="btn-success" style={{ padding: 12 }} onClick={() => navigate(`/character/${character.id}/stats`)}>
            Allocate Stats (+{availablePoints})
          </button>
        )}
        <button className="btn-danger" style={{ padding: 12 }} onClick={() => setShowDelete(true)}>
          Delete Character
        </button>
        <button className="btn-secondary" style={{ padding: 12 }} onClick={() => navigate('/menu')}>
          Back to Menu
        </button>
      </div>

      {showDelete && (
        <ConfirmDialog
          title="Delete Character"
          message={`Are you sure you want to delete ${character.name}? This cannot be undone.`}
          confirmLabel="Delete"
          danger
          onConfirm={handleDelete}
          onCancel={() => setShowDelete(false)}
        />
      )}
    </div>
  )
}

function StatRow({ label, value, detail, color }: { label: string; value: string; detail?: string; color?: string }) {
  return (
    <>
      <span className="text-muted">{label}</span>
      <span>
        <span style={{ fontWeight: 600, color }}>{value}</span>
        {detail && <span className="text-muted" style={{ fontSize: '0.85rem', marginLeft: 6 }}>{detail}</span>}
      </span>
    </>
  )
}

function getAvailablePoints(character: Character, statPointsPerLevel: number, hpPerStatPoint: number): number {
  const totalAllowed = character.level * statPointsPerLevel
  const hpPoints = Math.floor(character.bonusHP / hpPerStatPoint)
  const totalUsed = character.attack + character.defense + character.speed + hpPoints
  return Math.max(0, totalAllowed - totalUsed)
}

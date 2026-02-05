import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useGame } from '../context/GameContext'
import { api } from '../api/client'
import { LoadingSpinner } from '../components/LoadingSpinner'
import { StatStepper } from '../components/StatStepper'

export function StatAllocation() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { settings, currentCharacter, refreshCharacter } = useGame()
  const [attack, setAttack] = useState(0)
  const [defense, setDefense] = useState(0)
  const [speed, setSpeed] = useState(0)
  const [bonusHP, setBonusHP] = useState(0)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const character = currentCharacter?.id === id ? currentCharacter : null

  useEffect(() => {
    if (!character && id) refreshCharacter(id)
  }, [id])

  useEffect(() => {
    if (character) {
      setAttack(character.attack)
      setDefense(character.defense)
      setSpeed(character.speed)
      setBonusHP(character.bonusHP)
    }
  }, [character?.id])

  if (!character || !settings) return <div className="page"><LoadingSpinner /></div>

  const hpPerStatPoint = settings.hpPerStatPoint
  const totalAllowed = character.level * settings.statPointsPerLevel
  const hpPoints = Math.floor(bonusHP / hpPerStatPoint)
  const totalUsed = attack + defense + speed + hpPoints
  const remaining = Math.max(0, totalAllowed - totalUsed)

  const handleSave = async () => {
    setSaving(true)
    setError(null)
    try {
      await api.updateStats(character.id, attack, defense, speed, bonusHP)
      await refreshCharacter(character.id)
      navigate(`/character/${character.id}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="page" style={{ maxWidth: 'min(500px, 100%)' }}>
      <h1 className="page-title">Allocate Stats</h1>

      <div className="panel" style={{ marginBottom: 16 }}>
        <div style={{ textAlign: 'center', marginBottom: 20 }}>
          <span style={{ fontSize: '1.3rem', fontWeight: 700, color: remaining > 0 ? 'var(--color-success)' : 'var(--color-text-secondary)' }}>
            {remaining} point(s) remaining
          </span>
        </div>

        <div className="flex flex-col gap-3">
          <StatStepper
            label={`HP (+${hpPerStatPoint} per point)`}
            value={bonusHP}
            displayValue={`+${bonusHP}`}
            color="#22c55e"
            onIncrement={() => { if (remaining > 0) setBonusHP(bonusHP + hpPerStatPoint) }}
            onDecrement={() => { if (bonusHP >= hpPerStatPoint) setBonusHP(bonusHP - hpPerStatPoint) }}
            canIncrement={remaining > 0}
            canDecrement={bonusHP >= hpPerStatPoint}
          />
          <StatStepper
            label={`Attack (+${settings.attackPercentPerPoint}% dmg per point)`}
            value={attack}
            color="#ef4444"
            onIncrement={() => { if (remaining > 0) setAttack(attack + 1) }}
            onDecrement={() => { if (attack > 0) setAttack(attack - 1) }}
            canIncrement={remaining > 0}
            canDecrement={attack > 0}
          />
          <StatStepper
            label={`Defense (-${settings.defensePercentPerPoint}% dmg taken per point)`}
            value={defense}
            color="#3b82f6"
            onIncrement={() => { if (remaining > 0) setDefense(defense + 1) }}
            onDecrement={() => { if (defense > 0) setDefense(defense - 1) }}
            canIncrement={remaining > 0}
            canDecrement={defense > 0}
          />
          <StatStepper
            label="Speed"
            value={speed}
            color="#eab308"
            onIncrement={() => { if (remaining > 0) setSpeed(speed + 1) }}
            onDecrement={() => { if (speed > 1) setSpeed(speed - 1) }}
            canIncrement={remaining > 0}
            canDecrement={speed > 1}
          />
        </div>
      </div>

      {error && <div className="panel" style={{ color: 'var(--color-danger)', borderColor: 'var(--color-danger)' }}>{error}</div>}

      <div className="flex gap-2">
        <button className="btn-secondary" onClick={() => navigate(`/character/${character.id}`)}>Cancel</button>
        <button className="btn-primary" onClick={handleSave} disabled={saving}>
          {saving ? 'Saving...' : 'Save Stats'}
        </button>
      </div>
    </div>
  )
}

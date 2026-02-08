import { useState, useEffect } from 'react'
import { useParams, useNavigate, useLocation } from 'react-router-dom'
import { api } from '../api/client'
import { LoadingSpinner } from '../components/LoadingSpinner'
import type { CareerSummaryResponse, BattleResult } from '../types'

export function RetirementPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const location = useLocation()
  const [career, setCareer] = useState<CareerSummaryResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [battleLoading, setBattleLoading] = useState(false)

  // Check if we just retired (coming from battle result)
  const justRetired = (location.state as { result?: BattleResult })?.result?.characterRetired

  useEffect(() => {
    if (!id) return
    api.getCareerSummary(id)
      .then(setCareer)
      .catch(err => setError(err instanceof Error ? err.message : 'Failed to load career'))
      .finally(() => setLoading(false))
  }, [id])

  const handleUberChallenge = async () => {
    if (!id) return
    setBattleLoading(true)
    try {
      const res = await api.startBattle(id, { battleType: 'uber' })
      navigate(`/battle/${res.battleId}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to start UBER battle')
      setBattleLoading(false)
    }
  }

  if (loading) return <div className="page"><LoadingSpinner message="Loading career..." /></div>

  if (error) {
    return (
      <div className="page text-center">
        <p style={{ color: 'var(--color-danger)' }}>{error}</p>
        <button className="btn-secondary mt-4" onClick={() => navigate('/menu')}>Back to Menu</button>
      </div>
    )
  }

  if (!career) return null

  const careerDays = career.retiredAt && career.createdAt
    ? Math.max(1, Math.ceil((new Date(career.retiredAt).getTime() - new Date(career.createdAt).getTime()) / (1000 * 60 * 60 * 24)))
    : 0

  return (
    <div className="page" style={{ maxWidth: 'min(500px, 100%)' }}>
      {/* Trophy celebration header */}
      <div style={{ textAlign: 'center', marginBottom: 24, marginTop: 20 }}>
        <div style={{ fontSize: '4rem', marginBottom: 8 }}>&#127942;</div>
        <h1 style={{
          fontSize: 'clamp(1.5rem, 4vw, 2rem)',
          fontWeight: 800,
          color: '#a855f7',
          margin: 0
        }}>
          {justRetired ? 'RETIRED!' : 'RETIRED CHAMPION'}
        </h1>
        {justRetired && (
          <p className="text-muted" style={{ marginTop: 8 }}>
            Your character has reached the pinnacle and retires as a champion!
          </p>
        )}
      </div>

      {/* Career stats */}
      <div className="panel" style={{
        background: 'rgba(168, 85, 247, 0.05)',
        borderColor: '#a855f7'
      }}>
        <h3 style={{ marginBottom: 16, color: '#a855f7' }}>Career Summary</h3>
        <div style={{ display: 'grid', gridTemplateColumns: 'auto 1fr', gap: '8px 16px', fontSize: '0.95rem' }}>
          <span className="text-muted">Total Battles</span>
          <span style={{ fontWeight: 600 }}>{career.totalBattles}</span>

          <span className="text-muted">Record</span>
          <span>
            <span style={{ color: '#22c55e', fontWeight: 600 }}>{career.wins}W</span>
            {' / '}
            <span style={{ color: '#ef4444', fontWeight: 600 }}>{career.losses}L</span>
          </span>

          <span className="text-muted">Win Rate</span>
          <span style={{ fontWeight: 600, color: career.winRate >= 50 ? '#22c55e' : '#ef4444' }}>
            {career.winRate}%
          </span>

          <span className="text-muted">Final MMR</span>
          <span style={{ fontWeight: 600, color: '#eab308' }}>{career.finalMMR}</span>

          <span className="text-muted">Final Level</span>
          <span style={{ fontWeight: 600 }}>{career.finalLevel}</span>
        </div>
      </div>

      {/* Final stats */}
      <div className="panel">
        <h3 style={{ marginBottom: 16 }}>Final Stats</h3>
        <div style={{ display: 'grid', gridTemplateColumns: 'auto 1fr', gap: '8px 16px', fontSize: '0.95rem' }}>
          <span className="text-muted">Attack</span>
          <span style={{ fontWeight: 600, color: '#ef4444' }}>{career.finalStats.attack}</span>

          <span className="text-muted">Defense</span>
          <span style={{ fontWeight: 600, color: '#3b82f6' }}>{career.finalStats.defense}</span>

          <span className="text-muted">Speed</span>
          <span style={{ fontWeight: 600, color: '#eab308' }}>{career.finalStats.speed}</span>

          <span className="text-muted">Bonus HP</span>
          <span style={{ fontWeight: 600, color: '#22c55e' }}>+{career.finalStats.bonusHP}</span>
        </div>
      </div>

      {/* Career span */}
      <div className="panel">
        <div style={{ display: 'grid', gridTemplateColumns: 'auto 1fr', gap: '8px 16px', fontSize: '0.9rem' }}>
          <span className="text-muted">Career Span</span>
          <span>{careerDays} day{careerDays !== 1 ? 's' : ''}</span>

          <span className="text-muted">Created</span>
          <span>{new Date(career.createdAt).toLocaleDateString()}</span>

          <span className="text-muted">Retired</span>
          <span>{career.retiredAt ? new Date(career.retiredAt).toLocaleDateString() : '-'}</span>
        </div>
      </div>

      {/* Retirement ghost info */}
      <div className="panel" style={{
        background: 'rgba(139, 92, 246, 0.05)',
        borderColor: '#8b5cf6',
        textAlign: 'center'
      }}>
        <div style={{ fontSize: '1.5rem', marginBottom: 4 }}>&#128123;</div>
        <p style={{ margin: 0, fontSize: '0.9rem' }}>
          A <strong>Retirement Ghost</strong> has been created.
        </p>
        <p className="text-muted" style={{ margin: '4px 0 0', fontSize: '0.8rem' }}>
          Other players can battle your champion's ghost!
        </p>
      </div>

      {/* Action buttons */}
      <div className="flex flex-col gap-2 mt-4">
        <button
          className="btn-primary"
          style={{ padding: 12, background: '#ef4444' }}
          onClick={handleUberChallenge}
          disabled={battleLoading}
        >
          {battleLoading ? 'Starting...' : 'Challenge the UBER Boss'}
        </button>
        <p className="text-muted text-center" style={{ fontSize: '0.8rem', margin: '0 0 8px' }}>
          Fight the Ultimate Challenger for glory (no rewards)
        </p>

        <button
          className="btn-secondary"
          style={{ padding: 12 }}
          onClick={() => navigate('/characters/new')}
        >
          Create New Character
        </button>

        <button
          className="btn-secondary"
          style={{ padding: 12 }}
          onClick={() => navigate('/menu')}
        >
          Back to Menu
        </button>
      </div>
    </div>
  )
}

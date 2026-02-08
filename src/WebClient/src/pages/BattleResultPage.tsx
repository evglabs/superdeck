import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useGame } from '../context/GameContext'
import { api } from '../api/client'
import { LoadingSpinner } from '../components/LoadingSpinner'
import type { BattleResult } from '../types'

export function BattleResultPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { currentCharacter, refreshCharacter } = useGame()
  const [result, setResult] = useState<BattleResult | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!id) return
    api.finalizeBattle(id)
      .then(r => setResult(r))
      .catch(err => setError(err instanceof Error ? err.message : 'Failed to finalize'))
  }, [id])

  if (error) {
    return (
      <div className="page text-center">
        <p style={{ color: 'var(--color-danger)' }}>{error}</p>
        <button className="btn-secondary mt-4" onClick={() => navigate('/menu')}>Back to Menu</button>
      </div>
    )
  }

  if (!result) return <div className="page"><LoadingSpinner message="Finalizing battle..." /></div>

  const characterId = currentCharacter?.id

  const handleContinue = async () => {
    if (characterId) await refreshCharacter(characterId)
    // Retirement takes priority over level up
    if (result.characterRetired && characterId) {
      navigate(`/character/${characterId}/retired`, { state: { result } })
    } else if (result.levelsGained > 0 && characterId) {
      navigate(`/battle/${id}/levelup`, { state: { result } })
    } else if (characterId) {
      navigate(`/character/${characterId}`)
    } else {
      navigate('/menu')
    }
  }

  return (
    <div className="page" style={{ maxWidth: 'min(500px, 100%)' }}>
      <div style={{ textAlign: 'center', marginBottom: 32, marginTop: 20 }}>
        {/* UBER battle results */}
        {result.wasUberBattle ? (
          <h1 style={{
            fontSize: 'clamp(1.8rem, 5vw, 2.5rem)',
            fontWeight: 800,
            color: result.playerWon ? '#22c55e' : '#ef4444',
          }}>
            {result.playerWon ? 'UBER DEFEATED!' : 'UBER BOSS WINS'}
          </h1>
        ) : (
          <h1 style={{
            fontSize: 'clamp(2rem, 5vw, 2.5rem)',
            fontWeight: 800,
            color: result.playerWon ? '#22c55e' : '#ef4444',
          }}>
            {result.playerWon ? 'VICTORY!' : 'DEFEAT'}
          </h1>
        )}
        {result.wasUberBattle && !result.playerWon && (
          <p className="text-muted" style={{ marginTop: 8 }}>Try Again!</p>
        )}
      </div>

      {/* Retirement celebration */}
      {result.characterRetired && (
        <div className="panel" style={{
          background: 'rgba(168, 85, 247, 0.1)',
          borderColor: '#a855f7',
          textAlign: 'center',
          marginBottom: 16
        }}>
          <div style={{ fontSize: '3rem', marginBottom: 8 }}>&#127942;</div>
          <h2 style={{ color: '#a855f7', margin: '0 0 8px' }}>RETIRED!</h2>
          <p style={{ margin: 0 }}>Your character has reached the pinnacle and retires as a champion!</p>
        </div>
      )}

      {/* Only show rewards for non-UBER battles */}
      {!result.wasUberBattle && (
        <div className="panel">
          <div style={{ display: 'grid', gridTemplateColumns: 'auto 1fr', gap: '8px clamp(12px, 3vw, 24px)', fontSize: '0.95rem' }}>
            <span className="text-muted">XP Gained</span>
            <span style={{ color: '#06b6d4', fontWeight: 600 }}>+{result.xpGained}</span>

            <span className="text-muted">MMR Change</span>
            <span style={{ color: result.mmrChange >= 0 ? '#22c55e' : '#ef4444', fontWeight: 600 }}>
              {result.mmrChange >= 0 ? `+${result.mmrChange}` : result.mmrChange}
            </span>

            {result.levelsGained > 0 && !result.characterRetired && (
              <>
                <span className="text-muted">Level Up!</span>
                <span style={{ color: '#eab308', fontWeight: 700 }}>Now Level {result.newLevel}!</span>
              </>
            )}
          </div>
        </div>
      )}

      {/* UBER battle - no rewards info */}
      {result.wasUberBattle && (
        <div className="panel" style={{ textAlign: 'center' }}>
          <p className="text-muted" style={{ margin: 0 }}>
            UBER battles are for glory only - no XP or MMR rewards.
          </p>
        </div>
      )}

      {/* Last log entries */}
      {result.battleLog.length > 0 && (
        <div className="panel" style={{ maxHeight: 200, overflowY: 'auto' }}>
          <div style={{ fontSize: '0.85rem', color: 'var(--color-text-secondary)', marginBottom: 8 }}>Battle Summary</div>
          {result.battleLog.slice(-10).map((entry, i) => (
            <div key={i} style={{ fontSize: '0.85rem', color: 'var(--color-text-secondary)' }}>{entry}</div>
          ))}
        </div>
      )}

      <button className="btn-primary mt-4" style={{ width: '100%', padding: 12 }} onClick={handleContinue}>
        {result.characterRetired ? 'View Career' : result.levelsGained > 0 ? 'Level Up!' : 'Continue'}
      </button>
    </div>
  )
}

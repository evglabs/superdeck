import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useGame } from '../context/GameContext'
import { api } from '../api/client'
import { LoadingSpinner } from '../components/LoadingSpinner'
import type { Character } from '../types'

export function CharacterSelection() {
  const navigate = useNavigate()
  const { playerId } = useAuth()
  const { setCurrentCharacter } = useGame()
  const [characters, setCharacters] = useState<Character[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    api.getCharacters(playerId ?? undefined)
      .then(setCharacters)
      .catch(() => setCharacters([]))
      .finally(() => setLoading(false))
  }, [playerId])

  const handleSelect = (c: Character) => {
    setCurrentCharacter(c)
    navigate(`/character/${c.id}`)
  }

  if (loading) return <div className="page"><LoadingSpinner message="Loading characters..." /></div>

  return (
    <div className="page">
      <h1 className="page-title">Select Character</h1>

      {characters.length === 0 ? (
        <div className="panel text-center">
          <p className="text-muted mb-4">No characters found. Create one first.</p>
          <button className="btn-primary" onClick={() => navigate('/characters/new')}>New Character</button>
        </div>
      ) : (
        <div className="flex flex-col gap-2">
          {characters.map(c => (
            <div
              key={c.id}
              className="panel"
              style={{ cursor: 'pointer', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
              onClick={() => handleSelect(c)}
              onMouseEnter={e => (e.currentTarget.style.borderColor = 'var(--color-accent)')}
              onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--color-border)')}
            >
              <div>
                <div style={{ fontWeight: 700, fontSize: '1.1rem' }}>
                  {c.name}
                  {c.isRetired && (
                    <span style={{
                      marginLeft: 8,
                      fontSize: '0.75rem',
                      color: '#a855f7',
                      background: 'rgba(168, 85, 247, 0.2)',
                      padding: '2px 8px',
                      borderRadius: 4,
                      verticalAlign: 'middle'
                    }}>
                      RETIRED
                    </span>
                  )}
                </div>
                <div className="text-muted" style={{ fontSize: '0.9rem' }}>
                  Level {c.level} &middot; MMR {c.mmr} &middot; {c.wins}W / {c.losses}L
                </div>
              </div>
              <span style={{ color: 'var(--color-text-secondary)', fontSize: '1.2rem' }}>&rarr;</span>
            </div>
          ))}
        </div>
      )}

      <button className="btn-secondary mt-4" onClick={() => navigate('/menu')}>Back</button>
    </div>
  )
}

import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useGame } from '../context/GameContext'
import { api } from '../api/client'
import { SuitSelector } from '../components/SuitSelector'
import { CardDisplay } from '../components/CardDisplay'
import { LoadingSpinner } from '../components/LoadingSpinner'
import type { Suit, Card } from '../types'

export function CharacterCreation() {
  const navigate = useNavigate()
  const { playerId } = useAuth()
  const { setCurrentCharacter } = useGame()
  const [step, setStep] = useState<'name' | 'suit' | 'cards' | 'creating'>('name')
  const [name, setName] = useState('')
  const [suit, setSuit] = useState<Suit | null>(null)
  const [starterCards, setStarterCards] = useState<Card[]>([])
  const [selectedCardIds, setSelectedCardIds] = useState<Set<string>>(new Set())
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const handleNameSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (name.trim()) setStep('suit')
  }

  const handleSuitSelect = async (s: Suit) => {
    setSuit(s)
    setLoading(true)
    try {
      const cards = await api.getStarterPack(s)
      setStarterCards(cards)
      setSelectedCardIds(new Set())
      setStep('cards')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load starter pack')
    } finally {
      setLoading(false)
    }
  }

  const toggleCard = (cardId: string) => {
    setSelectedCardIds(prev => {
      const next = new Set(prev)
      if (next.has(cardId)) {
        next.delete(cardId)
      } else if (next.size < 3) {
        next.add(cardId)
      }
      return next
    })
  }

  const handleCreate = async () => {
    if (!suit) return
    setStep('creating')
    setError(null)
    try {
      const character = await api.createCharacter(name.trim(), suit, playerId ?? undefined)

      if (selectedCardIds.size > 0) {
        const updated = await api.addCards(character.id, Array.from(selectedCardIds))
        setCurrentCharacter(updated)
        navigate(`/character/${updated.id}`)
      } else {
        setCurrentCharacter(character)
        navigate(`/character/${character.id}`)
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create character')
      setStep('cards')
    }
  }

  if (step === 'creating') {
    return <div className="page"><LoadingSpinner message="Creating character..." /></div>
  }

  return (
    <div className="page">
      <h1 className="page-title">Create New Character</h1>

      {error && <div className="panel" style={{ borderColor: 'var(--color-danger)', color: 'var(--color-danger)', marginBottom: 16 }}>{error}</div>}

      {step === 'name' && (
        <form onSubmit={handleNameSubmit} className="panel flex flex-col gap-3" style={{ maxWidth: 400 }}>
          <label style={{ fontWeight: 600 }}>Character Name</label>
          <input
            value={name}
            onChange={e => setName(e.target.value)}
            placeholder="Enter a name"
            autoFocus
            required
          />
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn-secondary" type="button" onClick={() => navigate('/menu')}>Back</button>
            <button className="btn-primary" type="submit" disabled={!name.trim()}>Next</button>
          </div>
        </form>
      )}

      {step === 'suit' && (
        <div className="panel">
          <label style={{ fontWeight: 600, display: 'block', marginBottom: 12 }}>Choose your starting suit</label>
          {loading ? <LoadingSpinner /> : <SuitSelector value={suit} onChange={handleSuitSelect} />}
          <button className="btn-secondary mt-4" onClick={() => setStep('name')}>Back</button>
        </div>
      )}

      {step === 'cards' && (
        <div className="panel">
          <div style={{ marginBottom: 12 }}>
            <span style={{ fontWeight: 600 }}>Select up to 3 starter cards</span>
            <span className="text-muted" style={{ marginLeft: 8 }}>({selectedCardIds.size}/3 selected)</span>
          </div>

          {starterCards.length === 0 ? (
            <p className="text-muted">No starter cards available for {suit}.</p>
          ) : (
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, marginBottom: 16 }}>
              {starterCards.map(card => (
                <CardDisplay
                  key={card.id}
                  card={card}
                  selected={selectedCardIds.has(card.id)}
                  onClick={() => toggleCard(card.id)}
                />
              ))}
            </div>
          )}

          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn-secondary" onClick={() => setStep('suit')}>Back</button>
            <button className="btn-primary" onClick={handleCreate}>
              {selectedCardIds.size > 0 ? `Create with ${selectedCardIds.size} card(s)` : 'Create (no extra cards)'}
            </button>
          </div>
        </div>
      )}
    </div>
  )
}

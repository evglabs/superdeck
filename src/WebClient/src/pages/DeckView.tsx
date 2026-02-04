import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useGame } from '../context/GameContext'
import { api } from '../api/client'
import { LoadingSpinner } from '../components/LoadingSpinner'
import { CardTable } from '../components/CardTable'
import { ConfirmDialog } from '../components/ConfirmDialog'
import type { Card } from '../types'

export function DeckView() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { currentCharacter, refreshCharacter } = useGame()
  const [deckCards, setDeckCards] = useState<{ id: string; card: Card }[]>([])
  const [loading, setLoading] = useState(true)
  const [removeTarget, setRemoveTarget] = useState<{ id: string; card: Card } | null>(null)

  const character = currentCharacter?.id === id ? currentCharacter : null

  const loadDeck = async () => {
    if (!character) {
      if (id) await refreshCharacter(id)
      return
    }
    setLoading(true)
    try {
      const allCards = await api.getCards()
      const cardMap = new Map(allCards.map(c => [c.id, c]))
      const items = character.deckCardIds
        .map(cardId => {
          const card = cardMap.get(cardId)
          return card ? { id: cardId, card } : null
        })
        .filter((x): x is { id: string; card: Card } => x !== null)
      setDeckCards(items)
    } catch {
      setDeckCards([])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { loadDeck() }, [character?.deckCardIds.length])

  const handleRemove = async () => {
    if (!removeTarget || !id) return
    try {
      await api.removeCards(id, [removeTarget.id])
      await refreshCharacter(id)
    } catch {
      // silently fail
    }
    setRemoveTarget(null)
  }

  if (loading) return <div className="page"><LoadingSpinner message="Loading deck..." /></div>

  const minDeck = 6

  return (
    <div className="page">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <h1 className="page-title" style={{ marginBottom: 0 }}>Deck ({deckCards.length} cards)</h1>
        <button className="btn-secondary" onClick={() => navigate(`/character/${id}`)}>Back</button>
      </div>

      {deckCards.length === 0 ? (
        <p className="text-muted">No cards in deck.</p>
      ) : (
        <div className="panel" style={{ padding: 0 }}>
          <CardTable
            cards={deckCards}
            onRemove={(cardId, card) => setRemoveTarget({ id: cardId, card })}
            removeDisabled={deckCards.length <= minDeck}
          />
        </div>
      )}

      {deckCards.length <= minDeck && (
        <p className="text-muted mt-2" style={{ fontSize: '0.85rem' }}>
          Minimum deck size is {minDeck}. Cannot remove more cards.
        </p>
      )}

      {removeTarget && (
        <ConfirmDialog
          title="Remove Card"
          message={`Remove ${removeTarget.card.name} from your deck?`}
          confirmLabel="Remove"
          danger
          onConfirm={handleRemove}
          onCancel={() => setRemoveTarget(null)}
        />
      )}
    </div>
  )
}

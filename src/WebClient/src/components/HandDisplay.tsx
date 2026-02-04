import type { Card } from '../types'
import { CardDisplay } from './CardDisplay'

interface HandDisplayProps {
  cards: Card[]
  onCardClick: (index: number) => void
  onCardDetail?: (card: Card) => void
  disabled?: boolean
}

export function HandDisplay({ cards, onCardClick, onCardDetail, disabled }: HandDisplayProps) {
  if (cards.length === 0) {
    return <div className="text-muted" style={{ padding: '12px 0' }}>No cards in hand</div>
  }

  return (
    <div style={{ display: 'flex', gap: 10, overflowX: 'auto', padding: '4px 0' }}>
      {cards.map((card, i) => (
        <div key={i} style={{ position: 'relative', flexShrink: 0 }}>
          <CardDisplay
            card={card}
            onClick={disabled ? undefined : () => onCardClick(i)}
          />
          {onCardDetail && (
            <button
              style={{
                position: 'absolute', top: 4, right: 4,
                background: 'rgba(0,0,0,0.6)', color: 'var(--color-text-secondary)',
                border: 'none', borderRadius: '50%', width: 20, height: 20,
                fontSize: '0.7rem', cursor: 'pointer', lineHeight: 1, padding: 0,
              }}
              onClick={e => { e.stopPropagation(); onCardDetail(card) }}
            >
              ?
            </button>
          )}
        </div>
      ))}
    </div>
  )
}

import { useState } from 'react'
import type { Card } from '../types'
import { typeColors, rarityColors } from '../styles/colors'
import { CardDetailModal } from './CardDetailModal'

interface CardTableProps {
  cards: { id: string; card: Card }[]
  onRemove?: (id: string, card: Card) => void
  removeDisabled?: boolean
}

type SortKey = 'name' | 'type' | 'suit' | 'rarity'

const rarityOrder = { Common: 1, Uncommon: 2, Rare: 3, Epic: 4, Legendary: 5 }

export function CardTable({ cards, onRemove, removeDisabled }: CardTableProps) {
  const [sortBy, setSortBy] = useState<SortKey>('name')
  const [detailCard, setDetailCard] = useState<Card | null>(null)

  const sorted = [...cards].sort((a, b) => {
    switch (sortBy) {
      case 'name': return a.card.name.localeCompare(b.card.name)
      case 'type': return a.card.type.localeCompare(b.card.type)
      case 'suit': return a.card.suit.localeCompare(b.card.suit)
      case 'rarity': return rarityOrder[a.card.rarity] - rarityOrder[b.card.rarity]
      default: return 0
    }
  })

  const thStyle = (key: SortKey): React.CSSProperties => ({
    padding: '8px 12px',
    textAlign: 'left',
    cursor: 'pointer',
    color: sortBy === key ? 'var(--color-accent)' : 'var(--color-text-secondary)',
    fontSize: '0.85rem',
    fontWeight: 600,
    borderBottom: '1px solid var(--color-border)',
    userSelect: 'none',
  })

  const tdStyle: React.CSSProperties = {
    padding: '8px 12px',
    borderBottom: '1px solid var(--color-border)',
    fontSize: '0.9rem',
  }

  return (
    <>
      <div style={{ overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th style={thStyle('name')} onClick={() => setSortBy('name')}>Card</th>
              <th style={thStyle('type')} onClick={() => setSortBy('type')}>Type</th>
              <th style={thStyle('suit')} onClick={() => setSortBy('suit')}>Suit</th>
              <th style={thStyle('rarity')} onClick={() => setSortBy('rarity')}>Rarity</th>
              <th style={{ ...tdStyle, fontSize: '0.85rem', fontWeight: 600, color: 'var(--color-text-secondary)' }}>Description</th>
              {onRemove && <th style={{ ...tdStyle, width: 40 }} />}
            </tr>
          </thead>
          <tbody>
            {sorted.map(({ id, card }, i) => (
              <tr
                key={`${id}-${i}`}
                style={{ cursor: 'pointer' }}
                onClick={() => setDetailCard(card)}
                onMouseEnter={e => (e.currentTarget.style.background = 'var(--color-bg-card)')}
                onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
              >
                <td style={{ ...tdStyle, fontWeight: 600, color: rarityColors[card.rarity] }}>{card.name}</td>
                <td style={{ ...tdStyle, color: typeColors[card.type] }}>{card.type}</td>
                <td style={tdStyle}>{card.suit}</td>
                <td style={{ ...tdStyle, color: rarityColors[card.rarity] }}>{card.rarity}</td>
                <td style={{ ...tdStyle, color: 'var(--color-text-secondary)', maxWidth: 200 }}>
                  {card.description && card.description.length > 50
                    ? card.description.slice(0, 47) + '...'
                    : card.description}
                </td>
                {onRemove && (
                  <td style={tdStyle}>
                    <button
                      className="btn-danger"
                      style={{ padding: '2px 8px', fontSize: '0.8rem' }}
                      disabled={removeDisabled}
                      onClick={e => { e.stopPropagation(); onRemove(id, card) }}
                    >
                      X
                    </button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {detailCard && <CardDetailModal card={detailCard} onClose={() => setDetailCard(null)} />}
    </>
  )
}

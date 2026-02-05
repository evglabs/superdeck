import type { Card } from '../types'
import { typeColors, rarityColors } from '../styles/colors'

interface CardDisplayProps {
  card: Card
  selected?: boolean
  onClick?: () => void
  compact?: boolean
  responsive?: boolean
}

export function CardDisplay({ card, selected, onClick, compact, responsive }: CardDisplayProps) {
  const borderColor = typeColors[card.type]
  const nameColor = rarityColors[card.rarity]

  if (compact) {
    return (
      <span
        onClick={onClick}
        style={{
          display: 'inline-block',
          background: 'var(--color-bg-card)',
          borderLeft: `3px solid ${borderColor}`,
          borderRadius: 4,
          padding: '4px 8px',
          fontSize: '0.85rem',
          cursor: onClick ? 'pointer' : 'default',
          color: nameColor,
          fontWeight: 600,
        }}
      >
        {card.name}
      </span>
    )
  }

  return (
    <div
      onClick={onClick}
      style={{
        background: 'var(--color-bg-card)',
        borderLeft: `4px solid ${borderColor}`,
        borderRadius: 8,
        padding: '12px 14px',
        cursor: onClick ? 'pointer' : 'default',
        border: selected ? '2px solid var(--color-accent)' : '1px solid var(--color-border)',
        boxShadow: selected ? '0 0 12px rgba(59,130,246,0.3)' : 'none',
        transition: 'transform 0.15s, box-shadow 0.15s',
        width: responsive ? 'var(--card-width)' : 180,
        flexShrink: 0,
      }}
      onMouseEnter={e => { if (onClick) e.currentTarget.style.transform = 'translateY(-4px)' }}
      onMouseLeave={e => { e.currentTarget.style.transform = 'translateY(0)' }}
    >
      <div style={{ fontWeight: 700, color: nameColor, marginBottom: 4, fontSize: '0.95rem' }}>
        {card.name}
      </div>
      <div style={{ fontSize: '0.8rem', color: 'var(--color-text-secondary)', marginBottom: 4 }}>
        {card.type} &middot; {card.suit}
      </div>
      <div style={{ fontSize: '0.8rem', color: 'var(--color-text-secondary)' }}>
        {card.description && card.description.length > 60
          ? card.description.slice(0, 57) + '...'
          : card.description}
      </div>
    </div>
  )
}

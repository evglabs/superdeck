import { SELECTABLE_SUITS, type Suit } from '../types'

interface SuitSelectorProps {
  value: Suit | null
  onChange: (suit: Suit) => void
}

export function SuitSelector({ value, onChange }: SuitSelectorProps) {
  return (
    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
      {SELECTABLE_SUITS.map(suit => (
        <button
          key={suit}
          type="button"
          onClick={() => onChange(suit)}
          style={{
            padding: '8px 14px',
            borderRadius: 6,
            border: suit === value ? '2px solid var(--color-accent)' : '1px solid var(--color-border)',
            background: suit === value ? 'var(--color-bg-card)' : 'var(--color-bg-secondary)',
            color: suit === value ? 'var(--color-text-primary)' : 'var(--color-text-secondary)',
            fontWeight: suit === value ? 600 : 400,
            fontSize: '0.9rem',
            boxShadow: suit === value ? '0 0 8px rgba(59,130,246,0.3)' : 'none',
          }}
        >
          {suit}
        </button>
      ))}
    </div>
  )
}

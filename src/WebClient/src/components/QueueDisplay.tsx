import type { Card } from '../types'
import { CardDisplay } from './CardDisplay'

interface QueueDisplayProps {
  queue: Card[]
  maxSlots: number
}

export function QueueDisplay({ queue, maxSlots }: QueueDisplayProps) {
  const slots: (Card | null)[] = []
  for (let i = 0; i < maxSlots; i++) {
    slots.push(i < queue.length ? queue[i] : null)
  }

  return (
    <div>
      <div style={{ fontSize: '0.85rem', color: 'var(--color-text-secondary)', marginBottom: 6 }}>
        Queue: {queue.length}/{maxSlots}
      </div>
      <div className="queue-container">
        {slots.map((card, i) => (
          <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
            {i > 0 && <span style={{ color: 'var(--color-text-secondary)', fontSize: '0.9rem' }}>&rarr;</span>}
            {card ? (
              <CardDisplay card={card} compact />
            ) : (
              <span style={{
                display: 'inline-block',
                background: 'var(--color-bg-primary)',
                border: '1px dashed var(--color-border)',
                borderRadius: 4,
                padding: '4px 12px',
                fontSize: '0.85rem',
                color: 'var(--color-text-secondary)',
              }}>
                empty
              </span>
            )}
          </div>
        ))}
      </div>
    </div>
  )
}

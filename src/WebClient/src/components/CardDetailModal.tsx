import type { Card } from '../types'
import { typeColors, rarityColors } from '../styles/colors'

interface CardDetailModalProps {
  card: Card
  onClose: () => void
}

export function CardDetailModal({ card, onClose }: CardDetailModalProps) {
  return (
    <div
      style={{
        position: 'fixed', inset: 0, background: 'var(--color-overlay-bg)',
        display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100,
      }}
      onClick={onClose}
    >
      <div
        className="panel"
        style={{ maxWidth: 420, width: '90%', borderLeft: `4px solid ${typeColors[card.type]}` }}
        onClick={e => e.stopPropagation()}
      >
        <h2 style={{ color: rarityColors[card.rarity], marginBottom: 16 }}>{card.name}</h2>

        <div style={{ display: 'grid', gridTemplateColumns: 'auto 1fr', gap: '8px 16px', fontSize: '0.95rem' }}>
          <span className="text-muted">Type</span>
          <span style={{ color: typeColors[card.type] }}>{card.type}</span>

          <span className="text-muted">Suit</span>
          <span>{card.suit}</span>

          <span className="text-muted">Rarity</span>
          <span style={{ color: rarityColors[card.rarity] }}>{card.rarity}</span>

          <span className="text-muted">Description</span>
          <span>{card.description || 'No description'}</span>

          {card.immediateEffect && (
            <>
              <span className="text-muted">Target</span>
              <span>{card.immediateEffect.target}</span>
            </>
          )}

          {card.grantsStatusTo && (
            <>
              <span className="text-muted">Grants Status</span>
              <span>
                {card.grantsStatusTo.status.name} ({card.grantsStatusTo.status.duration} turns)
                {' '}to {card.grantsStatusTo.target}
              </span>
            </>
          )}

          {card.targetSelf && (
            <>
              <span className="text-muted">Self-target</span>
              <span>Yes</span>
            </>
          )}
        </div>

        <button className="btn-secondary" style={{ marginTop: 20, width: '100%' }} onClick={onClose}>
          Close
        </button>
      </div>
    </div>
  )
}

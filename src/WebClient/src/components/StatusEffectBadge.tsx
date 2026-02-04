import type { StatusEffect } from '../types'

interface StatusEffectBadgeProps {
  effect: StatusEffect
}

export function StatusEffectBadge({ effect }: StatusEffectBadgeProps) {
  const bg = effect.isBuff ? 'rgba(34,197,94,0.15)' : 'rgba(249,115,22,0.15)'
  const border = effect.isBuff ? '#22c55e' : '#f97316'
  const color = effect.isBuff ? '#22c55e' : '#f97316'

  return (
    <span style={{
      display: 'inline-flex',
      alignItems: 'center',
      gap: 4,
      background: bg,
      border: `1px solid ${border}`,
      borderRadius: 12,
      padding: '2px 10px',
      fontSize: '0.8rem',
      color,
      fontWeight: 600,
    }}>
      {effect.name}
      <span style={{ opacity: 0.7 }}>({effect.remainingDuration})</span>
    </span>
  )
}

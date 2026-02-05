import type { StatusEffect } from '../types'

interface StatusEffectBadgeProps {
  effect: StatusEffect
}

export function StatusEffectBadge({ effect }: StatusEffectBadgeProps) {
  const badgeClass = effect.isBuff ? 'status-badge--buff' : 'status-badge--debuff'

  return (
    <span className={`status-badge ${badgeClass}`} title={effect.name}>
      <span className="status-badge-name">{effect.name}</span>
      <span className="status-badge-duration">({effect.remainingDuration})</span>
    </span>
  )
}

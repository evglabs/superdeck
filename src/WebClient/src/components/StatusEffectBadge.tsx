import type { StatusEffect } from '../types'

interface StatusEffectBadgeProps {
  effect: StatusEffect
  count?: number
}

export function StatusEffectBadge({ effect, count = 1 }: StatusEffectBadgeProps) {
  const badgeClass = effect.isBuff ? 'status-badge--buff' : 'status-badge--debuff'
  const title = count > 1 ? `${effect.name} x${count}` : effect.name

  return (
    <span className={`status-badge ${badgeClass}`} title={title}>
      <span className="status-badge-name">{effect.name}</span>
      <span className="status-badge-duration">({effect.remainingDuration}){count > 1 && ` x${count}`}</span>
    </span>
  )
}

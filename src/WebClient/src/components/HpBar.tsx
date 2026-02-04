import { hpColor } from '../styles/colors'

interface HpBarProps {
  current: number
  max: number
  label?: string
  tint?: string
}

export function HpBar({ current, max, label, tint }: HpBarProps) {
  const percent = Math.max(0, Math.min(100, (current / max) * 100))
  const color = hpColor(percent)

  return (
    <div>
      {label && (
        <div style={{ fontSize: '0.85rem', marginBottom: 4, display: 'flex', justifyContent: 'space-between' }}>
          <span style={{ fontWeight: 600, color: tint }}>{label}</span>
          <span style={{ color }}>{current}/{max}</span>
        </div>
      )}
      <div style={{
        height: 16, borderRadius: 8, background: 'var(--color-bg-primary)',
        border: '1px solid var(--color-border)', overflow: 'hidden',
      }}>
        <div style={{
          height: '100%',
          width: `${percent}%`,
          background: color,
          borderRadius: 8,
          transition: 'width 0.3s ease, background-color 0.3s ease',
        }} />
      </div>
    </div>
  )
}

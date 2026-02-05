interface StatStepperProps {
  label: string
  value: number
  displayValue?: string
  color: string
  onIncrement: () => void
  onDecrement: () => void
  canIncrement: boolean
  canDecrement: boolean
}

export function StatStepper({ label, value, displayValue, color, onIncrement, onDecrement, canIncrement, canDecrement }: StatStepperProps) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', justifyContent: 'space-between',
      background: 'var(--color-bg-card)', borderRadius: 8, padding: '10px 14px',
    }}>
      <span style={{ fontSize: '0.9rem', color: 'var(--color-text-secondary)', flex: 1 }}>{label}</span>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
        <button
          className="btn-secondary"
          style={{ width: 44, height: 44, padding: 0, fontSize: '1.1rem', lineHeight: 1 }}
          onClick={onDecrement}
          disabled={!canDecrement}
        >
          -
        </button>
        <span style={{ fontWeight: 700, color, minWidth: 32, textAlign: 'center', fontSize: '1.1rem' }}>
          {displayValue ?? value}
        </span>
        <button
          className="btn-secondary"
          style={{ width: 44, height: 44, padding: 0, fontSize: '1.1rem', lineHeight: 1 }}
          onClick={onIncrement}
          disabled={!canIncrement}
        >
          +
        </button>
      </div>
    </div>
  )
}

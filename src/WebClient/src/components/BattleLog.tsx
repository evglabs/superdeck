import { useEffect, useRef } from 'react'
import { logEntryColor } from '../styles/colors'

interface BattleLogProps {
  entries: string[]
  newStartIndex: number
}

export function BattleLog({ entries, newStartIndex }: BattleLogProps) {
  const endRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    endRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [entries.length])

  return (
    <div style={{
      background: 'var(--color-bg-primary)',
      border: '1px solid var(--color-border)',
      borderRadius: 6,
      padding: '8px 12px',
      maxHeight: 240,
      overflowY: 'auto',
      fontSize: '0.85rem',
      lineHeight: 1.6,
    }}>
      {entries.length === 0 && <span className="text-muted">No battle log entries yet.</span>}
      {entries.map((entry, i) => {
        const isNew = i >= newStartIndex
        return (
          <div
            key={i}
            style={{
              color: logEntryColor(entry),
              fontWeight: isNew ? 600 : 400,
              opacity: isNew ? 1 : 0.7,
            }}
          >
            {entry}
          </div>
        )
      })}
      <div ref={endRef} />
    </div>
  )
}

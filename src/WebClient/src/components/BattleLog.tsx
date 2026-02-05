import { useState, useEffect, useRef } from 'react'
import { logEntryColor } from '../styles/colors'
import { useIsMobile } from '../hooks/useMediaQuery'

interface BattleLogProps {
  entries: string[]
  newStartIndex: number
  collapsible?: boolean
}

export function BattleLog({ entries, newStartIndex, collapsible }: BattleLogProps) {
  const endRef = useRef<HTMLDivElement>(null)
  const isMobile = useIsMobile()
  const [collapsed, setCollapsed] = useState(false)

  const shouldCollapse = collapsible && isMobile

  useEffect(() => {
    if (!collapsed) {
      endRef.current?.scrollIntoView({ behavior: 'smooth' })
    }
  }, [entries.length, collapsed])

  return (
    <div>
      {shouldCollapse && (
        <button
          className="battle-log-toggle"
          style={{ display: 'block' }}
          onClick={() => setCollapsed(c => !c)}
        >
          Battle Log ({entries.length}) {collapsed ? '[ Show ]' : '[ Hide ]'}
        </button>
      )}
      <div
        className={`battle-log-content${shouldCollapse && collapsed ? ' collapsed' : ''}`}
        style={{
          background: 'var(--color-bg-primary)',
          border: shouldCollapse && collapsed ? 'none' : '1px solid var(--color-border)',
          borderRadius: 6,
          padding: shouldCollapse && collapsed ? 0 : '8px 12px',
          maxHeight: shouldCollapse && collapsed ? 0 : 240,
          overflowY: 'auto',
          overflow: shouldCollapse && collapsed ? 'hidden' : undefined,
          fontSize: '0.85rem',
          lineHeight: 1.6,
        }}
      >
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
    </div>
  )
}

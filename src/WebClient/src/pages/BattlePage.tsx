import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useBattle } from '../hooks/useBattle'
import { useIsMobile } from '../hooks/useMediaQuery'
import { LoadingSpinner } from '../components/LoadingSpinner'
import { HpBar } from '../components/HpBar'
import { StatusEffectBadge } from '../components/StatusEffectBadge'
import type { StatusEffect } from '../types'
import { BattleLog } from '../components/BattleLog'
import { HandDisplay } from '../components/HandDisplay'
import { QueueDisplay } from '../components/QueueDisplay'
import { CardDetailModal } from '../components/CardDetailModal'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { CardDisplay } from '../components/CardDisplay'

function StatValue({ label, base, effective, color }: { label: string; base: number; effective?: number; color: string }) {
  const value = effective ?? base
  const diff = value - base
  const hasBuff = diff > 0
  const hasDebuff = diff < 0

  return (
    <span style={{ color }}>
      {label} {value}
      {hasBuff && <span style={{ color: '#22c55e', fontSize: '0.65rem' }}> +{diff}</span>}
      {hasDebuff && <span style={{ color: '#ef4444', fontSize: '0.65rem' }}> {diff}</span>}
    </span>
  )
}

function SpeedControls({ speedMultiplier, setSpeedMultiplier }: { speedMultiplier: number; setSpeedMultiplier: (speed: number) => void }) {
  return (
    <div className="speed-controls">
      <span className="speed-label">Speed:</span>
      {[0.5, 1, 2, 4].map(speed => (
        <button
          key={speed}
          className={`speed-btn ${speedMultiplier === speed ? 'btn-primary' : 'btn-secondary'}`}
          onClick={() => setSpeedMultiplier(speed)}
        >
          {speed}x
        </button>
      ))}
    </div>
  )
}

interface GroupedStatus {
  key: string
  effect: StatusEffect
  count: number
  ids: string[]
}

function groupStatuses(statuses: StatusEffect[]): GroupedStatus[] {
  const groups = new Map<string, GroupedStatus>()
  for (const status of statuses) {
    const key = `${status.name}-${status.remainingDuration}`
    const existing = groups.get(key)
    if (existing) {
      existing.count++
      existing.ids.push(status.id)
    } else {
      groups.set(key, { key, effect: status, count: 1, ids: [status.id] })
    }
  }
  return Array.from(groups.values())
}

function renderStatusBadges(statuses: StatusEffect[], max: number) {
  const grouped = groupStatuses(statuses)
  const visible = grouped.slice(0, max)
  const overflow = grouped.length - max
  // Use sorted IDs in key to ensure stable identity when same statuses exist
  return (
    <>
      {visible.map(g => (
        <StatusEffectBadge
          key={`${g.key}-${g.ids.sort().join(',')}`}
          effect={g.effect}
          count={g.count}
        />
      ))}
      {overflow > 0 && (
        <span className="status-badge-overflow">+{overflow} more</span>
      )}
    </>
  )
}

export function BattlePage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const battle = useBattle(id!)
  const [showForfeit, setShowForfeit] = useState(false)
  const isMobile = useIsMobile()

  const { state, loading, error, autoBattle, animation, speedMultiplier, setSpeedMultiplier } = battle

  // Use animated values when animation is playing back events
  const isAnimating = animation.isPlaying && !animation.isComplete
  const displayPlayerHP = isAnimating ? animation.displayedPlayerHP : (state?.player.currentHP ?? 0)
  const displayOpponentHP = isAnimating ? animation.displayedOpponentHP : (state?.opponent.currentHP ?? 0)

  // Use animated statuses during playback, final statuses otherwise
  const displayPlayerStatuses = isAnimating || animation.isPaused
    ? animation.displayedPlayerStatuses
    : (state?.playerStatuses ?? [])
  const displayOpponentStatuses = isAnimating || animation.isPaused
    ? animation.displayedOpponentStatuses
    : (state?.opponentStatuses ?? [])

  // Filter battle log to show only entries up to current animation point
  const visibleLogEntries = state?.battleLog.slice(0, animation.visibleLogLength) ?? []

  if (!state) {
    return <div className="page"><LoadingSpinner message="Loading battle..." /></div>
  }

  // Battle is complete - show results button instead of auto-navigating
  const battleEnded = state.isComplete || state.phase === 'Ended'

  const isQueuePhase = state.phase === 'Queue'
  const canQueue = isQueuePhase && !autoBattle && state.playerQueue.length < state.currentPlayerQueueSlots

  return (
    <div className="battle-layout">
      {/* Main layout: content left, battle log right */}
      <div className="battle-main">

      {/* Battle Header */}
      <div className={`battle-header ${isMobile ? 'battle-header--mobile' : 'battle-header--desktop'}`}>
        <div className="battle-combatant" style={{ order: isMobile ? 1 : undefined }}>
          <HpBar current={displayPlayerHP} max={state.player.maxHP} label={state.player.name} tint="#60a5fa" />
          <div style={{ fontSize: '0.8rem', marginTop: 'var(--space-xs)', color: state.playerGoesFirst ? '#22c55e' : 'var(--color-text-secondary)' }}>
            {state.playerGoesFirst ? 'FIRST' : 'second'}
          </div>
          <div className={`battle-combatant-stats ${isMobile ? 'battle-combatant-stats--mobile' : ''}`}>
            <span style={{ color: 'var(--color-text-secondary)' }}>Lv.{state.player.level}</span>
            <StatValue label="ATK" base={state.player.attack} effective={state.playerEffectiveStats?.attack} color="#ef4444" />
            <StatValue label="DEF" base={state.player.defense} effective={state.playerEffectiveStats?.defense} color="#3b82f6" />
            <StatValue label="SPD" base={state.player.speed} effective={state.playerEffectiveStats?.speed} color="#eab308" />
          </div>
        </div>
        {!isMobile && (
          <div className="battle-phase-indicator">
            <div className="battle-phase-round">Round {state.round}</div>
            <div className="battle-phase-name">{state.phase}</div>
          </div>
        )}
        <div className="battle-combatant" style={{ order: isMobile ? 2 : undefined }}>
          <HpBar current={displayOpponentHP} max={state.opponent.maxHP} label={state.opponent.name} tint="#f87171" />
          <div style={{ fontSize: '0.8rem', marginTop: 'var(--space-xs)', textAlign: 'right', color: !state.playerGoesFirst ? '#22c55e' : 'var(--color-text-secondary)' }}>
            {!state.playerGoesFirst ? 'FIRST' : 'second'}
          </div>
          <div className={`battle-combatant-stats battle-combatant-stats--right ${isMobile ? 'battle-combatant-stats--mobile' : ''}`}>
            <span style={{ color: 'var(--color-text-secondary)' }}>Lv.{state.opponent.level}</span>
            <StatValue label="ATK" base={state.opponent.attack} effective={state.opponentEffectiveStats?.attack} color="#ef4444" />
            <StatValue label="DEF" base={state.opponent.defense} effective={state.opponentEffectiveStats?.defense} color="#3b82f6" />
            <StatValue label="SPD" base={state.opponent.speed} effective={state.opponentEffectiveStats?.speed} color="#eab308" />
          </div>
        </div>
        {isMobile && (
          <div className="battle-phase-mobile">
            <span className="battle-phase-round">Round {state.round}</span>
            <span className="battle-phase-name" style={{ marginLeft: 'var(--space-sm)' }}>{state.phase}</span>
          </div>
        )}
      </div>

      {/* Status Effects - always rendered with minHeight to prevent layout shift */}
      <div className={`battle-status-row ${isMobile ? 'battle-status-row--mobile' : ''}`}>
        <div className="battle-status-group">
          {renderStatusBadges(displayPlayerStatuses, isMobile ? 3 : 6)}
        </div>
        <div className="battle-status-group battle-status-group--right">
          {renderStatusBadges(displayOpponentStatuses, isMobile ? 3 : 6)}
        </div>
      </div>

      {/* Revealed Opponent Info */}
      {state.opponentQueueRevealed && state.opponentQueue.length > 0 && (
        <div className="panel" style={{ padding: '8px 12px', marginBottom: 8, borderColor: '#f97316' }}>
          <span style={{ fontSize: '0.85rem', color: '#f97316', fontWeight: 600 }}>Opponent Queue (revealed): </span>
          <span style={{ display: 'inline-flex', gap: 4, flexWrap: 'wrap' }}>
            {state.opponentQueue.map((c, i) => (
              <span key={i}>
                {i > 0 && <span style={{ color: 'var(--color-text-secondary)' }}> &rarr; </span>}
                <CardDisplay card={c} compact />
              </span>
            ))}
          </span>
        </div>
      )}
      {state.opponentHandRevealed && state.opponentHand.length > 0 && (
        <div className="panel" style={{ padding: '8px 12px', marginBottom: 8, borderColor: '#f97316' }}>
          <span style={{ fontSize: '0.85rem', color: '#f97316', fontWeight: 600 }}>Opponent Hand (revealed): </span>
          <span style={{ display: 'inline-flex', gap: 4, flexWrap: 'wrap' }}>
            {state.opponentHand.map((c, i) => <CardDisplay key={i} card={c} compact />)}
          </span>
        </div>
      )}

      {/* Queue Display */}
      {isQueuePhase && (
        <div style={{ marginBottom: 'var(--space-md)' }}>
          <QueueDisplay queue={state.playerQueue} maxSlots={state.currentPlayerQueueSlots} />
        </div>
      )}

      {/* Hand */}
      {isQueuePhase && (
        <div style={{ marginBottom: 'var(--space-md)' }}>
          <div style={{ fontSize: '0.85rem', color: 'var(--color-text-secondary)', marginBottom: 'var(--space-xs)', textAlign: 'center' }}>Your Hand</div>
          <HandDisplay
            cards={state.playerHand}
            onCardClick={i => { if (canQueue) battle.queueCard(i) }}
            onCardDetail={card => battle.setDetailCard(card)}
            disabled={!canQueue || loading}
          />
        </div>
      )}

      {/* Error */}
      {error && <div style={{ color: 'var(--color-danger)', fontSize: '0.9rem', marginBottom: 'var(--space-sm)' }}>{error}</div>}

      {/* Unified Controls Container */}
      <div className="battle-controls">
        {/* Queue phase buttons */}
        {isQueuePhase && !autoBattle && (
          <>
            <button className="btn-primary" onClick={battle.confirmQueue} disabled={loading}>
              Confirm Queue
            </button>
            <button className="btn-secondary" onClick={battle.toggleAutoBattle}>
              Auto-Battle
            </button>
            <button className="btn-danger" onClick={() => setShowForfeit(true)}>
              Forfeit
            </button>
          </>
        )}

        {/* Auto-battle indicator */}
        {autoBattle && (
          <>
            <span className="auto-battle-indicator">AUTO-BATTLE</span>
            <div className="spinner" />
            <button className="btn-secondary" onClick={battle.toggleAutoBattle}>
              Manual Mode
            </button>
          </>
        )}

        {/* Animation playback controls */}
        {isAnimating && (
          <>
            <span className="playback-status playback-status--playing">Playing events...</span>
            <button className="btn-secondary" onClick={animation.pause}>
              Pause
            </button>
            <button className="btn-secondary" onClick={animation.skipToEnd}>
              Skip
            </button>
            <SpeedControls speedMultiplier={speedMultiplier} setSpeedMultiplier={setSpeedMultiplier} />
          </>
        )}

        {/* Paused playback controls */}
        {animation.isPaused && !animation.isComplete && (
          <>
            <span className="playback-status playback-status--paused">Paused</span>
            <button className="btn-primary" onClick={animation.play}>
              Resume
            </button>
            <button className="btn-secondary" onClick={animation.skipToEnd}>
              Skip
            </button>
            <SpeedControls speedMultiplier={speedMultiplier} setSpeedMultiplier={setSpeedMultiplier} />
          </>
        )}

        {/* Resolving spinner */}
        {!isQueuePhase && !autoBattle && !battleEnded && !isAnimating && !animation.isPaused && (
          <>
            <span className="text-muted" style={{ fontSize: '0.9rem' }}>Resolving...</span>
            <div className="spinner" />
          </>
        )}

        {/* Battle complete - show results button */}
        {battleEnded && animation.isComplete && (
          <div className="battle-complete-panel">
            <div className={`battle-complete-title ${state.winnerId === state.player.id ? 'battle-complete-title--victory' : 'battle-complete-title--defeat'}`}>
              {state.winnerId === state.player.id ? 'Victory!' : 'Defeat'}
            </div>
            <button
              className="btn-primary"
              onClick={() => navigate(`/battle/${id}/result`, { replace: true })}
              style={{ padding: '8px 24px', fontSize: '1rem' }}
            >
              Continue to Results
            </button>
          </div>
        )}
      </div>

      </div>{/* End battle-main */}

      {/* Right column: Battle Log */}
      <div className="battle-sidebar">
        <div style={{ fontSize: '0.85rem', color: 'var(--color-text-secondary)', marginBottom: 'var(--space-xs)' }}>
          Battle Log {isAnimating && `(${visibleLogEntries.length}/${state.battleLog.length})`}
        </div>
        <BattleLog entries={visibleLogEntries} newStartIndex={battle.lastLogIndex} collapsible />
      </div>

      {/* Modals */}
      {battle.detailCard && (
        <CardDetailModal card={battle.detailCard} onClose={() => battle.setDetailCard(null)} />
      )}
      {showForfeit && (
        <ConfirmDialog
          title="Forfeit Battle"
          message="Are you sure you want to forfeit? This will count as a loss."
          confirmLabel="Forfeit"
          danger
          onConfirm={() => { setShowForfeit(false); battle.forfeit() }}
          onCancel={() => setShowForfeit(false)}
        />
      )}
    </div>
  )
}

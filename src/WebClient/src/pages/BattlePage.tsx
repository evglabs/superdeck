import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useBattle } from '../hooks/useBattle'
import { useIsMobile } from '../hooks/useMediaQuery'
import { LoadingSpinner } from '../components/LoadingSpinner'
import { HpBar } from '../components/HpBar'
import { StatusEffectBadge } from '../components/StatusEffectBadge'
import { BattleLog } from '../components/BattleLog'
import { HandDisplay } from '../components/HandDisplay'
import { QueueDisplay } from '../components/QueueDisplay'
import { CardDetailModal } from '../components/CardDetailModal'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { CardDisplay } from '../components/CardDisplay'
import { BattleEventDisplay, eventAnimationStyles } from '../components/BattleEventDisplay'

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

export function BattlePage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const battle = useBattle(id!)
  const [showForfeit, setShowForfeit] = useState(false)
  const isMobile = useIsMobile()

  const { state, loading, error, autoBattle, animation, speedMultiplier, setSpeedMultiplier } = battle

  // Use animated HP values when animation is playing back events
  const isAnimating = animation.isPlaying && !animation.isComplete
  const displayPlayerHP = isAnimating ? animation.displayedPlayerHP : (state?.player.currentHP ?? 0)
  const displayOpponentHP = isAnimating ? animation.displayedOpponentHP : (state?.opponent.currentHP ?? 0)

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
    <div className="battle-layout" style={{ maxWidth: 1200, margin: '0 auto' }}>
      {/* Animation styles */}
      <style>{eventAnimationStyles}</style>

      {/* Event Toast Display */}
      <BattleEventDisplay
        event={animation.currentEvent}
        isVisible={isAnimating}
      />

      {/* Main layout: content left, battle log right */}
      <div className="battle-main">

      {/* Battle Header */}
      <div style={{
        display: 'grid',
        gridTemplateColumns: isMobile ? '1fr 1fr' : '1fr auto 1fr',
        gap: isMobile ? 8 : 16,
        alignItems: 'center', marginBottom: 16,
      }}>
        <div style={{ order: isMobile ? 1 : undefined }}>
          <HpBar current={displayPlayerHP} max={state.player.maxHP} label={state.player.name} tint="#60a5fa" />
          <div style={{ fontSize: '0.8rem', marginTop: 4, color: state.playerGoesFirst ? '#22c55e' : 'var(--color-text-secondary)' }}>
            {state.playerGoesFirst ? 'FIRST' : 'second'}
          </div>
          <div style={{ fontSize: 'clamp(0.65rem, 1.5vw, 0.75rem)', marginTop: 4, display: 'flex', gap: isMobile ? 4 : 8, flexWrap: 'wrap' }}>
            <span style={{ color: 'var(--color-text-secondary)' }}>Lv.{state.player.level}</span>
            <StatValue label="ATK" base={state.player.attack} effective={state.playerEffectiveStats?.attack} color="#ef4444" />
            <StatValue label="DEF" base={state.player.defense} effective={state.playerEffectiveStats?.defense} color="#3b82f6" />
            <StatValue label="SPD" base={state.player.speed} effective={state.playerEffectiveStats?.speed} color="#eab308" />
          </div>
        </div>
        {!isMobile && (
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontSize: '0.85rem', color: 'var(--color-text-secondary)' }}>Round {state.round}</div>
            <div style={{ fontSize: '0.9rem', fontWeight: 600 }}>{state.phase}</div>
          </div>
        )}
        <div style={{ order: isMobile ? 2 : undefined }}>
          <HpBar current={displayOpponentHP} max={state.opponent.maxHP} label={state.opponent.name} tint="#f87171" />
          <div style={{ fontSize: '0.8rem', marginTop: 4, textAlign: 'right', color: !state.playerGoesFirst ? '#22c55e' : 'var(--color-text-secondary)' }}>
            {!state.playerGoesFirst ? 'FIRST' : 'second'}
          </div>
          <div style={{ fontSize: 'clamp(0.65rem, 1.5vw, 0.75rem)', marginTop: 4, display: 'flex', gap: isMobile ? 4 : 8, justifyContent: 'flex-end', flexWrap: 'wrap' }}>
            <span style={{ color: 'var(--color-text-secondary)' }}>Lv.{state.opponent.level}</span>
            <StatValue label="ATK" base={state.opponent.attack} effective={state.opponentEffectiveStats?.attack} color="#ef4444" />
            <StatValue label="DEF" base={state.opponent.defense} effective={state.opponentEffectiveStats?.defense} color="#3b82f6" />
            <StatValue label="SPD" base={state.opponent.speed} effective={state.opponentEffectiveStats?.speed} color="#eab308" />
          </div>
        </div>
        {isMobile && (
          <div style={{ gridColumn: '1 / -1', textAlign: 'center', order: 3 }}>
            <span style={{ fontSize: '0.85rem', color: 'var(--color-text-secondary)' }}>Round {state.round}</span>
            <span style={{ fontSize: '0.9rem', fontWeight: 600, marginLeft: 8 }}>{state.phase}</span>
          </div>
        )}
      </div>

      {/* Status Effects */}
      {(state.playerStatuses.length > 0 || state.opponentStatuses.length > 0) && (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: isMobile ? 8 : 16, marginBottom: 12 }}>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4 }}>
            {state.playerStatuses.map(s => <StatusEffectBadge key={s.id} effect={s} />)}
          </div>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4, justifyContent: 'flex-end' }}>
            {state.opponentStatuses.map(s => <StatusEffectBadge key={s.id} effect={s} />)}
          </div>
        </div>
      )}

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
        <div style={{ marginBottom: 12 }}>
          <QueueDisplay queue={state.playerQueue} maxSlots={state.currentPlayerQueueSlots} />
        </div>
      )}

      {/* Hand */}
      {isQueuePhase && (
        <div style={{ marginBottom: 12 }}>
          <div style={{ fontSize: '0.85rem', color: 'var(--color-text-secondary)', marginBottom: 4 }}>Your Hand</div>
          <HandDisplay
            cards={state.playerHand}
            onCardClick={i => { if (canQueue) battle.queueCard(i) }}
            onCardDetail={card => battle.setDetailCard(card)}
            disabled={!canQueue || loading}
          />
        </div>
      )}

      {/* Error */}
      {error && <div style={{ color: 'var(--color-danger)', fontSize: '0.9rem', marginBottom: 8 }}>{error}</div>}

      {/* Action Buttons */}
      {isQueuePhase && !autoBattle && (
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
          <button className="btn-primary" onClick={battle.confirmQueue} disabled={loading}>
            Confirm Queue
          </button>
          <button className="btn-secondary" onClick={battle.toggleAutoBattle}>
            Auto-Battle
          </button>
          <button className="btn-danger" onClick={() => setShowForfeit(true)}>
            Forfeit
          </button>
        </div>
      )}

      {autoBattle && (
        <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
          <span style={{ color: '#06b6d4', fontWeight: 600, fontSize: '0.9rem' }}>AUTO-BATTLE</span>
          <div className="spinner" />
          <button className="btn-secondary" onClick={battle.toggleAutoBattle}>
            Manual Mode
          </button>
        </div>
      )}

      {/* Animation Playback Controls - shown when playing events */}
      {isAnimating && (
        <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap', marginBottom: 12 }}>
          <span style={{ color: '#06b6d4', fontWeight: 600, fontSize: '0.9rem' }}>Playing events...</span>
          <div style={{ display: 'flex', gap: 8 }}>
            <button
              className="btn-secondary"
              onClick={animation.pause}
            >
              Pause
            </button>
            <button
              className="btn-secondary"
              onClick={animation.skipToEnd}
            >
              Skip
            </button>
          </div>
          {/* Speed Controls */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
            <span style={{ fontSize: '0.8rem', color: 'var(--color-text-secondary)' }}>Speed:</span>
            {[0.5, 1, 2, 4].map(speed => (
              <button
                key={speed}
                className={speedMultiplier === speed ? 'btn-primary' : 'btn-secondary'}
                onClick={() => setSpeedMultiplier(speed)}
                style={{
                  padding: '2px 8px',
                  fontSize: '0.75rem',
                  minWidth: 36,
                }}
              >
                {speed}x
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Paused playback controls */}
      {animation.isPaused && !animation.isComplete && (
        <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap', marginBottom: 12 }}>
          <span style={{ color: '#eab308', fontWeight: 600, fontSize: '0.9rem' }}>Paused</span>
          <div style={{ display: 'flex', gap: 8 }}>
            <button
              className="btn-primary"
              onClick={animation.play}
            >
              Resume
            </button>
            <button
              className="btn-secondary"
              onClick={animation.skipToEnd}
            >
              Skip
            </button>
          </div>
          {/* Speed Controls */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
            <span style={{ fontSize: '0.8rem', color: 'var(--color-text-secondary)' }}>Speed:</span>
            {[0.5, 1, 2, 4].map(speed => (
              <button
                key={speed}
                className={speedMultiplier === speed ? 'btn-primary' : 'btn-secondary'}
                onClick={() => setSpeedMultiplier(speed)}
                style={{
                  padding: '2px 8px',
                  fontSize: '0.75rem',
                  minWidth: 36,
                }}
              >
                {speed}x
              </button>
            ))}
          </div>
        </div>
      )}

      {!isQueuePhase && !autoBattle && !battleEnded && !isAnimating && !animation.isPaused && (
        <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
          <span className="text-muted" style={{ fontSize: '0.9rem' }}>Resolving...</span>
          <div className="spinner" />
        </div>
      )}

      {/* Battle Complete - show results button */}
      {battleEnded && (
        <div style={{ marginTop: 16, padding: 16, background: 'var(--color-surface)', borderRadius: 8, textAlign: 'center' }}>
          <div style={{
            fontSize: 'clamp(1.25rem, 4vw, 1.5rem)',
            fontWeight: 700,
            marginBottom: 8,
            color: state.winnerId === state.player.id ? '#22c55e' : '#ef4444'
          }}>
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

      </div>{/* End battle-main */}

      {/* Right column: Battle Log - hidden when battle ends */}
      {!battleEnded && (
        <div className="battle-sidebar">
          <div style={{ fontSize: '0.85rem', color: 'var(--color-text-secondary)', marginBottom: 4 }}>
            Battle Log {isAnimating && `(${visibleLogEntries.length}/${state.battleLog.length})`}
          </div>
          <BattleLog entries={visibleLogEntries} newStartIndex={battle.lastLogIndex} collapsible />
        </div>
      )}

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

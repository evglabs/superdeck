import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useBattle } from '../hooks/useBattle'
import { LoadingSpinner } from '../components/LoadingSpinner'
import { HpBar } from '../components/HpBar'
import { StatusEffectBadge } from '../components/StatusEffectBadge'
import { BattleLog } from '../components/BattleLog'
import { HandDisplay } from '../components/HandDisplay'
import { QueueDisplay } from '../components/QueueDisplay'
import { CardDetailModal } from '../components/CardDetailModal'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { CardDisplay } from '../components/CardDisplay'

export function BattlePage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const battle = useBattle(id!)
  const [showForfeit, setShowForfeit] = useState(false)

  const { state, loading, error, autoBattle } = battle

  if (!state) {
    return <div className="page"><LoadingSpinner message="Loading battle..." /></div>
  }

  // Navigate to result when battle ends
  if (state.isComplete || state.phase === 'Ended') {
    navigate(`/battle/${id}/result`, { replace: true })
    return null
  }

  const isQueuePhase = state.phase === 'Queue'
  const canQueue = isQueuePhase && !autoBattle && state.playerQueue.length < state.currentPlayerQueueSlots

  return (
    <div style={{ maxWidth: 900, margin: '0 auto', padding: '16px 24px' }}>
      {/* Battle Header */}
      <div style={{
        display: 'grid', gridTemplateColumns: '1fr auto 1fr', gap: 16,
        alignItems: 'center', marginBottom: 16,
      }}>
        <div>
          <HpBar current={state.player.currentHP} max={state.player.maxHP} label={state.player.name} tint="#60a5fa" />
          <div style={{ fontSize: '0.8rem', marginTop: 4, color: state.playerGoesFirst ? '#22c55e' : 'var(--color-text-secondary)' }}>
            {state.playerGoesFirst ? 'FIRST' : 'second'}
          </div>
        </div>
        <div style={{ textAlign: 'center' }}>
          <div style={{ fontSize: '0.85rem', color: 'var(--color-text-secondary)' }}>Round {state.round}</div>
          <div style={{ fontSize: '0.9rem', fontWeight: 600 }}>{state.phase}</div>
        </div>
        <div>
          <HpBar current={state.opponent.currentHP} max={state.opponent.maxHP} label={state.opponent.name} tint="#f87171" />
          <div style={{ fontSize: '0.8rem', marginTop: 4, textAlign: 'right', color: !state.playerGoesFirst ? '#22c55e' : 'var(--color-text-secondary)' }}>
            {!state.playerGoesFirst ? 'FIRST' : 'second'}
          </div>
        </div>
      </div>

      {/* Status Effects */}
      {(state.playerStatuses.length > 0 || state.opponentStatuses.length > 0) && (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 12 }}>
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

      {/* Battle Log */}
      <div style={{ marginBottom: 12 }}>
        <div style={{ fontSize: '0.85rem', color: 'var(--color-text-secondary)', marginBottom: 4 }}>Battle Log</div>
        <BattleLog entries={state.battleLog} newStartIndex={battle.lastLogIndex} />
      </div>

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
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <span style={{ color: '#06b6d4', fontWeight: 600, fontSize: '0.9rem' }}>AUTO-BATTLE</span>
          <div className="spinner" />
          <button className="btn-secondary" onClick={battle.toggleAutoBattle}>
            Manual Mode
          </button>
        </div>
      )}

      {!isQueuePhase && !autoBattle && !state.isComplete && (
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <span className="text-muted" style={{ fontSize: '0.9rem' }}>Resolving...</span>
          <div className="spinner" />
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

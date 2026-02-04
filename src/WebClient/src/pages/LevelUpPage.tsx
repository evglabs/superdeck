import { useState, useEffect } from 'react'
import { useParams, useNavigate, useLocation } from 'react-router-dom'
import { useGame } from '../context/GameContext'
import { api } from '../api/client'
import { LoadingSpinner } from '../components/LoadingSpinner'
import { CardDisplay } from '../components/CardDisplay'
import { CardDetailModal } from '../components/CardDetailModal'
import { StatStepper } from '../components/StatStepper'
import type { Card, BattleResult, BoosterPack } from '../types'

type Step = 'pack' | 'stats' | 'done'

export function LevelUpPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const location = useLocation()
  const { settings, currentCharacter, refreshCharacter } = useGame()

  const result = (location.state as { result?: BattleResult })?.result
  const levelsGained = result?.levelsGained ?? 1
  const characterId = currentCharacter?.id

  const [step, setStep] = useState<Step>('pack')
  const [currentLevel, setCurrentLevel] = useState(0)
  const [pack, setPack] = useState<BoosterPack | null>(null)
  const [packLoading, setPackLoading] = useState(false)

  // Booster pack actions
  const [cardsToAdd, setCardsToAdd] = useState<Card[]>([])
  const [cardsToRemove, setCardsToRemove] = useState<string[]>([])
  const maxActions = 3
  const actionsUsed = cardsToAdd.length + cardsToRemove.length

  // Stats
  const [attack, setAttack] = useState(0)
  const [defense, setDefense] = useState(0)
  const [speed, setSpeed] = useState(0)
  const [bonusHP, setBonusHP] = useState(0)
  const [saving, setSaving] = useState(false)

  const [detailCard, setDetailCard] = useState<Card | null>(null)
  const [deckCards, setDeckCards] = useState<{ id: string; card: Card }[]>([])

  useEffect(() => {
    if (currentLevel < levelsGained) {
      loadPack()
    }
  }, [currentLevel])

  const loadPack = async () => {
    if (!characterId) return
    setPackLoading(true)
    setCardsToAdd([])
    setCardsToRemove([])
    try {
      const p = await api.generatePack(characterId)
      setPack(p)
      // Load deck cards for removal
      const character = await api.getCharacter(characterId)
      const allCards = await api.getCards()
      const cardMap = new Map(allCards.map(c => [c.id, c]))
      setDeckCards(character.deckCardIds.map(cid => ({ id: cid, card: cardMap.get(cid)! })).filter(x => x.card))
    } catch {
      setPack(null)
    } finally {
      setPackLoading(false)
    }
  }

  const toggleAddCard = (card: Card) => {
    setCardsToAdd(prev => {
      const exists = prev.find(c => c.id === card.id)
      if (exists) return prev.filter(c => c.id !== card.id)
      if (actionsUsed >= maxActions) return prev
      return [...prev, card]
    })
  }

  const toggleRemoveCard = (cardId: string) => {
    setCardsToRemove(prev => {
      if (prev.includes(cardId)) return prev.filter(x => x !== cardId)
      if (actionsUsed >= maxActions) return prev
      // Enforce min deck size
      const currentSize = (currentCharacter?.deckCardIds.length ?? 0) - prev.length
      if (currentSize <= 6) return prev
      return [...prev, cardId]
    })
  }

  const handleConfirmPack = async () => {
    if (!characterId) return
    setSaving(true)
    try {
      if (cardsToRemove.length > 0) {
        await api.removeCards(characterId, cardsToRemove)
      }
      if (cardsToAdd.length > 0) {
        await api.addCards(characterId, cardsToAdd.map(c => c.id))
      }
      await refreshCharacter(characterId)

      const nextLevel = currentLevel + 1
      if (nextLevel < levelsGained) {
        setCurrentLevel(nextLevel)
      } else {
        // Move to stat allocation
        const char = await api.getCharacter(characterId)
        if (char && settings) {
          setAttack(char.attack)
          setDefense(char.defense)
          setSpeed(char.speed)
          setBonusHP(char.bonusHP)
          const totalAllowed = char.level * settings.statPointsPerLevel
          const hpPts = Math.floor(char.bonusHP / settings.hpPerStatPoint)
          const used = char.attack + char.defense + char.speed + hpPts
          if (totalAllowed - used > 0) {
            setStep('stats')
          } else {
            setStep('done')
          }
        } else {
          setStep('done')
        }
      }
    } catch {
      // ignore
    } finally {
      setSaving(false)
    }
  }

  const handleSaveStats = async () => {
    if (!characterId) return
    setSaving(true)
    try {
      await api.updateStats(characterId, attack, defense, speed, bonusHP)
      await refreshCharacter(characterId)
    } catch {
      // ignore
    }
    setSaving(false)
    setStep('done')
  }

  if (!characterId || !settings) return <div className="page"><LoadingSpinner /></div>

  // Done
  if (step === 'done') {
    navigate(`/character/${characterId}`, { replace: true })
    return null
  }

  // Stats step
  if (step === 'stats') {
    const hpPerStatPoint = settings.hpPerStatPoint
    const totalAllowed = (currentCharacter?.level ?? 1) * settings.statPointsPerLevel
    const hpPts = Math.floor(bonusHP / hpPerStatPoint)
    const totalUsed = attack + defense + speed + hpPts
    const remaining = Math.max(0, totalAllowed - totalUsed)

    return (
      <div className="page" style={{ maxWidth: 500 }}>
        <h1 className="page-title" style={{ color: '#eab308' }}>Stat Points!</h1>
        <p style={{ marginBottom: 16 }}>You have <strong style={{ color: 'var(--color-success)' }}>{remaining}</strong> point(s) to allocate.</p>

        <div className="panel flex flex-col gap-3">
          <StatStepper label={`HP (+${hpPerStatPoint})`} value={bonusHP} displayValue={`+${bonusHP}`} color="#22c55e"
            onIncrement={() => { if (remaining > 0) setBonusHP(bonusHP + hpPerStatPoint) }}
            onDecrement={() => { if (bonusHP >= hpPerStatPoint) setBonusHP(bonusHP - hpPerStatPoint) }}
            canIncrement={remaining > 0} canDecrement={bonusHP >= hpPerStatPoint}
          />
          <StatStepper label={`Attack (+${settings.attackPercentPerPoint}% dmg)`} value={attack} color="#ef4444"
            onIncrement={() => { if (remaining > 0) setAttack(attack + 1) }}
            onDecrement={() => { if (attack > (currentCharacter?.attack ?? 0)) setAttack(attack - 1) }}
            canIncrement={remaining > 0} canDecrement={attack > (currentCharacter?.attack ?? 0)}
          />
          <StatStepper label={`Defense (-${settings.defensePercentPerPoint}% dmg taken)`} value={defense} color="#3b82f6"
            onIncrement={() => { if (remaining > 0) setDefense(defense + 1) }}
            onDecrement={() => { if (defense > (currentCharacter?.defense ?? 0)) setDefense(defense - 1) }}
            canIncrement={remaining > 0} canDecrement={defense > (currentCharacter?.defense ?? 0)}
          />
          <StatStepper label="Speed" value={speed} color="#eab308"
            onIncrement={() => { if (remaining > 0) setSpeed(speed + 1) }}
            onDecrement={() => { if (speed > Math.max(1, currentCharacter?.speed ?? 1)) setSpeed(speed - 1) }}
            canIncrement={remaining > 0} canDecrement={speed > Math.max(1, currentCharacter?.speed ?? 1)}
          />
        </div>

        <div className="flex gap-2 mt-4">
          <button className="btn-secondary" onClick={() => setStep('done')}>Skip</button>
          <button className="btn-primary" onClick={handleSaveStats} disabled={saving}>
            {saving ? 'Saving...' : 'Save Stats'}
          </button>
        </div>
      </div>
    )
  }

  // Pack step
  if (packLoading) return <div className="page"><LoadingSpinner message="Generating booster pack..." /></div>

  const sortedPackCards = pack?.cards
    ? [...pack.cards].sort((a, b) => a.rarity.localeCompare(b.rarity) || a.name.localeCompare(b.name))
    : []

  return (
    <div className="page">
      <h1 className="page-title" style={{ color: '#eab308' }}>
        Level {(result?.newLevel ?? 1) - levelsGained + currentLevel + 1} Booster Pack
      </h1>
      <p className="text-muted mb-4">
        Actions: {actionsUsed}/{maxActions} used. Each action can add a pack card or remove a deck card.
      </p>

      {/* Pack Cards */}
      <div className="panel">
        <div style={{ fontWeight: 600, marginBottom: 8 }}>Pack Cards</div>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10 }}>
          {sortedPackCards.map((card, i) => (
            <CardDisplay
              key={`${card.id}-${i}`}
              card={card}
              selected={cardsToAdd.some(c => c.id === card.id)}
              onClick={() => toggleAddCard(card)}
            />
          ))}
        </div>
      </div>

      {/* Pending actions */}
      {(cardsToAdd.length > 0 || cardsToRemove.length > 0) && (
        <div className="panel" style={{ fontSize: '0.9rem' }}>
          <div style={{ fontWeight: 600, marginBottom: 4 }}>Pending Actions</div>
          {cardsToAdd.map(c => (
            <div key={c.id} style={{ color: '#22c55e' }}>+ ADD {c.name}</div>
          ))}
          {cardsToRemove.map(cid => {
            const dc = deckCards.find(x => x.id === cid)
            return <div key={cid} style={{ color: '#ef4444' }}>- REMOVE {dc?.card.name ?? cid}</div>
          })}
        </div>
      )}

      {/* Deck cards for removal */}
      {actionsUsed < maxActions && (
        <details style={{ marginBottom: 16 }}>
          <summary style={{ cursor: 'pointer', color: 'var(--color-text-secondary)', fontSize: '0.9rem', marginBottom: 8 }}>
            Remove card from deck ({deckCards.length} cards, min 6)
          </summary>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, maxHeight: 300, overflowY: 'auto' }}>
            {deckCards.filter(dc => !cardsToRemove.includes(dc.id)).map((dc, i) => (
              <CardDisplay
                key={`${dc.id}-${i}`}
                card={dc.card}
                compact
                onClick={() => toggleRemoveCard(dc.id)}
              />
            ))}
          </div>
        </details>
      )}

      <div className="flex gap-2">
        <button className="btn-secondary" onClick={handleConfirmPack} disabled={saving}>
          {cardsToAdd.length === 0 && cardsToRemove.length === 0 ? 'Skip' : 'Done'}
        </button>
        {(cardsToAdd.length > 0 || cardsToRemove.length > 0) && (
          <button className="btn-primary" onClick={handleConfirmPack} disabled={saving}>
            {saving ? 'Applying...' : `Confirm (${cardsToAdd.length} add, ${cardsToRemove.length} remove)`}
          </button>
        )}
      </div>

      {detailCard && <CardDetailModal card={detailCard} onClose={() => setDetailCard(null)} />}
    </div>
  )
}

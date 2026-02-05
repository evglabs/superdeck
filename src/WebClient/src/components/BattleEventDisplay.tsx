import type { BattleEvent } from '../types/events'

interface BattleEventDisplayProps {
  event: BattleEvent | null
  isVisible: boolean
}

export function BattleEventDisplay({ event, isVisible }: BattleEventDisplayProps) {
  if (!event || !isVisible) {
    return null
  }

  const { content, icon, colorClass } = getEventDisplay(event)

  return (
    <div
      className={`
        fixed top-1/3 left-1/2 transform -translate-x-1/2 -translate-y-1/2
        px-6 py-4 rounded-lg shadow-xl z-50
        animate-fade-in-out pointer-events-none
        ${colorClass}
      `}
    >
      <div className="flex items-center gap-3">
        <span className="text-2xl">{icon}</span>
        <span className="text-lg font-bold">{content}</span>
      </div>
    </div>
  )
}

interface EventDisplay {
  content: string
  icon: string
  colorClass: string
}

function getEventDisplay(event: BattleEvent): EventDisplay {
  switch (event.eventType) {
    case 'round_start':
      return {
        content: `Round ${event.roundNumber}`,
        icon: '\u2694\uFE0F',
        colorClass: 'bg-blue-600 text-white',
      }

    case 'speed_roll':
      return {
        content: event.playerGoesFirst
          ? `You go first! (${event.playerSpeed} vs ${event.opponentSpeed})`
          : `${event.opponentName} goes first! (${event.opponentSpeed} vs ${event.playerSpeed})`,
        icon: '\u26A1',
        colorClass: 'bg-yellow-500 text-black',
      }

    case 'card_played':
      return {
        content: `${event.casterName} plays ${event.card.name}!`,
        icon: '\uD83C\uDCCF',
        colorClass: event.casterIsPlayer ? 'bg-green-600 text-white' : 'bg-red-600 text-white',
      }

    case 'damage_dealt':
      return {
        content: `${event.targetName} takes ${event.finalDamage} damage!`,
        icon: '\uD83D\uDCA5',
        colorClass: 'bg-red-700 text-white',
      }

    case 'healing':
      return {
        content: `${event.targetName} heals ${event.amount} HP!`,
        icon: '\uD83D\uDC9A',
        colorClass: 'bg-green-500 text-white',
      }

    case 'status_gained':
      return {
        content: `${event.targetName} gains ${event.statusName}!`,
        icon: event.isBuff ? '\u2B06\uFE0F' : '\u2B07\uFE0F',
        colorClass: event.isBuff ? 'bg-blue-500 text-white' : 'bg-purple-600 text-white',
      }

    case 'status_expired':
      return {
        content: `${event.statusName} expired on ${event.targetName}`,
        icon: '\u23F0',
        colorClass: 'bg-gray-500 text-white',
      }

    case 'status_triggered':
      return {
        content: event.message || `${event.statusName} triggers!`,
        icon: '\u2728',
        colorClass: 'bg-purple-500 text-white',
      }

    case 'battle_end':
      return {
        content: event.playerWon ? 'Victory!' : 'Defeat!',
        icon: event.playerWon ? '\uD83C\uDFC6' : '\uD83D\uDC80',
        colorClass: event.playerWon ? 'bg-yellow-400 text-black' : 'bg-gray-800 text-white',
      }

    case 'message':
      return {
        content: event.message,
        icon: '\uD83D\uDCAC',
        colorClass: 'bg-gray-600 text-white',
      }

    default:
      return {
        content: 'Unknown event',
        icon: '\u2753',
        colorClass: 'bg-gray-500 text-white',
      }
  }
}

// Add animation styles via inline style tag or include in your CSS
export const eventAnimationStyles = `
@keyframes fadeInOut {
  0% {
    opacity: 0;
    transform: translate(-50%, -50%) scale(0.8);
  }
  15% {
    opacity: 1;
    transform: translate(-50%, -50%) scale(1);
  }
  85% {
    opacity: 1;
    transform: translate(-50%, -50%) scale(1);
  }
  100% {
    opacity: 0;
    transform: translate(-50%, -50%) scale(0.8);
  }
}

.animate-fade-in-out {
  animation: fadeInOut 0.5s ease-in-out forwards;
}
`

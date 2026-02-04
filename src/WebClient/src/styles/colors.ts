import type { CardType, Rarity } from '../types'

export const typeColors: Record<CardType, string> = {
  Attack: '#ef4444',
  Defense: '#3b82f6',
  Buff: '#22c55e',
  Debuff: '#a855f7',
  Utility: '#6b7280',
}

export const rarityColors: Record<Rarity, string> = {
  Common: '#9ca3af',
  Uncommon: '#22c55e',
  Rare: '#3b82f6',
  Epic: '#a855f7',
  Legendary: '#eab308',
}

export function hpColor(percent: number): string {
  if (percent > 50) return '#22c55e'
  if (percent > 25) return '#eab308'
  return '#ef4444'
}

export function logEntryColor(text: string): string {
  if (text.includes('damage')) return '#ef4444'
  if (text.includes('heal') || text.includes('restore')) return '#22c55e'
  if (text.includes('wins') || text.includes('VICTORY')) return '#eab308'
  if (text.includes('Round') || text.includes('---')) return '#eab308'
  return '#9ca3af'
}

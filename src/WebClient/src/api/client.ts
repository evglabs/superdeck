import type {
  AuthResponse,
  PlayerInfoResponse,
  Character,
  Card,
  BattleState,
  StartBattleResponse,
  ActionResponse,
  BattleResult,
  InstantBattleResponse,
  BoosterPack,
  ServerInfo,
  HealthResponse,
  PlayerAction,
  Suit,
} from '../types'

interface AppConfig {
  apiUrl: string
}

let _config: AppConfig = { apiUrl: '' }

export async function loadConfig(): Promise<void> {
  try {
    const res = await fetch('/config.json')
    if (res.ok) {
      const json = await res.json()
      _config = { apiUrl: (json.apiUrl ?? '').replace(/\/+$/, '') }
    }
  } catch {
    // Fall back to relative URLs (works with Vite proxy in dev)
  }
}

export function getConfig(): AppConfig {
  return _config
}

class ApiClient {
  private token: string | null = null

  constructor() {
    this.token = localStorage.getItem('auth_token')
  }

  private get baseUrl(): string {
    return _config.apiUrl
  }

  private headers(): HeadersInit {
    const h: HeadersInit = { 'Content-Type': 'application/json' }
    if (this.token) {
      h['Authorization'] = `Bearer ${this.token}`
    }
    return h
  }

  private async request<T>(url: string, options?: RequestInit): Promise<T> {
    const res = await fetch(`${this.baseUrl}${url}`, {
      ...options,
      headers: { ...this.headers(), ...options?.headers },
    })
    if (!res.ok) {
      const body = await res.json().catch(() => ({}))
      throw new Error(body.error || `Request failed: ${res.status}`)
    }
    if (res.status === 204) return undefined as T
    return res.json()
  }

  // Auth
  async register(username: string, password: string): Promise<AuthResponse> {
    const res = await this.request<AuthResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    })
    this.setToken(res.token)
    return res
  }

  async login(username: string, password: string): Promise<AuthResponse> {
    const res = await this.request<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    })
    this.setToken(res.token)
    return res
  }

  async logout(): Promise<void> {
    try {
      await this.request<{ message: string }>('/api/auth/logout', { method: 'POST' })
    } catch {
      // ignore
    }
    this.clearToken()
  }

  async getMe(): Promise<PlayerInfoResponse> {
    return this.request<PlayerInfoResponse>('/api/auth/me')
  }

  // Characters
  async getCharacters(playerId?: string): Promise<Character[]> {
    const q = playerId ? `?playerId=${playerId}` : ''
    return this.request<Character[]>(`/api/characters${q}`)
  }

  async createCharacter(name: string, suitChoice: Suit, playerId?: string): Promise<Character> {
    return this.request<Character>('/api/characters', {
      method: 'POST',
      body: JSON.stringify({ name, suitChoice, playerId }),
    })
  }

  async getCharacter(id: string): Promise<Character> {
    return this.request<Character>(`/api/characters/${id}`)
  }

  async updateStats(id: string, attack: number, defense: number, speed: number, bonusHP = 0): Promise<Character> {
    return this.request<Character>(`/api/characters/${id}/stats`, {
      method: 'PUT',
      body: JSON.stringify({ attack, defense, speed, bonusHP }),
    })
  }

  async deleteCharacter(id: string): Promise<void> {
    return this.request<void>(`/api/characters/${id}`, { method: 'DELETE' })
  }

  async addCards(characterId: string, cardIds: string[]): Promise<Character> {
    return this.request<Character>(`/api/characters/${characterId}/cards`, {
      method: 'POST',
      body: JSON.stringify({ cardIds }),
    })
  }

  async removeCards(characterId: string, cardIds: string[]): Promise<Character> {
    return this.request<Character>(`/api/characters/${characterId}/cards`, {
      method: 'DELETE',
      body: JSON.stringify({ cardIds }),
    })
  }

  // Cards
  async getCards(): Promise<Card[]> {
    return this.request<Card[]>('/api/cards')
  }

  async getCard(id: string): Promise<Card> {
    return this.request<Card>(`/api/cards/${id}`)
  }

  async getCardsBySuit(suit: Suit): Promise<Card[]> {
    return this.request<Card[]>(`/api/cards/suit/${suit}`)
  }

  async getStarterPack(suit: Suit): Promise<Card[]> {
    return this.request<Card[]>(`/api/cards/starterpack/${suit}`)
  }

  // Battle
  async startBattle(characterId: string, autoBattle = false, autoBattleMode = 'Watch', aiProfileId?: string): Promise<StartBattleResponse> {
    return this.request<StartBattleResponse>('/api/battle/start', {
      method: 'POST',
      body: JSON.stringify({ characterId, autoBattle, autoBattleMode, aiProfileId }),
    })
  }

  async submitAction(battleId: string, action: PlayerAction): Promise<ActionResponse> {
    return this.request<ActionResponse>(`/api/battle/${battleId}/action`, {
      method: 'POST',
      body: JSON.stringify(action),
    })
  }

  async getBattleState(battleId: string): Promise<BattleState> {
    return this.request<BattleState>(`/api/battle/${battleId}/state`)
  }

  async forfeitBattle(battleId: string): Promise<BattleState> {
    return this.request<BattleState>(`/api/battle/${battleId}/forfeit`, { method: 'POST' })
  }

  async finalizeBattle(battleId: string): Promise<BattleResult> {
    return this.request<BattleResult>(`/api/battle/${battleId}/finalize`, { method: 'POST' })
  }

  async toggleAutoBattle(battleId: string, enabled: boolean, aiProfileId?: string): Promise<BattleState> {
    return this.request<BattleState>(`/api/battle/${battleId}/auto-battle`, {
      method: 'POST',
      body: JSON.stringify({ enabled, aiProfileId }),
    })
  }

  async autoQueue(battleId: string): Promise<ActionResponse> {
    return this.request<ActionResponse>(`/api/battle/${battleId}/auto-queue`, { method: 'POST' })
  }

  async instantBattle(characterId: string, aiProfileId?: string): Promise<InstantBattleResponse> {
    return this.request<InstantBattleResponse>('/api/battle/instant', {
      method: 'POST',
      body: JSON.stringify({ characterId, autoBattle: true, autoBattleMode: 'Instant', aiProfileId }),
    })
  }

  // Packs
  async generatePack(characterId: string): Promise<BoosterPack> {
    return this.request<BoosterPack>('/api/packs/generate', {
      method: 'POST',
      body: JSON.stringify({ characterId }),
    })
  }

  // System
  async health(): Promise<HealthResponse> {
    return this.request<HealthResponse>('/api/health')
  }

  async getServerInfo(): Promise<ServerInfo> {
    return this.request<ServerInfo>('/api/info')
  }

  // Token management
  setToken(token: string) {
    this.token = token
    localStorage.setItem('auth_token', token)
  }

  clearToken() {
    this.token = null
    localStorage.removeItem('auth_token')
    localStorage.removeItem('player_id')
    localStorage.removeItem('username')
  }

  getToken(): string | null {
    return this.token
  }

  isAuthenticated(): boolean {
    return this.token !== null
  }
}

export const api = new ApiClient()

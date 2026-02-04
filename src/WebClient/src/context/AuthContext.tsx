import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react'
import { api } from '../api/client'

interface AuthState {
  token: string | null
  playerId: string | null
  username: string | null
  loading: boolean
}

interface AuthContextValue extends AuthState {
  login: (username: string, password: string) => Promise<void>
  register: (username: string, password: string) => Promise<void>
  logout: () => Promise<void>
  isAuthenticated: boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({
    token: localStorage.getItem('auth_token'),
    playerId: localStorage.getItem('player_id'),
    username: localStorage.getItem('username'),
    loading: true,
  })

  useEffect(() => {
    if (state.token) {
      api.getMe()
        .then(info => {
          setState(prev => ({
            ...prev,
            playerId: info.playerId,
            username: info.username,
            loading: false,
          }))
          localStorage.setItem('player_id', info.playerId)
          localStorage.setItem('username', info.username)
        })
        .catch(() => {
          api.clearToken()
          setState({ token: null, playerId: null, username: null, loading: false })
        })
    } else {
      setState(prev => ({ ...prev, loading: false }))
    }
  }, [])

  const login = useCallback(async (username: string, password: string) => {
    const res = await api.login(username, password)
    localStorage.setItem('player_id', res.playerId)
    localStorage.setItem('username', res.username)
    setState({ token: res.token, playerId: res.playerId, username: res.username, loading: false })
  }, [])

  const register = useCallback(async (username: string, password: string) => {
    const res = await api.register(username, password)
    localStorage.setItem('player_id', res.playerId)
    localStorage.setItem('username', res.username)
    setState({ token: res.token, playerId: res.playerId, username: res.username, loading: false })
  }, [])

  const logout = useCallback(async () => {
    await api.logout()
    setState({ token: null, playerId: null, username: null, loading: false })
  }, [])

  const value: AuthContextValue = {
    ...state,
    login,
    register,
    logout,
    isAuthenticated: state.token !== null,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}

import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function MainMenu() {
  const { isAuthenticated, login, register } = useAuth()
  const navigate = useNavigate()
  const [mode, setMode] = useState<'menu' | 'login' | 'register'>('menu')
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      if (mode === 'login') {
        await login(username, password)
      } else {
        await register(username, password)
      }
      setMode('menu')
      setUsername('')
      setPassword('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed')
    } finally {
      setLoading(false)
    }
  }

  if (mode === 'login' || mode === 'register') {
    return (
      <div className="page" style={{ maxWidth: 400 }}>
        <h1 className="page-title">{mode === 'login' ? 'Login' : 'Register'}</h1>
        <form onSubmit={handleSubmit} className="panel flex flex-col gap-3">
          <input
            placeholder="Username"
            value={username}
            onChange={e => setUsername(e.target.value)}
            autoFocus
            required
          />
          <input
            type="password"
            placeholder="Password"
            value={password}
            onChange={e => setPassword(e.target.value)}
            required
          />
          {error && <div style={{ color: 'var(--color-danger)', fontSize: '0.9rem' }}>{error}</div>}
          <button className="btn-primary" type="submit" disabled={loading}>
            {loading ? 'Please wait...' : mode === 'login' ? 'Login' : 'Register'}
          </button>
          <button type="button" className="btn-secondary" onClick={() => { setMode('menu'); setError(null) }}>
            Back
          </button>
        </form>
      </div>
    )
  }

  return (
    <div className="page" style={{ maxWidth: 400 }}>
      <div style={{ textAlign: 'center', marginBottom: 32, marginTop: 40 }}>
        <h1 style={{ fontSize: '2.5rem', color: '#eab308', fontWeight: 800, marginBottom: 4 }}>SuperDeck</h1>
        <p className="text-muted">A Deck-Building Card Game</p>
      </div>

      <div className="flex flex-col gap-3">
        {!isAuthenticated && (
          <>
            <button className="btn-primary" style={{ padding: '12px' }} onClick={() => setMode('login')}>
              Login
            </button>
            <button className="btn-secondary" style={{ padding: '12px' }} onClick={() => setMode('register')}>
              Register
            </button>
          </>
        )}
        {isAuthenticated && (
          <>
            <button className="btn-primary" style={{ padding: '12px' }} onClick={() => navigate('/characters/new')}>
              New Character
            </button>
            <button className="btn-secondary" style={{ padding: '12px' }} onClick={() => navigate('/characters')}>
              Load Character
            </button>
          </>
        )}
      </div>
    </div>
  )
}

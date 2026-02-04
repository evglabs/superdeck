import { Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useGame } from '../context/GameContext'

export function Layout() {
  const { isAuthenticated, username, logout } = useAuth()
  const { serverOnline } = useGame()
  const navigate = useNavigate()

  const handleLogout = async () => {
    await logout()
    navigate('/menu')
  }

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <header style={{
        background: 'var(--color-bg-secondary)',
        borderBottom: '1px solid var(--color-border)',
        padding: '10px 24px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <span
            style={{ fontWeight: 700, fontSize: '1.2rem', color: '#eab308', cursor: 'pointer' }}
            onClick={() => navigate('/menu')}
          >
            SuperDeck
          </span>
          <span style={{
            display: 'inline-block',
            width: 8,
            height: 8,
            borderRadius: '50%',
            background: serverOnline ? 'var(--color-success)' : 'var(--color-danger)',
          }} />
        </div>
        {isAuthenticated && (
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <span style={{ color: 'var(--color-text-secondary)', fontSize: '0.9rem' }}>{username}</span>
            <button className="btn-secondary" style={{ padding: '4px 12px', fontSize: '0.85rem' }} onClick={handleLogout}>
              Logout
            </button>
          </div>
        )}
      </header>
      <main style={{ flex: 1 }}>
        <Outlet />
      </main>
    </div>
  )
}

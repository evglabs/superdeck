import { Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useGame } from '../context/GameContext'
import { useTheme } from '../context/ThemeContext'

export function Layout() {
  const { isAuthenticated, username, logout } = useAuth()
  const { serverOnline } = useGame()
  const { theme, toggleTheme } = useTheme()
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
        padding: 'clamp(8px, 2vw, 10px) clamp(12px, 3vw, 24px)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        flexWrap: 'wrap',
        gap: 8,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <span
            style={{ fontWeight: 700, fontSize: 'clamp(1rem, 2vw + 0.5rem, 1.25rem)', color: 'var(--color-brand)', cursor: 'pointer' }}
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
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <button
            className="btn-secondary"
            onClick={toggleTheme}
            style={{ padding: '4px 10px', fontSize: '1rem', lineHeight: 1 }}
            aria-label={`Switch to ${theme === 'dark' ? 'light' : 'dark'} theme`}
          >
            {theme === 'dark' ? '\u2600' : '\u263E'}
          </button>
          {isAuthenticated && (
            <>
              <span style={{ color: 'var(--color-text-secondary)', fontSize: '0.9rem' }}>{username}</span>
              <button className="btn-secondary" style={{ padding: '4px 12px', fontSize: '0.85rem' }} onClick={handleLogout}>
                Logout
              </button>
            </>
          )}
        </div>
      </header>
      <main style={{ flex: 1 }}>
        <Outlet />
      </main>
    </div>
  )
}

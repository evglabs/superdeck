import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { ThemeProvider } from './context/ThemeContext'
import { AuthProvider } from './context/AuthContext'
import { GameProvider } from './context/GameContext'
import { App } from './App'
import { loadConfig } from './api/client'
import './styles/global.css'

loadConfig().then(() => {
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <BrowserRouter>
        <ThemeProvider>
          <AuthProvider>
            <GameProvider>
              <App />
            </GameProvider>
          </AuthProvider>
        </ThemeProvider>
      </BrowserRouter>
    </StrictMode>,
  )
})

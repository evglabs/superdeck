import { Routes, Route, Navigate } from 'react-router-dom'
import { Layout } from './components/Layout'
import { MainMenu } from './pages/MainMenu'
import { CharacterCreation } from './pages/CharacterCreation'
import { CharacterSelection } from './pages/CharacterSelection'
import { CharacterHub } from './pages/CharacterHub'
import { DeckView } from './pages/DeckView'
import { StatAllocation } from './pages/StatAllocation'
import { BattlePage } from './pages/BattlePage'
import { BattleResultPage } from './pages/BattleResultPage'
import { LevelUpPage } from './pages/LevelUpPage'
import { RetirementPage } from './pages/RetirementPage'

export function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/menu" element={<MainMenu />} />
        <Route path="/characters/new" element={<CharacterCreation />} />
        <Route path="/characters" element={<CharacterSelection />} />
        <Route path="/character/:id" element={<CharacterHub />} />
        <Route path="/character/:id/deck" element={<DeckView />} />
        <Route path="/character/:id/stats" element={<StatAllocation />} />
        <Route path="/battle/:id" element={<BattlePage />} />
        <Route path="/battle/:id/result" element={<BattleResultPage />} />
        <Route path="/battle/:id/levelup" element={<LevelUpPage />} />
        <Route path="/character/:id/retired" element={<RetirementPage />} />
        <Route path="*" element={<Navigate to="/menu" replace />} />
      </Route>
    </Routes>
  )
}

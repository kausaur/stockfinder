import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Layout from './components/Layout';
import Dashboard from './pages/Dashboard';
import StockList from './pages/StockList';
import StockDetail from './pages/StockDetail';
import Alerts from './pages/Alerts';
import Settings from './pages/Settings';
import Admin from './pages/Admin';
import Recommendations from './pages/Recommendations';
import Screener from './pages/Screener';
import Compare from './pages/Compare';
import './index.css';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<Dashboard />} />
          <Route path="/stocks" element={<StockList />} />
          <Route path="/stocks/:id" element={<StockDetail />} />
          <Route path="/alerts" element={<Alerts />} />
          <Route path="/recommendations" element={<Recommendations />} />
          <Route path="/screener" element={<Screener />} />
          <Route path="/compare/:id" element={<Compare />} />
          <Route path="/settings" element={<Settings />} />
          <Route path="/admin" element={<Admin />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

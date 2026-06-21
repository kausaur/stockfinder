import { Outlet, NavLink, useLocation } from 'react-router-dom';
import { refreshData } from '../services/api';
import { useState } from 'react';

const navItems = [
  { path: '/', label: 'Dashboard', icon: '📊' },
  { path: '/stocks', label: 'Stocks', icon: '📈' },
  { path: '/alerts', label: 'Alerts', icon: '🚨' },
  { path: '/settings', label: 'Settings', icon: '⚙️' },
  { path: '/admin', label: 'Admin', icon: '🔧' },
];

export default function Layout() {
  const [refreshing, setRefreshing] = useState(false);
  const location = useLocation();

  const handleRefresh = async () => {
    setRefreshing(true);
    try { await refreshData(); } catch (e) { console.error(e); }
    setTimeout(() => setRefreshing(false), 2000);
  };

  return (
    <div className="min-h-screen bg-[#0f172a] flex">
      {/* Sidebar */}
      <aside className="w-64 bg-[#1e293b]/80 backdrop-blur-xl border-r border-slate-700/50 flex flex-col fixed h-full z-10">
        <div className="p-6 border-b border-slate-700/50">
          <h1 className="text-xl font-bold bg-gradient-to-r from-blue-400 to-emerald-400 bg-clip-text text-transparent">
            📈 Nifty50 Analyzer
          </h1>
          <p className="text-xs text-slate-500 mt-1">Stock Intelligence Platform</p>
        </div>
        <nav className="flex-1 p-4 space-y-1">
          {navItems.map(item => (
            <NavLink key={item.path} to={item.path}
              className={({ isActive }) =>
                `flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium transition-all duration-200 ${
                  isActive ? 'bg-blue-500/20 text-blue-400 shadow-lg shadow-blue-500/10' : 'text-slate-400 hover:text-slate-200 hover:bg-slate-700/50'
                }`
              }>
              <span className="text-lg">{item.icon}</span>{item.label}
            </NavLink>
          ))}
        </nav>
        <div className="p-4 border-t border-slate-700/50">
          <button onClick={handleRefresh} disabled={refreshing}
            className="w-full py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-blue-500 text-white text-sm font-medium hover:from-blue-500 hover:to-blue-400 transition-all disabled:opacity-50 flex items-center justify-center gap-2">
            {refreshing ? <span className="animate-spin">⟳</span> : '🔄'} {refreshing ? 'Refreshing...' : 'Refresh Data'}
          </button>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 ml-64">
        <header className="sticky top-0 z-20 bg-[#0f172a]/80 backdrop-blur-xl border-b border-slate-700/50 px-8 py-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold text-slate-200">
              {navItems.find(n => n.path === location.pathname)?.label || 'Stock Detail'}
            </h2>
            <div className="flex items-center gap-4">
              <span className="text-xs text-slate-500">{new Date().toLocaleDateString('en-IN', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}</span>
            </div>
          </div>
        </header>
        <div className="p-8 animate-fade-in">
          <Outlet />
        </div>
      </main>
    </div>
  );
}

import { Outlet, NavLink, useLocation } from 'react-router-dom';
import { refreshData, getAdminHealth } from '../services/api';
import { useState, useEffect } from 'react';

const navItems = [
  { path: '/', label: 'Dashboard', icon: '📊' },
  { path: '/stocks', label: 'Stocks', icon: '📈' },
  { path: '/alerts', label: 'Alerts', icon: '🚨' },
  { path: '/recommendations', label: 'Picks', icon: '🎯' },
  { path: '/screener', label: 'Screener', icon: '🔍' },
  { path: '/settings', label: 'Settings', icon: '⚙️' },
  { path: '/admin', label: 'Admin', icon: '🔧' },
];

export default function Layout() {
  const [refreshing, setRefreshing] = useState(false);
  const [lastRefresh, setLastRefresh] = useState(null);
  const location = useLocation();

  useEffect(() => {
    getAdminHealth()
      .then(r => setLastRefresh(r.data?.lastRefreshAt ? new Date(r.data.lastRefreshAt) : null))
      .catch(() => {});
  }, []);

  const formatRelative = (date) => {
    if (!date) return null;
    const mins = Math.round((Date.now() - date.getTime()) / 60000);
    if (mins < 1) return 'just now';
    if (mins < 60) return `${mins}m ago`;
    const hrs = Math.round(mins / 60);
    if (hrs < 24) return `${hrs}h ago`;
    return `${Math.round(hrs / 24)}d ago`;
  };

  const handleRefresh = async () => {
    setRefreshing(true);
    try { await refreshData(); } catch (e) { console.error(e); }
    setTimeout(() => setRefreshing(false), 2000);
  };

  return (
    <div className="min-h-screen bg-[#0f172a] flex flex-col md:flex-row pb-16 md:pb-0">
      {/* Desktop Sidebar */}
      <aside className="hidden md:flex w-64 bg-[#1e293b]/80 backdrop-blur-xl border-r border-slate-700/50 flex-col fixed h-full z-10">
        <div className="p-6 border-b border-slate-700/50">
          <h1 className="text-xl font-bold bg-gradient-to-r from-blue-400 to-emerald-400 bg-clip-text text-transparent">
            📈 Nifty50 Analyzer
          </h1>
          <p className="text-xs text-slate-500 mt-1">Stock Intelligence Platform</p>
        </div>
        <nav className="flex-1 p-4 space-y-1 overflow-y-auto">
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

      {/* Mobile Bottom Navigation */}
      <nav className="md:hidden fixed bottom-0 left-0 right-0 bg-[#1e293b]/95 backdrop-blur-xl border-t border-slate-700/50 z-50 flex justify-around items-center h-16 pb-safe">
        {navItems.map(item => (
          <NavLink key={item.path} to={item.path}
            className={({ isActive }) =>
              `flex flex-col items-center justify-center w-full h-full space-y-1 transition-colors ${
                isActive ? 'text-blue-400' : 'text-slate-400 hover:text-slate-200'
              }`
            }>
            <span className="text-xl">{item.icon}</span>
            <span className="text-[10px] font-medium leading-none">{item.label}</span>
          </NavLink>
        ))}
      </nav>

      {/* Main content */}
      <main className="flex-1 md:ml-64 w-full">
        <header className="sticky top-0 z-20 bg-[#0f172a]/80 backdrop-blur-xl border-b border-slate-700/50 px-4 md:px-8 py-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold text-slate-200 truncate pr-2">
              <span className="md:hidden mr-2">📈</span>
              {navItems.find(n => n.path === location.pathname)?.label || 'Stock Detail'}
            </h2>
            <div className="flex items-center gap-2 md:gap-4 flex-shrink-0">
              {lastRefresh && (
                <span className="text-xs text-slate-500 hidden sm:inline-block">
                  🔄 Data: <span className="text-slate-400">{formatRelative(lastRefresh)}</span>
                </span>
              )}
              <span className="text-xs text-slate-500 hidden md:inline-block">{new Date().toLocaleDateString('en-IN', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}</span>
              <button onClick={handleRefresh} disabled={refreshing}
                className="md:hidden bg-blue-600/20 text-blue-400 p-2 rounded-lg text-xs font-medium hover:bg-blue-600/30 flex items-center justify-center">
                {refreshing ? <span className="animate-spin">⟳</span> : '🔄'}
              </button>
            </div>
          </div>
        </header>
        <div className="p-4 md:p-8 animate-fade-in max-w-full overflow-x-hidden">
          <Outlet />
        </div>
      </main>
    </div>
  );
}

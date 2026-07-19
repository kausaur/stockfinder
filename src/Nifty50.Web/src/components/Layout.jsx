import { Outlet, NavLink, useLocation } from 'react-router-dom';
import { getDashboard } from '../services/api';
import { useState, useEffect } from 'react';
import { 
  LayoutDashboard, LineChart, Bell, Target, Search, Settings, Wrench,
  TrendingUp, ChevronLeft, ChevronRight, RefreshCw, AlertTriangle
} from 'lucide-react';

const navItems = [
  { path: '/', label: 'Dashboard', icon: <LayoutDashboard size={20} /> },
  { path: '/stocks', label: 'Stocks', icon: <LineChart size={20} /> },
  { path: '/alerts', label: 'Alerts', icon: <Bell size={20} /> },
  { path: '/recommendations', label: 'Picks', icon: <Target size={20} /> },
  { path: '/screener', label: 'Screener', icon: <Search size={20} /> },
  { path: '/settings', label: 'Settings', icon: <Settings size={20} /> },
  { path: '/admin', label: 'Admin', icon: <Wrench size={20} /> },
];

export default function Layout() {
  const [lastRefresh, setLastRefresh] = useState(null);
  const [healthLoaded, setHealthLoaded] = useState(false);
  const [collapsed, setCollapsed] = useState(false);
  const location = useLocation();

  useEffect(() => {
    getDashboard()
      .then(r => {
        setLastRefresh(r.data?.dataAsOf ? new Date(r.data.dataAsOf) : null);
        setHealthLoaded(true);
      })
      .catch(() => setHealthLoaded(true));
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

  const sidebarW = collapsed ? 'w-16' : 'w-64';
  const mainML = collapsed ? 'md:ml-16' : 'md:ml-64';

  return (
    <div className="min-h-screen bg-[#0f172a] flex flex-col md:flex-row pb-16 md:pb-0">
      {/* Desktop Sidebar */}
      <aside className={`hidden md:flex ${sidebarW} bg-[#1e293b]/80 backdrop-blur-xl border-r border-slate-700/50 flex-col fixed h-full z-10 transition-all duration-300`}>
        {/* Logo / Header */}
        <div className={`flex items-center border-b border-slate-700/50 h-[69px] ${collapsed ? 'justify-center px-2' : 'justify-between px-6'}`}>
          {!collapsed && (
            <div>
              <h1 className="text-xl font-bold bg-gradient-to-r from-blue-400 to-emerald-400 bg-clip-text text-transparent flex items-center">
                <TrendingUp className="text-emerald-400 mr-2" size={24} /> Nifty50
              </h1>
              <p className="text-xs text-slate-500">Stock Intelligence</p>
            </div>
          )}
          {collapsed && <TrendingUp className="text-emerald-400" size={24} />}
          <button
            onClick={() => setCollapsed(c => !c)}
            title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
            className={`text-slate-400 hover:text-slate-200 hover:bg-slate-700/50 rounded-lg p-1.5 transition-colors ${collapsed ? 'mt-4' : ''}`}
          >
            {collapsed ? <ChevronRight size={20} /> : <ChevronLeft size={20} />}
          </button>
        </div>

        {/* Nav Links */}
        <nav className="flex-1 p-2 space-y-1 overflow-y-auto">
          {navItems.map(item => (
            <NavLink key={item.path} to={item.path}
              title={collapsed ? item.label : undefined}
              className={({ isActive }) =>
                `flex items-center rounded-xl text-sm font-medium transition-all duration-200 ${
                  collapsed ? 'justify-center px-2 py-3' : 'gap-3 px-4 py-3'
                } ${
                  isActive
                    ? 'bg-blue-500/20 text-blue-400 shadow-lg shadow-blue-500/10'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-700/50'
                }`
              }>
              <span className="flex-shrink-0 flex items-center justify-center w-5">{item.icon}</span>
              {!collapsed && <span>{item.label}</span>}
            </NavLink>
          ))}
        </nav>
      </aside>

      {/* Mobile Bottom Navigation */}
      <nav className="md:hidden fixed bottom-0 left-0 right-0 bg-[#1e293b]/95 backdrop-blur-xl border-t border-slate-700/50 z-50 flex justify-around items-center h-16 pb-safe">
        {navItems.slice(0, 5).map(item => (
          <NavLink key={item.path} to={item.path}
            className={({ isActive }) =>
              `flex flex-col items-center justify-center w-full h-full space-y-1 transition-colors ${
                isActive ? 'text-blue-400' : 'text-slate-400 hover:text-slate-200'
              }`
            }>
            <span className="flex items-center justify-center mb-1">{item.icon}</span>
            <span className="text-[10px] font-medium leading-none">{item.label}</span>
          </NavLink>
        ))}
      </nav>

      {/* Main content */}
      <main className={`flex-1 ${mainML} w-full transition-all duration-300`}>
        <header className="sticky top-0 z-20 bg-[#0f172a]/80 backdrop-blur-xl border-b border-slate-700/50 px-4 md:px-8 py-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold text-slate-200 truncate pr-2 flex items-center">
              <span className="md:hidden mr-2"><TrendingUp className="text-emerald-400" size={20} /></span>
              {navItems.find(n => n.path === location.pathname)?.label || 'Stock Detail'}
            </h2>
            <div className="flex items-center gap-2 md:gap-4 flex-shrink-0">
              {!healthLoaded ? (
                <span className="text-xs text-slate-600 flex items-center"><RefreshCw size={14} className="mr-1 animate-spin" /> Loading...</span>
              ) : lastRefresh ? (
                <span className="text-xs text-slate-500 flex items-center">
                  <RefreshCw size={14} className="mr-1" /> {formatRelative(lastRefresh)} <span className="hidden md:inline ml-1">· {lastRefresh.toLocaleString('en-IN', { day: 'numeric', month: 'short', year: 'numeric', hour: 'numeric', minute: '2-digit', hour12: true })}</span>
                </span>
              ) : (
                <span className="text-xs text-slate-600 flex items-center"><RefreshCw size={14} className="mr-1" /> Not yet refreshed</span>
              )}
            </div>
          </div>
        </header>
        <div className="p-4 md:p-8 animate-fade-in max-w-full overflow-x-hidden">
          <Outlet />
          
          <div className="mt-12 p-4 bg-slate-800/50 border border-slate-700/50 rounded-xl text-xs text-slate-400">
            <p className="font-semibold text-slate-300 mb-2 flex items-center"><AlertTriangle size={16} className="mr-2 text-amber-500" /> Educational Purposes Only - Not SEBI Registered</p>
            <p className="leading-relaxed">This application provides stock analysis scores based on publicly available data for educational and informational purposes only. The creators are not SEBI-registered Research Analysts (SEBI RA Regulations, 2014) nor Investment Advisers (SEBI IA Regulations, 2013). All scores and signals are algorithmically generated and may not reflect current market conditions. Past performance is not indicative of future results. Investment in securities market are subject to market risks. Always consult a qualified, SEBI-registered financial advisor before making investment decisions.</p>
          </div>
        </div>
      </main>
    </div>
  );
}

import { useState, useEffect } from 'react';
import { getStocks } from '../services/api';
import { useNavigate } from 'react-router-dom';

const SignalBadge = ({ signal }) => {
  const cls = `signal-${(signal || 'hold').toLowerCase().replace(/\s/g, '')}`;
  return <span className={`${cls} px-2.5 py-1 rounded-full text-xs font-bold`}>{signal || '—'}</span>;
};

export default function StockList() {
  const [stocks, setStocks] = useState([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    getStocks().then(r => setStocks(r.data)).catch(console.error).finally(() => setLoading(false));
  }, []);

  const filtered = stocks.filter(s =>
    s.symbol.toLowerCase().includes(search.toLowerCase()) ||
    s.companyName.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-4">
        <input type="text" placeholder="🔍 Search by symbol or company..." value={search} onChange={e => setSearch(e.target.value)}
          className="flex-1 bg-slate-800/50 border border-slate-700/50 rounded-xl px-4 py-3 text-sm text-slate-200 placeholder-slate-500 focus:outline-none focus:border-blue-500/50 focus:ring-1 focus:ring-blue-500/20" />
        <span className="text-sm text-slate-500">{filtered.length} stocks</span>
      </div>
      <div className="glass-card overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-700/50">
              {['Symbol', 'Company', 'Sector', 'Price', 'Change%', 'Market Cap', 'Signal', 'Score'].map(h => (
                <th key={h} className="text-left px-4 py-3 text-xs text-slate-500 uppercase tracking-wider font-medium">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={8} className="text-center py-12 text-slate-500">Loading...</td></tr>
            ) : filtered.map(s => (
              <tr key={s.id} onClick={() => navigate(`/stocks/${s.id}`)}
                className="border-b border-slate-800/50 hover:bg-slate-700/30 cursor-pointer transition-colors">
                <td className="px-4 py-3 font-semibold text-blue-400">{s.symbol}</td>
                <td className="px-4 py-3 text-slate-300">{s.companyName}</td>
                <td className="px-4 py-3 text-slate-500">{s.sector || '—'}</td>
                <td className="px-4 py-3 font-mono text-white">₹{s.currentPrice?.toFixed(2) || '—'}</td>
                <td className={`px-4 py-3 font-mono font-bold ${(s.dayChangePercent || 0) >= 0 ? 'text-emerald-400' : 'text-red-400'}`}>
                  {(s.dayChangePercent || 0) >= 0 ? '+' : ''}{(s.dayChangePercent || 0).toFixed(2)}%
                </td>
                <td className="px-4 py-3 text-slate-400">{s.marketCap ? `₹${(s.marketCap / 1e10).toFixed(0)}K Cr` : '—'}</td>
                <td className="px-4 py-3"><SignalBadge signal={s.overallSignal} /></td>
                <td className="px-4 py-3 font-mono font-bold text-white">{s.overallScore ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

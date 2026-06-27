import { useState, useEffect } from 'react';
import { getStocks } from '../services/api';
import { useNavigate } from 'react-router-dom';

const SignalBadge = ({ signal }) => {
  const cls = `signal-${(signal || 'hold').toLowerCase().replace(/\s/g, '')}`;
  return <span className={`${cls} px-2.5 py-1 rounded-full text-xs font-bold`}>{signal || '—'}</span>;
};

const SORT_KEYS = {
  score: (a, b) => (b.overallScore ?? -1) - (a.overallScore ?? -1),
  symbol: (a, b) => a.symbol.localeCompare(b.symbol),
  price: (a, b) => (b.currentPrice ?? 0) - (a.currentPrice ?? 0),
  change: (a, b) => (b.dayChangePercent ?? -999) - (a.dayChangePercent ?? -999),
  marketcap: (a, b) => (b.marketCap ?? 0) - (a.marketCap ?? 0),
};

const SortIcon = ({ active, dir }) => (
  <span className={`ml-1 text-xs ${active ? 'text-blue-400' : 'text-slate-600'}`}>
    {active ? (dir === 'asc' ? '↑' : '↓') : '↕'}
  </span>
);

export default function StockList() {
  const [stocks, setStocks] = useState([]);
  const [search, setSearch] = useState('');
  const [sector, setSector] = useState('');
  const [sortKey, setSortKey] = useState('score');
  const [sortDir, setSortDir] = useState('desc');
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    getStocks().then(r => setStocks(r.data)).catch(console.error).finally(() => setLoading(false));
  }, []);

  const sectors = ['', ...Array.from(new Set(stocks.map(s => s.sector).filter(Boolean))).sort()];

  const handleSort = (key) => {
    if (sortKey === key) setSortDir(d => d === 'asc' ? 'desc' : 'asc');
    else { setSortKey(key); setSortDir('desc'); }
  };

  const filtered = stocks
    .filter(s =>
      (s.symbol.toLowerCase().includes(search.toLowerCase()) ||
       s.companyName.toLowerCase().includes(search.toLowerCase())) &&
      (sector === '' || s.sector === sector)
    )
    .sort((a, b) => {
      const cmp = SORT_KEYS[sortKey]?.(a, b) ?? 0;
      return sortDir === 'asc' ? -cmp : cmp;
    });

  const SortHeader = ({ label, sortId }) => (
    <th className="text-left px-4 py-3 text-xs text-slate-500 uppercase tracking-wider font-medium cursor-pointer select-none hover:text-slate-300 transition-colors"
      onClick={() => handleSort(sortId)}>
      {label}<SortIcon active={sortKey === sortId} dir={sortDir} />
    </th>
  );

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3 flex-wrap">
        <input type="text" placeholder="🔍 Search by symbol or company..." value={search}
          onChange={e => setSearch(e.target.value)}
          className="flex-1 min-w-[200px] bg-slate-800/50 border border-slate-700/50 rounded-xl px-4 py-3 text-sm text-slate-200 placeholder-slate-500 focus:outline-none focus:border-blue-500/50 focus:ring-1 focus:ring-blue-500/20" />
        <select value={sector} onChange={e => setSector(e.target.value)}
          className="bg-slate-800/50 border border-slate-700/50 rounded-xl px-4 py-3 text-sm text-slate-200 focus:outline-none focus:border-blue-500/50">
          {sectors.map(s => <option key={s} value={s}>{s || 'All Sectors'}</option>)}
        </select>
        <span className="text-sm text-slate-500">{filtered.length} stocks</span>
      </div>

      <div className="glass-card overflow-x-auto">
        <table className="w-full text-sm min-w-[800px]">
          <thead>
            <tr className="border-b border-slate-700/50">
              <SortHeader label="Symbol" sortId="symbol" />
              <th className="text-left px-4 py-3 text-xs text-slate-500 uppercase tracking-wider font-medium">Company</th>
              <th className="text-left px-4 py-3 text-xs text-slate-500 uppercase tracking-wider font-medium">Sector</th>
              <SortHeader label="Price" sortId="price" />
              <SortHeader label="Change %" sortId="change" />
              <SortHeader label="Market Cap" sortId="marketcap" />
              <th className="text-left px-4 py-3 text-xs text-slate-500 uppercase tracking-wider font-medium">Signal</th>
              <SortHeader label="Score" sortId="score" />
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={8} className="text-center py-12 text-slate-500">Loading...</td></tr>
            ) : filtered.length === 0 ? (
              <tr><td colSpan={8} className="text-center py-12 text-slate-500">No stocks match your filter.</td></tr>
            ) : filtered.map(s => (
              <tr key={s.id} onClick={() => navigate(`/stocks/${s.id}`)}
                className="border-b border-slate-800/50 hover:bg-slate-700/30 cursor-pointer transition-colors">
                <td className="px-4 py-3 font-semibold text-blue-400">{s.symbol}</td>
                <td className="px-4 py-3 text-slate-300">{s.companyName}</td>
                <td className="px-4 py-3">
                  {s.sector
                    ? <span className="px-2 py-0.5 bg-slate-700/50 rounded text-xs text-slate-400">{s.sector}</span>
                    : <span className="text-slate-600">—</span>}
                </td>
                <td className="px-4 py-3 font-mono text-white">
                  {s.currentPrice != null ? `₹${s.currentPrice.toFixed(2)}` : '—'}
                </td>
                <td className={`px-4 py-3 font-mono font-bold ${
                  s.dayChangePercent == null ? 'text-slate-500'
                  : s.dayChangePercent >= 0 ? 'text-emerald-400' : 'text-red-400'}`}>
                  {s.dayChangePercent != null
                    ? `${s.dayChangePercent >= 0 ? '+' : ''}${s.dayChangePercent.toFixed(2)}%`
                    : '—'}
                </td>
                <td className="px-4 py-3 text-slate-400">
                  {s.marketCap ? `₹${(s.marketCap / 1e10).toFixed(0)}K Cr` : '—'}
                </td>
                <td className="px-4 py-3"><SignalBadge signal={s.overallSignal} /></td>
                <td className="px-4 py-3">
                  {s.overallScore != null
                    ? <span className={`font-mono font-bold ${s.overallScore >= 65 ? 'text-emerald-400' : s.overallScore >= 45 ? 'text-amber-400' : 'text-red-400'}`}>{s.overallScore}</span>
                    : <span className="text-slate-600">—</span>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

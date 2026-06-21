import { useState, useEffect } from 'react';
import { getDashboard, getAlerts } from '../services/api';
import { useNavigate } from 'react-router-dom';

const SignalBadge = ({ signal }) => {
  const cls = `signal-${(signal || 'hold').toLowerCase().replace(/\s/g, '')}`;
  return <span className={`${cls} px-3 py-1 rounded-full text-xs font-bold`}>{signal || 'N/A'}</span>;
};

export default function Dashboard() {
  const [data, setData] = useState(null);
  const [alerts, setAlerts] = useState([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    Promise.all([getDashboard(), getAlerts()])
      .then(([d, a]) => { setData(d.data); setAlerts(a.data); })
      .catch(console.error).finally(() => setLoading(false));
  }, []);

  // Group alerts by their actual signal type for accurate display
  const alertsBySignal = alerts.reduce((acc, a) => {
    acc[a.overallSignal] = (acc[a.overallSignal] || 0) + 1;
    return acc;
  }, {});

  if (loading) return <div className="flex items-center justify-center h-64"><div className="animate-spin text-4xl">⟳</div></div>;
  if (!data) return <p className="text-slate-400">No data available. Try refreshing.</p>;

  return (
    <div className="space-y-6">
      {/* Alert Banner */}
      {alerts.length > 0 && (
        <div className="glass-card animate-pulse-glow p-5 border-amber-500/30 bg-amber-500/5">
          <div className="flex items-center gap-3">
            <span className="text-2xl">🚨</span>
            <div>
              <h3 className="font-bold text-amber-400">
                {alerts.length} Active Signal{alerts.length > 1 ? 's' : ''}
              </h3>
              <p className="text-sm text-slate-400">
                {Object.entries(alertsBySignal)
                  .map(([sig, cnt]) => `${cnt}× ${sig}`).join(' · ')}
                {' '}&mdash; {alerts.slice(0, 4).map(a => a.symbol).join(', ')}
                {alerts.length > 4 ? ` +${alerts.length - 4} more` : ''}
              </p>
            </div>
            <button onClick={() => navigate('/alerts')} className="ml-auto text-sm text-amber-400 hover:underline">View All →</button>
          </div>
        </div>
      )}

      {/* Stats */}
      <div className="grid grid-cols-4 gap-4">
        {[
          { label: 'Total Stocks', value: data.totalStocks, icon: '📊', color: 'blue' },
          { label: 'Active Alerts', value: data.alertCount, icon: '🚨', color: 'amber' },
          { label: 'Top Gainer', value: data.topGainers?.[0]?.symbol || '-', sub: data.topGainers?.[0]?.dayChangePercent ? `+${data.topGainers[0].dayChangePercent.toFixed(2)}%` : '', icon: '🟢', color: 'green' },
          { label: 'Top Loser', value: data.topLosers?.[0]?.symbol || '-', sub: data.topLosers?.[0]?.dayChangePercent ? `${data.topLosers[0].dayChangePercent.toFixed(2)}%` : '', icon: '🔴', color: 'red' },
        ].map((s, i) => (
          <div key={i} className="glass-card p-5">
            <div className="flex items-center gap-3 mb-2"><span className="text-xl">{s.icon}</span><span className="text-xs text-slate-500 uppercase tracking-wide">{s.label}</span></div>
            <div className="text-2xl font-bold text-white">{s.value}</div>
            {s.sub && <div className={`text-sm ${s.color === 'green' ? 'text-emerald-400' : 'text-red-400'}`}>{s.sub}</div>}
          </div>
        ))}
      </div>

      {/* Gainers & Losers */}
      <div className="grid grid-cols-2 gap-6">
        {[{ title: '🟢 Top Gainers', items: data.topGainers, isGain: true }, { title: '🔴 Top Losers', items: data.topLosers, isGain: false }].map(({ title, items, isGain }) => (
          <div key={title} className="glass-card p-5">
            <h3 className="text-sm font-semibold text-slate-300 mb-4">{title}</h3>
            <div className="space-y-3">
              {(items || []).map((s, i) => (
                <div key={i} onClick={() => navigate(`/stocks/${s.id}`)} className="flex items-center justify-between p-3 rounded-lg bg-slate-800/50 hover:bg-slate-700/50 cursor-pointer transition-colors">
                  <div><div className="font-semibold text-white text-sm">{s.symbol}</div><div className="text-xs text-slate-500">{s.companyName}</div></div>
                  <div className="text-right">
                    <div className="text-sm font-mono text-white">₹{s.currentPrice?.toFixed(2) || '-'}</div>
                    <div className={`text-xs font-bold ${isGain ? 'text-emerald-400' : 'text-red-400'}`}>{isGain ? '+' : ''}{s.dayChangePercent?.toFixed(2) || 0}%</div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>

      {/* Sector Performance */}
      {data.sectorPerformance?.length > 0 && (
        <div className="glass-card p-5">
          <h3 className="text-sm font-semibold text-slate-300 mb-4">📈 Sector Performance</h3>
          <div className="grid grid-cols-4 gap-3">
            {data.sectorPerformance.map((s, i) => (
              <div key={i} className={`p-3 rounded-lg text-center ${s.averageChangePercent >= 0 ? 'bg-emerald-500/10 border border-emerald-500/20' : 'bg-red-500/10 border border-red-500/20'}`}>
                <div className="text-xs text-slate-400">{s.sector}</div>
                <div className={`text-sm font-bold ${s.averageChangePercent >= 0 ? 'text-emerald-400' : 'text-red-400'}`}>{s.averageChangePercent >= 0 ? '+' : ''}{s.averageChangePercent.toFixed(2)}%</div>
                <div className="text-xs text-slate-500">{s.stockCount} stocks</div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

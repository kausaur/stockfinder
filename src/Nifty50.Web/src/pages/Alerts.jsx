import { useState, useEffect } from 'react';
import { getAlerts } from '../services/api';
import { useNavigate } from 'react-router-dom';

export default function Alerts() {
  const [alerts, setAlerts] = useState([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    getAlerts().then(r => setAlerts(r.data)).catch(console.error).finally(() => setLoading(false));
  }, []);

  return (
    <div className="space-y-4">
      <div className="glass-card p-5 border-amber-500/20 bg-amber-500/5">
        <div className="flex items-center gap-3">
          <span className="text-2xl">🚨</span>
          <h3 className="text-lg font-bold text-amber-400">Buy Alerts ({alerts.length})</h3>
        </div>
        <p className="text-sm text-slate-400 mt-1">Stocks meeting all alert thresholds based on your active scoring profile.</p>
      </div>

      <div className="glass-card overflow-x-auto">
        <table className="w-full text-sm min-w-[800px]">
          <thead><tr className="border-b border-slate-700/50">
            {['Stock', 'Signal', 'Overall', 'Technical', 'Fundamental', 'Sentiment', 'Dividend', 'Reasoning'].map(h => (
              <th key={h} className="text-left px-4 py-3 text-xs text-slate-500 uppercase tracking-wider font-medium">{h}</th>
            ))}
          </tr></thead>
          <tbody>
            {loading ? <tr><td colSpan={8} className="text-center py-12 text-slate-500">Loading...</td></tr>
            : alerts.length === 0 ? <tr><td colSpan={8} className="text-center py-12 text-slate-500">No alerts currently. Adjust thresholds in Settings.</td></tr>
            : alerts.map((a, i) => (
              <tr key={i} onClick={() => navigate(`/stocks/${a.stockId}`)} className="border-b border-slate-800/50 hover:bg-slate-700/30 cursor-pointer transition-colors">
                <td className="px-4 py-3"><div className="font-semibold text-blue-400">{a.symbol}</div><div className="text-xs text-slate-500">{a.companyName}</div></td>
                <td className="px-4 py-3"><span className={`signal-${a.overallSignal.toLowerCase().replace(/\s/g, '')} px-2.5 py-1 rounded-full text-xs font-bold`}>{a.overallSignal}</span></td>
                <td className="px-4 py-3 font-mono font-bold text-white">{a.overallScore}</td>
                <td className="px-4 py-3 font-mono text-emerald-400">{a.technicalScore}</td>
                <td className="px-4 py-3 font-mono text-purple-400">{a.fundamentalScore}</td>
                <td className="px-4 py-3 font-mono text-amber-400">{a.sentimentScore}</td>
                <td className="px-4 py-3 font-mono text-pink-400">{a.dividendScore}</td>
                <td className="px-4 py-3 text-xs text-slate-400 max-w-xs truncate">{a.reasoning}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

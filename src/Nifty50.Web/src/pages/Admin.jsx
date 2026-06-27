import { useState, useEffect } from 'react';
import { getAdminHealth, getAdminApiCalls } from '../services/api';

export default function Admin() {
  const [health, setHealth] = useState(null);
  const [calls, setCalls] = useState([]);
  const [filter, setFilter] = useState('');
  const [loading, setLoading] = useState(true);

  const load = () => {
    getAdminHealth().then(r => setHealth(r.data)).catch(console.error).finally(() => setLoading(false));
    getAdminApiCalls(filter || undefined, 50).then(r => setCalls(r.data)).catch(console.error);
  };

  useEffect(() => { load(); const interval = setInterval(load, 30000); return () => clearInterval(interval); }, [filter]);

  if (loading) return <div className="flex items-center justify-center h-64"><div className="animate-spin text-4xl">⟳</div></div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <span className="text-2xl">🔧</span>
        <h3 className="text-lg font-bold text-white">API Monitor</h3>
        <span className="text-xs text-slate-500">Auto-refreshes every 30s</span>
      </div>

      {/* Server Info */}
      {health && (
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div className="glass-card p-4">
            <div className="text-xs text-slate-500">Server Started</div>
            <div className="text-sm font-mono text-white">{new Date(health.serverStartedAt).toLocaleString()}</div>
          </div>
          <div className="glass-card p-4">
            <div className="text-xs text-slate-500">Stocks in DB</div>
            <div className="text-2xl font-bold text-white">{health.totalStocksInDb}</div>
          </div>
          <div className="glass-card p-4">
            <div className="text-xs text-slate-500">Last Refresh</div>
            <div className="text-sm font-mono text-white">{health.lastRefreshAt ? new Date(health.lastRefreshAt).toLocaleString() : 'Never'}</div>
          </div>
        </div>
      )}

      {/* API Health Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {(health?.apiHealth || []).map((api, i) => {
          const successRate = api.totalCalls > 0 ? ((api.successCount / api.totalCalls) * 100).toFixed(1) : 0;
          return (
            <div key={i} className="glass-card p-5" onClick={() => setFilter(api.apiName)} style={{ cursor: 'pointer' }}>
              <div className="flex items-center justify-between mb-3">
                <h4 className="font-semibold text-white text-sm">{api.apiName}</h4>
                <span className={`px-2 py-0.5 rounded-full text-xs font-bold ${parseFloat(successRate) >= 90 ? 'bg-emerald-500/20 text-emerald-400' : 'bg-red-500/20 text-red-400'}`}>
                  {successRate}%
                </span>
              </div>
              <div className="grid grid-cols-2 gap-3 text-xs">
                <div><span className="text-slate-500">Total Calls</span><div className="font-mono font-bold text-white">{api.totalCalls}</div></div>
                <div><span className="text-slate-500">Avg Latency</span><div className="font-mono font-bold text-white">{api.averageLatencyMs?.toFixed(0)}ms</div></div>
                <div><span className="text-slate-500">Success</span><div className="font-mono text-emerald-400">{api.successCount}</div></div>
                <div><span className="text-slate-500">Errors</span><div className="font-mono text-red-400">{api.errorCount}</div></div>
              </div>
              {api.lastErrorMessage && (
                <div className="mt-3 p-2 bg-red-500/10 rounded text-xs text-red-400 truncate">{api.lastErrorMessage}</div>
              )}
            </div>
          );
        })}
      </div>

      {/* Recent API Calls */}
      <div className="glass-card p-5">
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between mb-4 gap-4">
          <h4 className="text-sm font-semibold text-slate-300">📋 Recent API Calls</h4>
          <div className="flex flex-wrap gap-2">
            {['', 'YahooFinance', 'YahooFundamentals', 'GNews'].map(f => (
              <button key={f} onClick={() => setFilter(f)}
                className={`px-3 py-1 rounded-lg text-xs ${filter === f ? 'bg-blue-500 text-white' : 'bg-slate-800/50 text-slate-400 hover:bg-slate-700'}`}>
                {f || 'All'}
              </button>
            ))}
          </div>
        </div>
        <div className="overflow-x-auto max-h-96">
          <table className="w-full text-xs">
            <thead><tr className="border-b border-slate-700/50">
              {['API', 'Endpoint', 'Status', 'Latency', 'Time', 'Error'].map(h => (
                <th key={h} className="text-left px-3 py-2 text-slate-500 uppercase tracking-wider">{h}</th>
              ))}
            </tr></thead>
            <tbody>
              {calls.map((c, i) => (
                <tr key={i} className="border-b border-slate-800/50">
                  <td className="px-3 py-2 text-blue-400">{c.apiName}</td>
                  <td className="px-3 py-2 text-slate-400 max-w-xs truncate font-mono">{c.endpoint}</td>
                  <td className="px-3 py-2">
                    <span className={`px-1.5 py-0.5 rounded ${c.statusCode < 300 ? 'bg-emerald-500/20 text-emerald-400' : 'bg-red-500/20 text-red-400'}`}>{c.statusCode}</span>
                  </td>
                  <td className="px-3 py-2 font-mono text-slate-300">{c.latencyMs}ms</td>
                  <td className="px-3 py-2 text-slate-500">{new Date(c.calledAt).toLocaleTimeString()}</td>
                  <td className="px-3 py-2 text-red-400 truncate max-w-xs">{c.errorMessage || '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

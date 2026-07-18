import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getPeers, getStock } from '../services/api';

const Compare = () => {
  const { id } = useParams();
  const [stock, setStock] = useState(null);
  const [peers, setPeers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (id) {
      fetchData();
    }
  }, [id]);

  const fetchData = async () => {
    try {
      setLoading(true);
      setError(null);
      const stockRes = await getStock(id);
      setStock(stockRes.data);
      
      try {
        const peersRes = await getPeers(id);
        setPeers(peersRes.data);
      } catch (peerErr) {
        setPeers([]);
        console.error("Failed to fetch peers:", peerErr);
      }
    } catch (err) {
      console.error(err);
      setError("Failed to load stock details. Please check if the stock exists.");
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div className="flex items-center justify-center h-64"><div className="animate-spin text-4xl">⟳</div></div>;
  if (error) return <div className="p-8 text-center text-red-400">{error}</div>;
  if (!stock) return <div className="p-8 text-center text-red-400">Stock not found.</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Link to={`/stocks/${id}`} className="text-slate-500 hover:text-blue-400 transition-colors">
          <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M10 19l-7-7m0 0l7-7m-7 7h18"></path></svg>
        </Link>
        <div>
          <h1 className="text-2xl font-bold text-white">Peer Comparison: {stock.symbol}</h1>
          <div className="text-sm text-slate-400">Sector: <span className="font-semibold text-slate-300">{stock.sector}</span></div>
        </div>
      </div>

      <div className="glass-card overflow-hidden">
        {peers.length === 0 ? (
          <div className="p-8 text-center text-slate-500">
            No peers found in the same sector.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full">
              <thead>
                <tr className="border-b border-slate-700/50">
                  <th className="px-5 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider sticky left-0 bg-[#0f172a]/95 backdrop-blur z-10">Company</th>
                  <th className="px-5 py-3 text-right text-xs font-medium text-slate-500 uppercase tracking-wider">Overall Score</th>
                  <th className="px-5 py-3 text-right text-xs font-medium text-slate-500 uppercase tracking-wider">Technical</th>
                  <th className="px-5 py-3 text-right text-xs font-medium text-slate-500 uppercase tracking-wider">Fundamental</th>
                  <th className="px-5 py-3 text-right text-xs font-medium text-slate-500 uppercase tracking-wider">Valuation</th>
                  <th className="px-5 py-3 text-right text-xs font-medium text-slate-500 uppercase tracking-wider">Quality</th>
                  <th className="px-5 py-3 text-right text-xs font-medium text-slate-500 uppercase tracking-wider">P/E Ratio</th>
                  <th className="px-5 py-3 text-right text-xs font-medium text-slate-500 uppercase tracking-wider">ROE (%)</th>
                  <th className="px-5 py-3 text-right text-xs font-medium text-slate-500 uppercase tracking-wider">D/E Ratio</th>
                  <th className="px-5 py-3 text-right text-xs font-medium text-slate-500 uppercase tracking-wider">Mkt Cap (Cr)</th>
                </tr>
              </thead>
              <tbody>
                {peers.map(p => (
                  <tr key={p.stockId} className={`border-b border-slate-800/50 hover:bg-slate-800/30 transition-colors ${String(p.stockId) === id ? 'bg-blue-500/10' : ''}`}>
                    <td className={`px-5 py-4 whitespace-nowrap sticky left-0 z-10 ${String(p.stockId) === id ? 'bg-blue-500/10' : 'bg-[#0f172a]/95'} backdrop-blur`}>
                      <Link to={`/stocks/${p.stockId}`} className={`font-semibold ${String(p.stockId) === id ? 'text-blue-400' : 'text-white'} hover:underline`}>
                        {p.symbol}
                      </Link>
                    </td>
                    <td className="px-5 py-4 whitespace-nowrap text-right font-bold font-mono text-white">{p.overallScore}</td>
                    <td className="px-5 py-4 whitespace-nowrap text-right text-sm text-slate-400 font-mono">{p.technicalScore}</td>
                    <td className="px-5 py-4 whitespace-nowrap text-right text-sm text-slate-400 font-mono">{p.fundamentalScore}</td>
                    <td className="px-5 py-4 whitespace-nowrap text-right text-sm text-slate-400 font-mono">{p.valuationScore}</td>
                    <td className="px-5 py-4 whitespace-nowrap text-right text-sm text-slate-400 font-mono">{p.qualityScore}</td>
                    <td className="px-5 py-4 whitespace-nowrap text-right text-sm text-slate-400 font-mono">{p.pe ? p.pe.toFixed(1) : '—'}</td>
                    <td className="px-5 py-4 whitespace-nowrap text-right text-sm text-slate-400 font-mono">{p.roe ? p.roe.toFixed(1) : '—'}</td>
                    <td className="px-5 py-4 whitespace-nowrap text-right text-sm text-slate-400 font-mono">{p.debtToEquity ? p.debtToEquity.toFixed(2) : '—'}</td>
                    <td className="px-5 py-4 whitespace-nowrap text-right text-sm text-slate-400 font-mono">{p.marketCap ? (p.marketCap / 10000000).toFixed(0) : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default Compare;

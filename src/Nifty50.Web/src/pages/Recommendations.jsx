import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getRecommendationsDashboard } from '../services/api';

const Recommendations = () => {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchDashboard();
  }, []);

  const fetchDashboard = async () => {
    try {
      const res = await getRecommendationsDashboard();
      setData(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const renderStockRow = (stock) => (
    <div key={stock.stockId} className="flex justify-between items-center p-3 rounded-lg bg-slate-800/50 hover:bg-slate-700/50 transition-colors">
      <div>
        <Link to={`/stocks/${stock.stockId}`} className="font-semibold text-blue-400 hover:underline">{stock.symbol}</Link>
        <div className="text-xs text-slate-500">{stock.companyName}</div>
      </div>
      <div className="text-right">
        <div className={`font-bold ${stock.overallScore >= 70 ? 'text-emerald-400' : stock.overallScore <= 30 ? 'text-red-400' : 'text-amber-400'}`}>
          {stock.overallScore}/100
        </div>
        <div className="text-xs text-slate-500">{stock.overallSignal}</div>
      </div>
    </div>
  );

  if (loading) return <div className="flex items-center justify-center h-64"><div className="animate-spin text-4xl">⟳</div></div>;
  if (!data) return <div className="p-8 text-center text-red-400">Failed to load recommendations.</div>;

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold text-white">Investment Recommendations</h1>
        <div className="flex gap-3">
          <div className="bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 px-4 py-2 rounded-lg text-sm font-semibold">
            Bullish: {data.bullishCount}
          </div>
          <div className="bg-red-500/10 border border-red-500/20 text-red-400 px-4 py-2 rounded-lg text-sm font-semibold">
            Bearish: {data.bearishCount}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {/* Top Bullish */}
        <div className="glass-card p-5">
          <h2 className="text-sm font-semibold text-emerald-400 mb-4 flex items-center gap-2">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6"></path></svg>
            Top Bullish Opportunities
          </h2>
          <div className="space-y-2">
            {data.topBullish?.length > 0 ? data.topBullish.map(renderStockRow) : <div className="text-sm text-slate-500">No bullish signals</div>}
          </div>
        </div>

        {/* Value Opportunities */}
        <div className="glass-card p-5">
          <h2 className="text-sm font-semibold text-blue-400 mb-1 flex items-center gap-2">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
            Deep Value Picks
          </h2>
          <div className="text-xs text-slate-500 mb-4">High Quality &amp; Undervalued</div>
          <div className="space-y-2">
            {data.valueOpportunities?.length > 0 ? data.valueOpportunities.map(renderStockRow) : <div className="text-sm text-slate-500">No value opportunities currently</div>}
          </div>
        </div>

        {/* Top Bearish */}
        <div className="glass-card p-5">
          <h2 className="text-sm font-semibold text-red-400 mb-4 flex items-center gap-2">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M13 17h8m0 0V9m0 8l-8-8-4 4-6-6"></path></svg>
            Top Bearish Signals
          </h2>
          <div className="space-y-2">
            {data.bottomBearish?.length > 0 ? data.bottomBearish.map(renderStockRow) : <div className="text-sm text-slate-500">No bearish signals</div>}
          </div>
        </div>
      </div>

      <div className="glass-card p-5">
        <h2 className="text-sm font-semibold text-slate-300 mb-4">📊 Sector Heatmap</h2>
        <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-5 gap-3">
          {data.sectorAverages && data.sectorAverages.map(sector => (
            <div key={sector.sector} className={`p-4 rounded-lg text-center ${sector.averageChangePercent > 0 ? 'bg-emerald-500/10 border border-emerald-500/20' : sector.averageChangePercent < 0 ? 'bg-red-500/10 border border-red-500/20' : 'bg-slate-800/50 border border-slate-700/50'}`}>
              <div className="text-xs text-slate-400 font-semibold truncate" title={sector.sector}>{sector.sector}</div>
              <div className={`text-lg font-bold ${sector.averageChangePercent > 0 ? 'text-emerald-400' : sector.averageChangePercent < 0 ? 'text-red-400' : 'text-slate-400'}`}>
                {sector.averageChangePercent > 0 ? '+' : ''}{sector.averageChangePercent?.toFixed(2) ?? '0.00'}%
              </div>
              <div className="text-xs text-slate-500 mt-1">{sector.stockCount} stocks</div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default Recommendations;

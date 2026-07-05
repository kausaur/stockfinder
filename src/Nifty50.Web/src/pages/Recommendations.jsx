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
    <div key={stock.stockId} className="flex justify-between items-center p-3 border-b border-gray-100 hover:bg-gray-50 transition-colors">
      <div>
        <Link to={`/stocks/${stock.stockId}`} className="font-semibold text-primary hover:underline">{stock.symbol}</Link>
        <div className="text-xs text-gray-500">{stock.companyName}</div>
      </div>
      <div className="text-right">
        <div className={`font-bold ${stock.overallScore >= 70 ? 'text-green-600' : stock.overallScore <= 30 ? 'text-red-600' : 'text-yellow-600'}`}>
          {stock.overallScore}/100
        </div>
        <div className="text-xs text-gray-500">{stock.overallSignal}</div>
      </div>
    </div>
  );

  if (loading) return <div className="p-8 text-center"><div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto"></div></div>;
  if (!data) return <div className="p-8 text-center text-red-500">Failed to load recommendations.</div>;

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      <div className="flex justify-between items-center mb-4">
        <h1 className="text-3xl font-bold">Investment Recommendations</h1>
        <div className="flex gap-4">
          <div className="bg-green-100 text-green-800 px-4 py-2 rounded-lg text-sm font-semibold">
            Bullish Signals: {data.bullishCount}
          </div>
          <div className="bg-red-100 text-red-800 px-4 py-2 rounded-lg text-sm font-semibold">
            Bearish Signals: {data.bearishCount}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {/* Top Bullish */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-4">
          <h2 className="text-xl font-bold mb-4 text-green-700 flex items-center gap-2">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6"></path></svg>
            Top Bullish Opportunities
          </h2>
          <div className="space-y-1">
            {data.topBullish?.length > 0 ? data.topBullish.map(renderStockRow) : <div className="text-sm text-gray-500">No bullish signals</div>}
          </div>
        </div>

        {/* Value Opportunities */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-4">
          <h2 className="text-xl font-bold mb-4 text-blue-700 flex items-center gap-2">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
            Deep Value Picks
          </h2>
          <div className="text-xs text-gray-500 mb-2">High Quality & Undervalued</div>
          <div className="space-y-1">
            {data.valueOpportunities?.length > 0 ? data.valueOpportunities.map(renderStockRow) : <div className="text-sm text-gray-500">No value opportunities currently</div>}
          </div>
        </div>

        {/* Top Bearish */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-4">
          <h2 className="text-xl font-bold mb-4 text-red-700 flex items-center gap-2">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M13 17h8m0 0V9m0 8l-8-8-4 4-6-6"></path></svg>
            Top Bearish Signals
          </h2>
          <div className="space-y-1">
            {data.bottomBearish?.length > 0 ? data.bottomBearish.map(renderStockRow) : <div className="text-sm text-gray-500">No bearish signals</div>}
          </div>
        </div>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6 mt-6">
        <h2 className="text-xl font-bold mb-4">Sector Heatmap</h2>
        <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-5 gap-4">
          {data.sectorAverages && data.sectorAverages.map(sector => (
            <div key={sector.sector} className={`p-4 rounded-lg text-center ${sector.averageChangePercent > 0 ? 'bg-green-50 border border-green-100' : sector.averageChangePercent < 0 ? 'bg-red-50 border border-red-100' : 'bg-gray-50 border border-gray-200'}`}>
              <div className="text-sm font-semibold truncate" title={sector.sector}>{sector.sector}</div>
              <div className={`text-lg font-bold ${sector.averageChangePercent > 0 ? 'text-green-700' : sector.averageChangePercent < 0 ? 'text-red-700' : 'text-gray-600'}`}>
                {sector.averageChangePercent > 0 ? '+' : ''}{sector.averageChangePercent?.toFixed(2) ?? '0.00'}%
              </div>
              <div className="text-xs text-gray-500 mt-1">{sector.stockCount} stocks</div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default Recommendations;

import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Loader2 } from 'lucide-react';
import { screenStocks } from '../services/api';

const Screener = () => {
  const [results, setResults] = useState([]);
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState({
    MinScore: 50,
    MaxScore: 100,
    MaxPE: 30,
    MinROE: 15,
    MaxDebtToEquity: 1,
    MinDividendYield: 0,
    MinPiotroskiScore: 5,
    ValuationVerdict: 'Any',
    SortBy: 'Score'
  });

  useEffect(() => {
    handleSearch();
  }, []);

  const handleSearch = async (e) => {
    if (e) e.preventDefault();
    setLoading(true);
    try {
      // Clean up filters before sending
      const payload = { ...filters };
      if (payload.MaxPE === undefined || payload.MaxPE === '') delete payload.MaxPE;
      if (payload.MinROE === undefined || payload.MinROE === '') delete payload.MinROE;
      if (payload.MaxDebtToEquity === undefined || payload.MaxDebtToEquity === '') delete payload.MaxDebtToEquity;
      if (payload.MinDividendYield === undefined || payload.MinDividendYield === '') delete payload.MinDividendYield;
      if (payload.MinPiotroskiScore === undefined || payload.MinPiotroskiScore === '') delete payload.MinPiotroskiScore;
      
      const res = await screenStocks(payload);
      setResults(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleFilterChange = (e) => {
    const { name, value } = e.target;
    setFilters(prev => ({
      ...prev,
      [name]: value === '' ? '' : (name === 'ValuationVerdict' || name === 'SortBy' ? value : Number(value))
    }));
  };

  const inputClass = "w-full bg-slate-800/50 border border-slate-700/50 rounded-lg px-3 py-2 text-white text-sm focus:border-blue-500 focus:ring-1 focus:ring-blue-500 focus:outline-none";
  const selectClass = "w-full bg-slate-800/50 border border-slate-700/50 rounded-lg px-3 py-2 text-white text-sm focus:border-blue-500 focus:ring-1 focus:ring-blue-500 focus:outline-none appearance-none";

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-white">Stock Screener</h1>
      
      <div className="glass-card p-5">
        <form onSubmit={handleSearch} className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-4 gap-4">
          
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Min Overall Score</label>
            <input type="number" name="MinScore" value={filters.MinScore} onChange={handleFilterChange} className={inputClass} />
          </div>
          
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Max P/E Ratio</label>
            <input type="number" name="MaxPE" value={filters.MaxPE} onChange={handleFilterChange} className={inputClass} />
          </div>
          
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Min ROE (%)</label>
            <input type="number" name="MinROE" value={filters.MinROE} onChange={handleFilterChange} className={inputClass} />
          </div>
          
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Max Debt/Equity</label>
            <input type="number" step="0.1" name="MaxDebtToEquity" value={filters.MaxDebtToEquity} onChange={handleFilterChange} className={inputClass} />
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Min Dividend Yield (%)</label>
            <input type="number" step="0.1" name="MinDividendYield" value={filters.MinDividendYield} onChange={handleFilterChange} className={inputClass} />
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Min Piotroski F-Score (0-9)</label>
            <input type="number" min="0" max="9" name="MinPiotroskiScore" value={filters.MinPiotroskiScore} onChange={handleFilterChange} className={inputClass} />
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Valuation Verdict</label>
            <select name="ValuationVerdict" value={filters.ValuationVerdict} onChange={handleFilterChange} className={selectClass}>
              <option value="Any">Any</option>
              <option value="Significantly Undervalued">Significantly Undervalued</option>
              <option value="Moderately Undervalued">Moderately Undervalued</option>
              <option value="Fairly Valued">Fairly Valued</option>
            </select>
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Sort By</label>
            <select name="SortBy" value={filters.SortBy} onChange={handleFilterChange} className={selectClass}>
              <option value="Score">Overall Score (Desc)</option>
              <option value="PE">P/E Ratio (Asc)</option>
              <option value="ROE">ROE (Desc)</option>
            </select>
          </div>

          <div className="md:col-span-3 lg:col-span-4 flex justify-end mt-2">
            <button type="submit" disabled={loading} className="bg-blue-600 hover:bg-blue-700 text-white font-bold py-2.5 px-8 rounded-lg transition-colors disabled:opacity-50">
              {loading ? 'Screening...' : 'Screen Stocks'}
            </button>
          </div>
        </form>
      </div>

      <div className="glass-card overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-700/50 flex justify-between items-center">
          <h2 className="text-sm font-semibold text-slate-300">Results ({results.length})</h2>
        </div>
        
        {loading && results.length === 0 ? (
          <div className="flex items-center justify-center h-32"><Loader2 className="animate-spin text-slate-400" size={48} /></div>
        ) : results.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="min-w-full">
              <thead>
                <tr className="border-b border-slate-700/50">
                  <th className="px-5 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Stock</th>
                  <th className="px-5 py-3 text-right text-xs font-medium text-slate-500 uppercase tracking-wider">Score</th>
                  <th className="px-5 py-3 text-right text-xs font-medium text-slate-500 uppercase tracking-wider">P/E</th>
                  <th className="px-5 py-3 text-right text-xs font-medium text-slate-500 uppercase tracking-wider">ROE</th>
                  <th className="px-5 py-3 text-right text-xs font-medium text-slate-500 uppercase tracking-wider">Signal</th>
                  <th className="px-5 py-3 text-right text-xs font-medium text-slate-500 uppercase tracking-wider">Valuation</th>
                </tr>
              </thead>
              <tbody>
                {results.map(stock => (
                  <tr key={stock.stockId} className="border-b border-slate-800/50 hover:bg-slate-800/30 transition-colors">
                    <td className="px-5 py-4 whitespace-nowrap">
                      <Link to={`/stocks/${stock.stockId}`} className="text-sm font-medium text-blue-400 hover:underline">{stock.symbol}</Link>
                      <div className="text-xs text-slate-500">{stock.companyName}</div>
                    </td>
                    <td className="px-5 py-4 whitespace-nowrap text-right">
                      <div className={`text-sm font-bold font-mono ${stock.overallScore >= 70 ? 'text-emerald-400' : stock.overallScore <= 30 ? 'text-red-400' : 'text-white'}`}>
                        {stock.overallScore}
                      </div>
                    </td>
                    <td className="px-5 py-4 whitespace-nowrap text-right text-sm text-slate-400 font-mono">
                      {stock.pe ? stock.pe.toFixed(1) : '—'}
                    </td>
                    <td className="px-5 py-4 whitespace-nowrap text-right text-sm text-slate-400 font-mono">
                      {stock.roe ? `${stock.roe.toFixed(1)}%` : '—'}
                    </td>
                    <td className="px-5 py-4 whitespace-nowrap text-right text-sm">
                      <span className={`px-2.5 py-1 rounded-full text-xs font-bold ${stock.overallSignal?.includes('Bull') ? 'bg-emerald-500/15 text-emerald-400' : stock.overallSignal?.includes('Bear') ? 'bg-red-500/15 text-red-400' : 'bg-slate-700/50 text-slate-400'}`}>
                        {stock.overallSignal}
                      </span>
                    </td>
                    <td className="px-5 py-4 whitespace-nowrap text-right text-sm text-slate-400">
                      {stock.valuationVerdict || '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="p-8 text-center text-slate-500">No stocks match your filter criteria.</div>
        )}
      </div>
    </div>
  );
};

export default Screener;

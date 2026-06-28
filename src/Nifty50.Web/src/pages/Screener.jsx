import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
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
      if (!payload.MaxPE) delete payload.MaxPE;
      if (!payload.MinROE) delete payload.MinROE;
      if (!payload.MaxDebtToEquity) delete payload.MaxDebtToEquity;
      if (!payload.MinDividendYield) delete payload.MinDividendYield;
      if (!payload.MinPiotroskiScore) delete payload.MinPiotroskiScore;
      
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

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      <h1 className="text-3xl font-bold">Stock Screener</h1>
      
      <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
        <form onSubmit={handleSearch} className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-4 gap-4">
          
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Min Overall Score</label>
            <input type="number" name="MinScore" value={filters.MinScore} onChange={handleFilterChange} className="w-full border-gray-300 rounded-md shadow-sm focus:border-primary focus:ring-primary" />
          </div>
          
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Max P/E Ratio</label>
            <input type="number" name="MaxPE" value={filters.MaxPE} onChange={handleFilterChange} className="w-full border-gray-300 rounded-md shadow-sm focus:border-primary focus:ring-primary" />
          </div>
          
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Min ROE (%)</label>
            <input type="number" name="MinROE" value={filters.MinROE} onChange={handleFilterChange} className="w-full border-gray-300 rounded-md shadow-sm focus:border-primary focus:ring-primary" />
          </div>
          
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Max Debt/Equity</label>
            <input type="number" step="0.1" name="MaxDebtToEquity" value={filters.MaxDebtToEquity} onChange={handleFilterChange} className="w-full border-gray-300 rounded-md shadow-sm focus:border-primary focus:ring-primary" />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Min Dividend Yield (%)</label>
            <input type="number" step="0.1" name="MinDividendYield" value={filters.MinDividendYield} onChange={handleFilterChange} className="w-full border-gray-300 rounded-md shadow-sm focus:border-primary focus:ring-primary" />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Min Piotroski F-Score (0-9)</label>
            <input type="number" min="0" max="9" name="MinPiotroskiScore" value={filters.MinPiotroskiScore} onChange={handleFilterChange} className="w-full border-gray-300 rounded-md shadow-sm focus:border-primary focus:ring-primary" />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Valuation Verdict</label>
            <select name="ValuationVerdict" value={filters.ValuationVerdict} onChange={handleFilterChange} className="w-full border-gray-300 rounded-md shadow-sm focus:border-primary focus:ring-primary">
              <option value="Any">Any</option>
              <option value="Significantly Undervalued">Significantly Undervalued</option>
              <option value="Moderately Undervalued">Moderately Undervalued</option>
              <option value="Fairly Valued">Fairly Valued</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Sort By</label>
            <select name="SortBy" value={filters.SortBy} onChange={handleFilterChange} className="w-full border-gray-300 rounded-md shadow-sm focus:border-primary focus:ring-primary">
              <option value="Score">Overall Score (Desc)</option>
              <option value="PE">P/E Ratio (Asc)</option>
              <option value="ROE">ROE (Desc)</option>
            </select>
          </div>

          <div className="md:col-span-3 lg:col-span-4 flex justify-end mt-2">
            <button type="submit" disabled={loading} className="bg-primary hover:bg-blue-700 text-white font-bold py-2 px-6 rounded-lg transition-colors shadow-sm disabled:opacity-50">
              {loading ? 'Screening...' : 'Screen Stocks'}
            </button>
          </div>
        </form>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="p-4 bg-gray-50 border-b border-gray-100 flex justify-between items-center">
          <h2 className="font-bold text-gray-700">Results ({results.length})</h2>
        </div>
        
        {loading && results.length === 0 ? (
          <div className="p-8 text-center"><div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto"></div></div>
        ) : results.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Stock</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Score</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">P/E</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">ROE</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Signal</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Valuation</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {results.map(stock => (
                  <tr key={stock.stockId} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center">
                        <div>
                          <Link to={`/stock/${stock.stockId}`} className="text-sm font-medium text-primary hover:underline">{stock.symbol}</Link>
                          <div className="text-xs text-gray-500">{stock.companyName}</div>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right">
                      <div className={`text-sm font-bold ${stock.overallScore >= 70 ? 'text-green-600' : stock.overallScore <= 30 ? 'text-red-600' : 'text-gray-900'}`}>
                        {stock.overallScore}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm text-gray-500">
                      {stock.pe ? stock.pe.toFixed(1) : '-'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm text-gray-500">
                      {stock.roe ? `${stock.roe.toFixed(1)}%` : '-'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                      <span className={`px-2 py-1 rounded-full text-xs ${stock.overallSignal.includes('Buy') ? 'bg-green-100 text-green-800' : stock.overallSignal.includes('Sell') ? 'bg-red-100 text-red-800' : 'bg-gray-100 text-gray-800'}`}>
                        {stock.overallSignal}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm text-gray-500">
                      {stock.valuationVerdict || '-'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="p-8 text-center text-gray-500">No stocks match your filter criteria.</div>
        )}
      </div>
    </div>
  );
};

export default Screener;

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

  if (loading) return <div className="p-8 text-center"><div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto"></div></div>;
  if (error) return <div className="p-8 text-center text-red-500">{error}</div>;
  if (!stock) return <div className="p-8 text-center text-red-500">Stock not found.</div>;

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      <div className="flex items-center gap-4 mb-2">
        <Link to={`/stocks/${id}`} className="text-gray-500 hover:text-primary">
          <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M10 19l-7-7m0 0l7-7m-7 7h18"></path></svg>
        </Link>
        <h1 className="text-3xl font-bold">Peer Comparison: {stock.symbol}</h1>
      </div>
      <div className="text-gray-600">Sector: <span className="font-semibold text-gray-800">{stock.sector}</span></div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        {peers.length === 0 ? (
          <div className="p-8 text-center text-gray-500">
            No peers found in the same sector.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider sticky left-0 bg-gray-50 z-10">Company</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Overall Score</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Technical</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Fundamental</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Valuation</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Quality</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">P/E Ratio</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">ROE (%)</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">D/E Ratio</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Mkt Cap (Cr)</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {peers.map(p => (
                  <tr key={p.stockId} className={`hover:bg-gray-50 ${String(p.stockId) === id ? 'bg-blue-50' : ''}`}>
                    <td className="px-6 py-4 whitespace-nowrap sticky left-0 z-10 bg-inherit">
                      <Link to={`/stocks/${p.stockId}`} className={`font-semibold ${String(p.stockId) === id ? 'text-primary' : 'text-gray-900'} hover:underline`}>
                        {p.symbol}
                      </Link>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right font-bold text-gray-900">{p.overallScore}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-gray-500">{p.technicalScore}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-gray-500">{p.fundamentalScore}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-gray-500">{p.valuationScore}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-gray-500">{p.qualityScore}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-gray-500">{p.pe ? p.pe.toFixed(1) : '-'}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-gray-500">{p.roe ? p.roe.toFixed(1) : '-'}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-gray-500">{p.debtToEquity ? p.debtToEquity.toFixed(2) : '-'}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-gray-500">{p.marketCap ? (p.marketCap / 10000000).toFixed(0) : '-'}</td>
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

import { useState, useEffect, useRef } from 'react';
import { useParams } from 'react-router-dom';
import { getStock, getStockPrices, getTechnicals, getFundamentals, getSentiment, getAnalysis, getDividends, getActiveProfile } from '../services/api';
import { createChart, CandlestickSeries, HistogramSeries } from 'lightweight-charts';
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, LineChart, Line, PieChart, Pie, Cell } from 'recharts';

const SignalBadge = ({ signal, large }) => {
  const cls = `signal-${(signal || 'hold').toLowerCase().replace(/\s/g, '')}`;
  return <span className={`${cls} ${large ? 'px-5 py-2 text-lg' : 'px-3 py-1 text-xs'} rounded-full font-bold inline-block`}>{signal || 'N/A'}</span>;
};

const ScoreGauge = ({ score, label, color }) => {
  const offset = 283 - (283 * (score || 0)) / 100;
  return (
    <div className="flex flex-col items-center">
      <svg width="80" height="80" viewBox="0 0 100 100"><circle cx="50" cy="50" r="45" fill="none" stroke="#334155" strokeWidth="8" />
        <circle cx="50" cy="50" r="45" fill="none" stroke={color} strokeWidth="8" className="score-circle" style={{ strokeDashoffset: offset }} transform="rotate(-90 50 50)" />
        <text x="50" y="55" textAnchor="middle" fill="white" fontSize="22" fontWeight="bold">{score ?? '—'}</text></svg>
      <span className="text-xs text-slate-400 mt-1">{label}</span>
    </div>
  );
};

const ranges = [
  { label: '1W', days: 7 }, { label: '1M', days: 30 }, { label: '3M', days: 90 },
  { label: '6M', days: 180 }, { label: '1Y', days: 365 }, { label: '2Y', days: 730 },
  { label: '5Y', days: 1825 }, { label: '8Y', days: 2920 },
];

export default function StockDetail() {
  const { id } = useParams();
  const chartRef = useRef(null);
  const chartInstance = useRef(null);
  const [stock, setStock] = useState(null);
  const [analysis, setAnalysis] = useState(null);
  const [technicals, setTechnicals] = useState(null);
  const [fundamentals, setFundamentals] = useState(null);
  const [sentiment, setSentiment] = useState(null);
  const [profile, setProfile] = useState(null);
  const [range, setRange] = useState('1Y');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([
      getStock(id), getAnalysis(id).catch(() => ({ data: null })),
      getTechnicals(id).catch(() => ({ data: null })), getFundamentals(id).catch(() => ({ data: null })),
      getSentiment(id).catch(() => ({ data: null })), getActiveProfile().catch(() => ({ data: null })),
    ]).then(([s, a, t, f, se, p]) => {
      setStock(s.data); setAnalysis(a.data); setTechnicals(t.data); setFundamentals(f.data); setSentiment(se.data); setProfile(p.data);
    }).catch(console.error).finally(() => setLoading(false));
  }, [id]);

  useEffect(() => {
    if (!chartRef.current || loading) return;
    if (chartInstance.current) { chartInstance.current.remove(); chartInstance.current = null; }

    const chart = createChart(chartRef.current, {
      width: chartRef.current.clientWidth, height: 400,
      layout: { background: { color: 'transparent' }, textColor: '#94a3b8', attributionLogo: false },
      grid: { vertLines: { color: '#1e293b' }, horzLines: { color: '#1e293b' } },
      crosshair: { mode: 0 }, timeScale: { borderColor: '#334155' },
    });
    chartInstance.current = chart;
    const candleSeries = chart.addSeries(CandlestickSeries, {
      upColor: '#10b981', downColor: '#ef4444', borderUpColor: '#10b981', borderDownColor: '#ef4444',
      wickUpColor: '#10b981', wickDownColor: '#ef4444',
    });
    const volumeSeries = chart.addSeries(HistogramSeries, { priceFormat: { type: 'volume' }, priceScaleId: 'vol' });
    chart.priceScale('vol').applyOptions({ scaleMargins: { top: 0.85, bottom: 0 } });

    const days = ranges.find(r => r.label === range)?.days || 365;
    const from = new Date(Date.now() - days * 86400000).toISOString();
    getStockPrices(id, from).then(r => {
      const uniquePrices = new Map();
      r.data.forEach(p => {
        const time = p.date.split('T')[0];
        uniquePrices.set(time, { time, open: p.open, high: p.high, low: p.low, close: p.close, volume: p.volume });
      });
      const sortedUnique = Array.from(uniquePrices.values()).sort((a, b) => a.time.localeCompare(b.time));

      const prices = sortedUnique.map(p => ({ time: p.time, open: p.open, high: p.high, low: p.low, close: p.close }));
      const volumes = sortedUnique.map(p => ({ time: p.time, value: p.volume, color: p.close >= p.open ? '#10b98133' : '#ef444433' }));
      
      candleSeries.setData(prices);
      volumeSeries.setData(volumes);
      chart.timeScale().fitContent();
    }).catch(console.error);

    const handleResize = () => chart.applyOptions({ width: chartRef.current?.clientWidth || 600 });
    window.addEventListener('resize', handleResize);
    return () => { window.removeEventListener('resize', handleResize); chart.remove(); chartInstance.current = null; };
  }, [id, range, loading]);

  if (loading) return <div className="flex items-center justify-center h-64"><div className="animate-spin text-4xl">⟳</div></div>;
  if (!stock) return <p className="text-slate-400">Stock not found.</p>;

  const headlines = sentiment?.topHeadlines || [];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="glass-card p-6 flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-white">{stock.companyName}</h2>
          <div className="flex items-center gap-3 mt-1">
            <span className="text-blue-400 font-mono font-bold">{stock.symbol}</span>
            {stock.sector && <span className="px-2 py-0.5 bg-slate-700/50 rounded text-xs text-slate-400">{stock.sector}</span>}
          </div>
        </div>
        <div className="text-right">
          <div className="text-3xl font-bold text-white font-mono">₹{stock.currentPrice?.toFixed(2) || '—'}</div>
          <div className={`text-sm font-bold ${(stock.dayChangePercent || 0) >= 0 ? 'text-emerald-400' : 'text-red-400'}`}>
            {(stock.dayChangePercent || 0) >= 0 ? '▲' : '▼'} {stock.dayChange?.toFixed(2)} ({(stock.dayChangePercent || 0).toFixed(2)}%)
          </div>
        </div>
      </div>

      {/* Chart */}
      <div className="glass-card p-5">
        <div className="flex items-center gap-2 mb-4">
          {ranges.map(r => (
            <button key={r.label} onClick={() => setRange(r.label)}
              className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-all ${range === r.label ? 'bg-blue-500 text-white' : 'bg-slate-800/50 text-slate-400 hover:bg-slate-700'}`}>
              {r.label}
            </button>
          ))}
        </div>
        <div ref={chartRef} className="w-full" />
      </div>

      {/* Analysis Verdict */}
      {analysis && (
        <div className="glass-card p-6">
          <div className="flex items-center gap-3 mb-4">
            <h3 className="text-lg font-bold text-white">Analysis Verdict</h3>
            {profile && <span className="text-xs px-2 py-1 bg-blue-500/20 text-blue-400 rounded-full">Using: {profile.name}</span>}
          </div>
          <div className="grid grid-cols-1 md:grid-cols-6 gap-6 items-center">
            <div className="col-span-1 text-center"><SignalBadge signal={analysis.overallSignal} large /></div>
            <div className="col-span-1"><ScoreGauge score={analysis.overallScore} label="Overall" color="#3b82f6" /></div>
            <div className="col-span-1 md:col-span-4 grid grid-cols-2 sm:grid-cols-6 gap-3">
              <ScoreGauge score={analysis.technicalScore} label="Technical" color="#10b981" />
              <ScoreGauge score={analysis.fundamentalScore} label="Fundamental" color="#8b5cf6" />
              <ScoreGauge score={analysis.sentimentScore} label="Sentiment" color="#f59e0b" />
              <ScoreGauge score={analysis.dividendScore} label="Dividend" color="#ec4899" />
              <ScoreGauge score={analysis.valuationScore} label="Valuation" color="#0ea5e9" />
              <ScoreGauge score={analysis.qualityScore} label="Quality" color="#14b8a6" />
            </div>
          </div>
          {analysis.reasoning && <p className="text-sm text-slate-400 mt-4 p-3 bg-slate-800/30 rounded-lg">{analysis.reasoning}</p>}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Technical Panel */}
        <div className="glass-card p-5">
          <h3 className="text-sm font-semibold text-slate-300 mb-4">📉 Technical Indicators</h3>
          {technicals ? (
            <div className="grid grid-cols-2 gap-3">
              {[
                { label: 'RSI (14)', value: technicals.rsI14?.toFixed(1), warn: technicals.rsI14 > 70 || technicals.rsI14 < 30 },
                { label: 'MACD', value: technicals.macd?.toFixed(2) },
                { label: 'SMA 50', value: technicals.smA50?.toFixed(2) },
                { label: 'SMA 200', value: technicals.smA200?.toFixed(2) },
                { label: 'ADX (14)', value: technicals.adX14?.toFixed(1) },
                { label: 'ATR (14)', value: technicals.atR14?.toFixed(2) },
              ].map((ind, i) => (
                <div key={i} className={`p-3 rounded-lg ${ind.warn ? 'bg-amber-500/10 border border-amber-500/20' : 'bg-slate-800/30'}`}>
                  <div className="text-xs text-slate-500">{ind.label}</div>
                  <div className="text-lg font-mono font-bold text-white">{ind.value || '—'}</div>
                </div>
              ))}
            </div>
          ) : <p className="text-slate-500 text-sm">No technical data available.</p>}
        </div>

        {/* Fundamental Panel */}
        <div className="glass-card p-5">
          <h3 className="text-sm font-semibold text-slate-300 mb-4">📊 Fundamental Ratios</h3>
          {fundamentals ? (
            <div className="grid grid-cols-2 gap-3">
              {[
                { label: 'P/E Ratio', value: fundamentals.peRatio?.toFixed(1) },
                { label: 'ROE', value: fundamentals.roe ? `${fundamentals.roe.toFixed(1)}%` : null },
                { label: 'D/E Ratio', value: fundamentals.debtToEquity?.toFixed(2) },
                { label: 'Current Ratio', value: fundamentals.currentRatio?.toFixed(2) },
                { label: 'EPS', value: fundamentals.eps ? `₹${fundamentals.eps.toFixed(2)}` : null },
                { label: 'Revenue Growth', value: fundamentals.revenueGrowthYoY ? `${fundamentals.revenueGrowthYoY.toFixed(1)}%` : null },
              ].map((r, i) => (
                <div key={i} className="p-3 rounded-lg bg-slate-800/30">
                  <div className="text-xs text-slate-500">{r.label}</div>
                  <div className="text-lg font-mono font-bold text-white">{r.value || '—'}</div>
                </div>
              ))}
            </div>
          ) : <p className="text-slate-500 text-sm">No fundamental data available.</p>}
        </div>
      </div>

      {/* Sentiment Panel */}
      <div className="glass-card p-5">
        <h3 className="text-sm font-semibold text-slate-300 mb-4">🗞️ Sentiment Analysis</h3>
        {sentiment ? (
          <div>
            <div className="flex flex-col md:flex-row items-start md:items-center gap-4 md:gap-6 mb-4">
              <div className={`text-2xl md:text-3xl font-bold ${(sentiment.sentimentScore || 0) > 0.1 ? 'text-emerald-400' : (sentiment.sentimentScore || 0) < -0.1 ? 'text-red-400' : 'text-slate-400'}`}>
                {(sentiment.sentimentScore || 0) > 0.1 ? '😊' : (sentiment.sentimentScore || 0) < -0.1 ? '😟' : '😐'} {sentiment.overallSentiment}
              </div>
              <div className="text-sm text-slate-400">Score: <span className="font-mono font-bold text-white">{sentiment.sentimentScore?.toFixed(2) || 'N/A'}</span></div>
              <div className="flex flex-wrap gap-3 text-xs">
                <span className="text-emerald-400">✅ {sentiment.positiveCount} positive</span>
                <span className="text-red-400">❌ {sentiment.negativeCount} negative</span>
                <span className="text-slate-500">• {sentiment.neutralCount} neutral</span>
              </div>
            </div>
            <div className="space-y-2">
              {headlines.map((h, i) => (
                <div key={i} className="p-3 bg-slate-800/30 rounded-lg text-sm text-slate-300">{h}</div>
              ))}
            </div>
          </div>
        ) : <p className="text-slate-500 text-sm">No sentiment data available.</p>}
      </div>
    </div>
  );
}

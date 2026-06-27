import { useState, useEffect } from 'react';
import { getScoringProfiles, getActiveProfile, updateActiveProfile, activateProfile, resetProfile, recalculateAnalyses } from '../services/api';
import { PieChart, Pie, Cell, ResponsiveContainer } from 'recharts';

const presetEmoji = { Balanced: '🟢', Growth: '🚀', Value: '💎', Income: '💰', Momentum: '⚡', Quality: '🏆' };
const COLORS = ['#3b82f6', '#8b5cf6', '#f59e0b', '#ec4899'];

export default function Settings() {
  const [profiles, setProfiles] = useState([]);
  const [active, setActive] = useState(null);
  const [weights, setWeights] = useState({});
  const [saving, setSaving] = useState(false);
  const [recalculating, setRecalculating] = useState(false);

  const load = () => {
    Promise.all([getScoringProfiles(), getActiveProfile()])
      .then(([p, a]) => { setProfiles(p.data); setActive(a.data); setWeights(a.data); })
      .catch(console.error);
  };
  useEffect(load, []);

  const handleSlider = (key, value) => {
    const val = parseInt(value);
    const others = ['technicalWeight', 'fundamentalWeight', 'sentimentWeight', 'dividendWeight'].filter(k => k !== key);
    const remaining = 100 - val;
    const currentOthers = others.reduce((sum, k) => sum + (weights[k] || 0), 0);
    const newWeights = { ...weights, [key]: val };
    if (currentOthers > 0) {
      others.forEach(k => { newWeights[k] = Math.round((weights[k] / currentOthers) * remaining); });
      // Fix rounding
      const total = others.reduce((sum, k) => sum + newWeights[k], 0) + val;
      if (total !== 100) newWeights[others[0]] += 100 - total;
    }
    setWeights(newWeights);
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      await updateActiveProfile(weights);
      setRecalculating(true);
      await recalculateAnalyses();
      load();
    } catch (e) { console.error(e); }
    setSaving(false); setRecalculating(false);
  };

  const handleActivate = async (id) => {
    await activateProfile(id);
    setRecalculating(true);
    await recalculateAnalyses();
    setRecalculating(false);
    load();
  };

  const handleReset = async () => { await resetProfile(); await recalculateAnalyses(); load(); };

  const pieData = [
    { name: 'Technical', value: weights.technicalWeight || 0 },
    { name: 'Fundamental', value: weights.fundamentalWeight || 0 },
    { name: 'Sentiment', value: weights.sentimentWeight || 0 },
    { name: 'Dividend', value: weights.dividendWeight || 0 },
  ];

  return (
    <div className="space-y-6">
      {recalculating && (
        <div className="glass-card p-4 bg-blue-500/10 border-blue-500/30 flex items-center gap-3">
          <span className="animate-spin text-xl">⟳</span>
          <span className="text-blue-400 text-sm font-medium">Recalculating all analyses with new weights...</span>
        </div>
      )}

      {/* Presets */}
      <div>
        <h3 className="text-sm font-semibold text-slate-300 mb-3">📋 Scoring Presets</h3>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {profiles.filter(p => p.isPreset).map(p => (
            <div key={p.id} onClick={() => handleActivate(p.id)}
              className={`glass-card p-5 cursor-pointer text-center transition-all ${p.isDefault ? 'ring-2 ring-blue-500 bg-blue-500/10' : 'hover:bg-slate-700/30'}`}>
              <div className="text-3xl mb-2">{presetEmoji[p.name] || '📊'}</div>
              <div className="font-semibold text-white text-sm">{p.name}</div>
              {p.isDefault && <span className="text-xs text-blue-400 mt-1">Active</span>}
              <div className="mt-3" style={{ width: 80, height: 80, margin: '0 auto' }}>
                <ResponsiveContainer>
                  <PieChart><Pie data={[
                    { v: p.technicalWeight }, { v: p.fundamentalWeight }, { v: p.sentimentWeight }, { v: p.dividendWeight }
                  ]} dataKey="v" cx="50%" cy="50%" innerRadius={20} outerRadius={35}>
                    {COLORS.map((c, i) => <Cell key={i} fill={c} />)}
                  </Pie></PieChart>
                </ResponsiveContainer>
              </div>
              <div className="text-xs text-slate-500 mt-1">{p.technicalWeight}/{p.fundamentalWeight}/{p.sentimentWeight}/{p.dividendWeight}</div>
            </div>
          ))}
        </div>
      </div>

      {/* Custom Weights */}
      <div className="glass-card p-6">
        <h3 className="text-sm font-semibold text-slate-300 mb-4">🎛️ Custom Weight Sliders</h3>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          <div className="space-y-5">
            {[
              { key: 'technicalWeight', label: 'Technical', color: '#3b82f6', icon: '📉' },
              { key: 'fundamentalWeight', label: 'Fundamental', color: '#8b5cf6', icon: '📊' },
              { key: 'sentimentWeight', label: 'Sentiment', color: '#f59e0b', icon: '🗞️' },
              { key: 'dividendWeight', label: 'Dividend', color: '#ec4899', icon: '💰' },
            ].map(({ key, label, color, icon }) => (
              <div key={key}>
                <div className="flex justify-between mb-1">
                  <span className="text-sm text-slate-400">{icon} {label}</span>
                  <span className="text-sm font-mono font-bold text-white">{weights[key] || 0}%</span>
                </div>
                <input type="range" min="0" max="100" value={weights[key] || 0} onChange={e => handleSlider(key, e.target.value)}
                  className="w-full h-2 rounded-lg appearance-none cursor-pointer" style={{ accentColor: color }} />
              </div>
            ))}
            <div className={`text-xs font-mono ${(weights.technicalWeight || 0) + (weights.fundamentalWeight || 0) + (weights.sentimentWeight || 0) + (weights.dividendWeight || 0) === 100 ? 'text-emerald-400' : 'text-red-400'}`}>
              Sum: {(weights.technicalWeight || 0) + (weights.fundamentalWeight || 0) + (weights.sentimentWeight || 0) + (weights.dividendWeight || 0)}%
            </div>
          </div>
          <div className="flex items-center justify-center w-full">
            <div style={{ width: '100%', height: 250 }}>
              <ResponsiveContainer>
                <PieChart>
                  <Pie data={pieData} dataKey="value" cx="50%" cy="50%" innerRadius={60} outerRadius={80} label={({ name, value }) => `${name}: ${value}%`} labelLine={true} style={{ fontSize: '12px', fill: '#94a3b8' }}>
                    {COLORS.map((c, i) => <Cell key={i} fill={c} />)}
                  </Pie>
                </PieChart>
              </ResponsiveContainer>
            </div>
          </div>
        </div>

        {/* Alert Thresholds */}
        <div className="mt-6 pt-6 border-t border-slate-700/50">
          <h4 className="text-sm font-semibold text-slate-300 mb-3">🚨 Alert Thresholds</h4>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            {[
              { key: 'alertMinOverallScore', label: 'Min Overall' },
              { key: 'alertMinTechnicalScore', label: 'Min Technical' },
              { key: 'alertMinFundamentalScore', label: 'Min Fundamental' },
              { key: 'alertMinSentimentScore', label: 'Min Sentiment' },
            ].map(({ key, label }) => (
              <div key={key}>
                <label className="text-xs text-slate-500">{label}</label>
                <input type="number" min="0" max="100" value={weights[key] || 0} onChange={e => setWeights({ ...weights, [key]: parseInt(e.target.value) || 0 })}
                  className="w-full bg-slate-800/50 border border-slate-700/50 rounded-lg px-3 py-2 text-sm text-white font-mono mt-1" />
              </div>
            ))}
          </div>
        </div>

        <div className="flex flex-col sm:flex-row gap-3 mt-6">
          <button onClick={handleSave} disabled={saving} className="px-6 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-blue-500 text-white text-sm font-medium hover:from-blue-500 hover:to-blue-400 transition-all disabled:opacity-50">
            {saving ? 'Saving...' : '💾 Save & Apply'}
          </button>
          <button onClick={handleReset} className="px-6 py-2.5 rounded-xl bg-slate-700 text-slate-300 text-sm font-medium hover:bg-slate-600 transition-all">
            🔄 Reset to Defaults
          </button>
        </div>
      </div>
    </div>
  );
}

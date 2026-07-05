# StockFinder — Nifty50 Stock Analyzer

A full-stack, real-time stock analysis platform for India's Nifty 50 blue-chip index. StockFinder automatically harvests live market data from IndianAPI.in and GNews (with Yahoo Finance as fallback), runs it through a configurable scoring engine, and delivers actionable **Buy / Hold / Sell** signals across a web dashboard, a cross-platform mobile app, and push notifications — all powered by **zero hardcoded or mock data**.

---

## ✨ Highlights

| | |
|-|-|
| 📊 **Live Market Data** | Prices, financials, dividends, metadata, and institutional shareholding — sourced from IndianAPI.in |
| 🧠 **Scoring Engine** | 6 Dimensions: Technical + Fundamental + Valuation (Intrinsic) + Quality (Piotroski/Altman/Shareholding) + Sentiment + Dividends |
| 🎛️ **Customizable Strategy** | Six preset profiles (Balanced, Growth, Value, Income, Momentum, Quality) or build your own weightings |
| 📱 **Cross-Platform Mobile** | React Native / Expo app for iOS and Android with offline caching |
| 🔔 **Push Notifications** | Instant Expo push alerts when a stock triggers a Buy or Strong Buy signal |
| 🌐 **Web Dashboard** | Interactive charts, sortable tables, sector heatmaps, and a full admin panel |
| ⚡ **Auto Refresh** | Background worker re-fetches everything every 24 hours (configurable) |
| ☁️ **Free Hosting** | Runs on Render (API + Static Site) and Neon (PostgreSQL) — entirely on free tiers |

---

## 🖥️ How to Use the Web App

### Dashboard

The landing page gives you a bird's-eye view of the market:

- **Stats bar** — Total tracked stocks and the number of active trade alerts.
- **Top Gainers & Losers** — Sorted by daily percentage change.
- **Recent Alerts** — The latest Buy/Strong Buy signals with per-category score breakdowns.
- **Sector Performance** — A horizontal bar chart showing which sectors are leading or lagging.
- **Data Freshness** — The `🔄 Data: Xh ago` indicator in the header shows when the last refresh ran. Click **Refresh Data** in the sidebar to trigger one immediately.

### Stocks List

Navigate to the **Stocks** tab to see all 50 constituents. The list is sorted by Overall Score by default so the strongest candidates rise to the top. You can:

- **Search** by symbol or company name.
- **Filter** by sector using the dropdown.
- **Re-sort** by clicking any column header (Price, Change %, Market Cap, Score, Signal).

### Stock Detail — Deep-Dive Analysis

Click any stock to open the full analysis view:

| Section | What you'll see |
|---------|----------------|
| **Price Chart** | Interactive candlestick chart with volume overlay and a range selector (1 W → 8 Y) |
| **Overall Score** | A gauge showing the composite 0–100 score and the resulting signal (Strong Buy → Strong Sell) |
| **Technical Indicators** | RSI-14, MACD histogram, SMA 50/200 crossover, Bollinger Band position, ADX trend strength |
| **Fundamental Ratios** | P/E, P/B, ROE, ROA, Debt-to-Equity, EPS, Revenue & Earnings Growth YoY |
| **Valuation & Quality** | Fair Value / Graham Number comparisons, Piotroski F-Score, Altman Z-Score, and institutional shareholding patterns (Promoter/FII/DII) |
| **Sentiment** | Latest news headlines via GNews, a bullish/bearish score, and article count breakdown |
| **Dividends** | Yield, payout ratio, and full dividend history |
| **Analysis Verdict** | A human-readable reasoning paragraph explaining why each sub-score was assigned |

### Scoring Profiles — Customize Your Strategy

Navigate to **Settings** to switch or tune the scoring algorithm:

1. **Preset Profiles** — One-click activation of curated strategies:
   - **Balanced** — Equal emphasis across all factors
   - **Growth** — Chases earnings & revenue momentum
   - **Value** — Hunts for undervalued, low-debt stocks
   - **Income** — Maximises dividend yield with sustainability checks
   - **Momentum** — Rides strong technical trends
   - **Quality** — Focuses on ROE, margins, and low leverage
2. **Custom Weights** — Drag the sliders to set your own Technical / Fundamental / Sentiment / Dividend split (they always sum to 100%).
3. **Alert Thresholds** — Define the minimum scores required for a stock to qualify as a signal.
4. **Instant Recalculation** — Scores and signals are recalculated immediately when you save changes.

---

## 📱 How to Use the Mobile App

The React Native (Expo) app mirrors the web experience with native performance and offline support:

| Tab | Description |
|-----|-------------|
| **Home** | Dashboard with stats, top movers, latest alerts, and sector performance |
| **Stocks** | Search and browse all 50 stocks; tap any row to open the detail view |
| **Alerts** | Dedicated feed of the latest trade signals from the analysis engine |
| **Settings** | Manage preferences and access the Admin panel |

**Offline Mode** — The app caches every API response locally. When your device loses connectivity, an **offline banner** appears at the top and the app seamlessly serves the most recently cached data.

**Push Notifications** — On first launch the app registers your device with the backend. When the daily data refresh generates a new Buy or Strong Buy alert, you'll receive a push notification. Tapping it routes you directly to that stock's detail screen.

**Admin Panel** — Accessible from Settings → Admin & Scoring Profiles. From here you can trigger a manual data refresh or switch the active scoring profile on the fly.

---

## 🏗️ Architecture Overview

```
┌──────────────┐    ┌──────────────────┐    ┌──────────────┐
│  React Web   │◄──►│  .NET 8 API      │◄──►│  PostgreSQL  │
│  (Vite)      │    │  (EF Core)       │    │  (Neon)      │
└──────────────┘    │                  │    └──────────────┘
                    │  Background Jobs │
┌──────────────┐    │  ┌────────────┐  │    ┌──────────────┐
│  Expo Mobile │◄──►│  │DataRefresh │──┼──► │ IndianAPI.in │
│  (RN)        │    │  │Service     │  │    │ Yahoo Finance│
└──────────────┘    │  └────────────┘  │    │ GNews API    │
                    │         │        │    └──────────────┘
       Push ◄───────│  PushNotification│
                    │  Service         │
                    └──────────────────┘
```

### Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend API | .NET 8, Entity Framework Core, PostgreSQL |
| Web Frontend | React 18, Vite, Recharts, Vanilla CSS |
| Mobile App | React Native, Expo SDK 54, Expo Router |
| Data Sources | IndianAPI.in (prices, fundamentals, shareholding), Yahoo Finance (fallback), GNews (sentiment) |
| Hosting | Render (Web Service + Static Site), Neon (managed PostgreSQL) |

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [Local Setup Guide](docs/SETUP.md) | Install dependencies, configure secrets, and run locally |
| [Cloud Deployment Guide](docs/DEPLOYMENT.md) | Deploy to Render + Neon for free |
| [System Architecture](docs/architecture.md) | Deep-dive into the application layers and data flow |
| [Data Refresh Flow](docs/data_refresh_flow.md) | How the background worker fetches and processes data |
| [Analysis Engine Flow](docs/analysis_engine_flow.md) | How scores and signals are calculated |

---

## 📄 License

MIT

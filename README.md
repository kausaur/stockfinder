# StockFinder — Nifty50 Stock Analyzer

A comprehensive, full-stack application designed to track, analyze, and score the top 50 blue-chip stocks of the Indian stock market (Nifty50). The application automatically fetches live market data, financial statements, and news sentiment from real external APIs — with **zero hardcoded or mock data** — and runs them through a customizable scoring engine to generate active Buy, Hold, and Sell signals.

## 🚀 Features

- **Automated Data Harvesting**: A background worker automatically pulls live metadata (sector, industry, market cap, 52-week range, day change), historical prices, dividends, and financial statements (Income, Balance Sheet, Cash Flow) from Yahoo Finance.
- **Real Stock Metadata**: Sector, industry, market cap, 52-week high/low, and shares outstanding are all fetched from Yahoo Finance's `quoteSummary` API on every refresh cycle.
- **Sentiment Analysis**: Leverages the GNews API to fetch recent news headlines for each stock and computes a bullish/bearish sentiment score.
- **Technical & Fundamental Engine**: Calculates deep technical indicators (MACD, RSI, Moving Averages, Bollinger Bands) and fundamental metrics (P/E ratio, ROE, Debt-to-Equity, EPS, Book Value, Free Cash Flow per share) using real shares outstanding data.
- **Customizable Scoring Profiles**: Define your own weightings for the analysis engine (e.g., weigh Technicals at 60% and Fundamentals at 40%) to cater to your specific trading strategy (Value, Growth, Momentum, Income).
- **Live Dashboard**: A React frontend with dynamic candlestick charts, interactive score gauges, sortable stock list, and real-time summary of top gainers and losers.
- **Auto Refresh**: Data refreshes automatically every 24 hours (configurable). The last-refreshed timestamp is displayed in the UI header.

---

## 🛠️ Setup Instructions (From Scratch)

### Prerequisites
- **.NET 10 SDK** or later
- **Node.js** (v18+) and **npm**
- **Docker Desktop** (for running the PostgreSQL database)
- **GNews API Key** (Free tier available at [gnews.io](https://gnews.io/))

### 1. Database Setup
The backend relies on PostgreSQL. Start a local instance using Docker:
```bash
docker run --name stockfinder-db -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres
```

### 2. Configure Backend Secrets
Navigate to the API project directory and set your GNews API key via user-secrets (keeps it safely outside source control):
```bash
cd f:\projects\stockfinder\src\Nifty50.Api
dotnet user-secrets init
dotnet user-secrets set "GNewsApiKey" "YOUR_API_KEY_HERE"
```

### 3. Start the .NET Backend
The backend uses Entity Framework Core to automatically apply database migrations and create the schema on startup.
```bash
cd f:\projects\stockfinder
dotnet run --project src\Nifty50.Api\Nifty50.Api.csproj
```
*Note: Upon startup, the `DataRefreshService` will automatically begin fetching 8 years of historical data for all 50 stocks. This initial population may take several minutes. After that, data auto-refreshes every 24 hours.*

### 4. Start the React Frontend
Open a new terminal to install UI dependencies and start the Vite development server:
```bash
cd f:\projects\stockfinder\src\Nifty50.Web
npm install
npm run dev
```

### 5. Access the Application
Open your browser and navigate to: **http://localhost:5173**

---

## ⚙️ Configuration

All runtime settings live in `src/Nifty50.Api/appsettings.json`:

| Setting | Default | Description |
|---------|---------|-------------|
| `DataRefresh:IntervalHours` | `24` | How often the background worker re-fetches all data |
| `GNews:MaxStocksPerRefresh` | `20` | Max stocks to fetch sentiment for per refresh (GNews free tier: 100 req/day) |
| `GNewsApiKey` | Set via user-secrets | Your GNews API key |

---

## 📖 How to Use the App

1. **Dashboard Overview**: Gives you an immediate bird's-eye view — total stocks tracked, active signals (grouped by signal type), top gainers/losers, and sector performance heatmap.
2. **Stock List**: Navigate to the "Stocks" tab. The list is sorted by Overall Score by default, so the strongest buy candidates appear first. You can re-sort by clicking any column header, and filter by sector using the dropdown.
3. **Deep Dive Analysis**: Click any stock to open the detailed view:
   - Interactive candlestick chart with volume overlay (1W to 8Y range selector)
   - Technical Indicators: RSI, MACD, SMA, ADX, ATR
   - Fundamental Ratios: P/E, ROE, D/E, EPS, Revenue Growth — all computed from real financial data with real shares outstanding
   - Sentiment: Latest news headlines and bullish/bearish score
   - Analysis Verdict: Overall score with gauges for each sub-category and a human-readable reasoning string
4. **Customize Your Strategy**: Navigate to "Settings" to:
   - Switch between preset profiles (Balanced, Growth, Value, Income)
   - Tune top-level weights with interactive sliders that always sum to 100%
   - Set alert thresholds for what qualifies as a "signal"
   - Scores are instantly recalculated after saving
5. **Data Freshness**: The "🔄 Data: Xh ago" indicator in the header shows when data was last refreshed. Click "Refresh Data" in the sidebar to trigger an immediate refresh (note: this counts against your GNews daily quota).

---

## 📚 Further Documentation

For technical deep-dives into how the application is built, see the `docs` folder:
- [System Architecture](docs/architecture.md)
- [Data Refresh Flow](docs/data_refresh_flow.md)
- [Analysis Engine Flow](docs/analysis_engine_flow.md)

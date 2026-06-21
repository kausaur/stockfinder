# Nifty50 Stock Analyzer

A comprehensive, full-stack application designed to track, analyze, and score the top 50 blue-chip stocks of the Indian stock market (Nifty50). The application automatically scrapes historical price data, financial statements, and news sentiment, and runs them through a customizable scoring engine to generate active Buy, Hold, and Sell signals.

## 🚀 Features

- **Automated Data Harvesting**: A background worker automatically pulls historical prices, dividends, and financial statements (Income, Balance Sheet, Cash Flow) from Yahoo Finance.
- **Sentiment Analysis**: Leverages the GNews API to scrape recent news headlines for each stock and computes a bullish/bearish sentiment score.
- **Technical & Fundamental Engine**: Calculates deep technical indicators (MACD, RSI, Moving Averages, Bollinger Bands) and fundamental metrics (P/E ratio, ROE, Debt-to-Equity, etc.) on the fly.
- **Customizable Scoring Profiles**: Define your own weightings for the analysis engine (e.g., weigh Technicals at 60% and Fundamentals at 40%) to cater to your specific trading strategy (Value, Growth, Momentum, etc.).
- **Live Dashboard**: A stunning React frontend with dynamic candlestick charts, interactive score gauges, and a real-time summary of top gainers and losers.

---

## 🛠️ Setup Instructions (From Scratch)

### Prerequisites
- **.NET 8.0 SDK** or later
- **Node.js** (v18+) and **npm**
- **Docker Desktop** (for running the PostgreSQL database)
- **GNews API Key** (Free tier available at [gnews.io](https://gnews.io/))

### 1. Database Setup
The backend relies on PostgreSQL. You can start a local instance using Docker:
```bash
docker run --name nifty50-db -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres
```

### 2. Configure Backend Secrets
Navigate to the API project directory and configure your user secrets (specifically your GNews API key) so they are safely stored outside the source code:
```bash
cd f:\projects\nifty50\src\Nifty50.Api
dotnet user-secrets init
dotnet user-secrets set "GNewsApiKey" "YOUR_API_KEY_HERE"
```

### 3. Start the .NET Backend
The backend application uses Entity Framework Core to automatically apply database migrations and create the schema on startup.
```bash
cd f:\projects\nifty50
dotnet run --project src\Nifty50.Api\Nifty50.Api.csproj
```
*Note: Upon startup, the `DataRefreshService` will automatically begin fetching 8 years of historical data for all 50 stocks. This initial population may take a few minutes.*

### 4. Start the React Frontend
Open a new terminal window to install the UI dependencies and start the Vite development server.
```bash
cd f:\projects\nifty50\src\Nifty50.Web
npm install
npm run dev
```

### 5. Access the Application
Open your web browser and navigate to:
**http://localhost:5173**

---

## 📖 How to Use the App to its Full Capability

1. **Dashboard Overview**: The main dashboard gives you an immediate bird's-eye view of the market. It shows the total stocks tracked, recent alerts (Strong Buy/Sell signals), and the top gainers/losers of the day.
2. **Deep Dive Analysis**: Navigate to the "Stocks" tab and click on any individual stock to open the detailed view. Here you can:
   - View historical price movements using the interactive Candlestick & Volume charts.
   - Analyze the raw Technical Indicators (RSI, MACD) and Fundamental Ratios (P/E, ROE) that the engine computed for today.
   - Review the latest news headlines and see why the system scored the sentiment as Bullish or Bearish.
3. **Customize Your Strategy**: The core power of Nifty50 Analyzer is its scoring engine. Navigate to the "Settings" or "Profiles" section (or use the backend API) to adjust the **Scoring Profile**. 
   - *Example*: If you are a value investor, you can increase the `FundamentalWeight` to 60% and `SentimentWeight` to 10%. The system will instantly recalculate all 50 stocks and potentially issue new Buy/Sell alerts based on your tailored strategy.
4. **Data Refreshes**: The background worker runs continuously. To force an immediate refresh of all data points, you can call the `POST /api/refresh` endpoint. Note that the GNews API is limited to 100 requests/day on the free tier, so manual refreshes should be used sparingly.

---

## 📚 Further Documentation

For deep technical insights into how the application is built, please refer to the flow diagrams in the `docs` folder:
- [System Architecture](docs/architecture.md)
- [Data Refresh Flow](docs/data_refresh_flow.md)
- [Analysis Engine Flow](docs/analysis_engine_flow.md)

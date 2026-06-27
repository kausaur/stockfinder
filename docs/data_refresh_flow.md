# Data Refresh Flow

The Data Refresh Flow is the heart of the Nifty50 Analyzer's automated data harvesting. Managed by the `DataRefreshService`, it systematically iterates through the Nifty50 index stocks on a configurable interval (default: 24 hours) and aggregates historical data and real-time metadata from various sources into the PostgreSQL database.

## Workflow Diagram

```mermaid
sequenceDiagram
    participant Worker as DataRefreshService
    participant Repo as StockRepository
    participant Yahoo as Yahoo Finance API
    participant GNews as GNews API
    participant Engine as AnalysisEngine

    Worker->>Worker: Loop through 50 Stocks (e.g., RELIANCE.NS)
    
    Worker->>Repo: Ensure Stock exists in DB
    Repo-->>Worker: Stock Id
    
    %% Metadata Fetching
    Worker->>Yahoo: Fetch Metadata (Sector, MarketCap, SharesOutstanding, etc.)
    Yahoo-->>Worker: JSON Metadata
    Worker->>Repo: Update Stock entity
    
    %% Price Data Fetching
    Worker->>Yahoo: Fetch Historical Prices (incremental from last date)
    Yahoo-->>Worker: JSON Price Data
    Worker->>Repo: Upsert StockPrices
    
    %% Dividends
    Worker->>Yahoo: Fetch Dividends (8 Years)
    Yahoo-->>Worker: JSON Dividend Data
    Worker->>Repo: Upsert Dividends
    
    %% Fundamentals
    Worker->>Yahoo: Fetch Financial Statements (Income, Balance, CashFlow)
    Yahoo-->>Worker: JSON Statements
    Worker->>Repo: Upsert FinancialStatements
    Worker->>Worker: Calculate FundamentalMetrics snapshot (P/E, ROE, etc.)
    Worker->>Repo: Insert FundamentalMetrics
    
    %% Technicals
    Worker->>Repo: Get Last 30 Days Prices
    Repo-->>Worker: StockPrice List
    Worker->>Worker: Calculate TechnicalIndicators (RSI, MACD, etc.)
    Worker->>Repo: Upsert TechnicalIndicators
    
    %% Sentiment (With Rate Limit Protection)
    opt If Daily API limit not exceeded
        Worker->>GNews: Fetch News Headlines
        GNews-->>Worker: JSON Headlines
        Worker->>Worker: Score Headlines (Bullish/Bearish)
        Worker->>Repo: Insert SentimentAnalysis
    end
    
    Worker->>Worker: Wait 500ms (Rate Limit Protection)
    Worker->>Worker: Next Stock...
    
    %% Analysis phase
    Worker->>Engine: RecalculateAllAsync()
    Engine-->>Worker: Done
    
    %% Notifications
    Worker->>Worker: Check for new Alerts
    Worker->>Expo: Send Push Notifications for Buy/Strong Buy
```

## Upsert Strategy
Because the process runs repeatedly, it is crucial not to duplicate data or violate primary keys. The `StockRepository` implements a careful "Upsert" (Update or Insert) pattern. It checks if a record for a specific Stock and Date exists; if it does, it manually updates the fields rather than attempting an EF Core `SetValues` on the primary key.

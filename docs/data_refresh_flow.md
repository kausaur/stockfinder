# Data Refresh Flow

The Data Refresh Flow is the heart of the Nifty50 Analyzer's automated data harvesting. Managed by the `DataRefreshService`, it systematically iterates through the Nifty50 index stocks daily at 00:00 UTC (5:30 AM IST), with smart cold-start detection, aggregating historical data and real-time metadata from various sources into the PostgreSQL database.

## Workflow Diagram

```mermaid
sequenceDiagram
    participant Worker as DataRefreshService
    participant Repo as StockRepository
    participant IndianAPI as IndianAPI.in
    participant Yahoo as Yahoo Finance API
    participant GNews as GNews API
    participant GoogleNewsRSS as Google News RSS
    participant Engine as AnalysisEngine

    Worker->>Worker: Loop through 50 Stocks (e.g., RELIANCE.NS)
    
    Worker->>Repo: Ensure Stock exists in DB
    Repo-->>Worker: Stock Id
    
    %% Metadata Fetching
    Worker->>IndianAPI: Fetch Metadata (Sector, MarketCap, SharesOutstanding, etc.)
    IndianAPI-->>Worker: JSON Metadata
    opt On IndianAPI Failure
        Worker->>Yahoo: Fetch Metadata (Fallback)
        Yahoo-->>Worker: JSON Metadata
    end
    Worker->>Repo: Update Stock entity
    
    %% Price Data Fetching
    Worker->>IndianAPI: Fetch Historical Prices (400 Calendar Days, Adjusted Close)
    IndianAPI-->>Worker: JSON Price Data
    opt On IndianAPI Failure
        Worker->>Yahoo: Fetch Historical Prices (Fallback)
        Yahoo-->>Worker: JSON Price Data
    end
    Worker->>Repo: Upsert StockPrices
    
    %% Dividends
    Worker->>Yahoo: Fetch Dividends (8 Years)
    Yahoo-->>Worker: JSON Dividend Data
    Worker->>Repo: Upsert Dividends
    
    %% Fundamentals
    Worker->>IndianAPI: Fetch Financial Statements (Income, Balance, CashFlow)
    IndianAPI-->>Worker: JSON Statements
    opt On IndianAPI Failure
        Worker->>Yahoo: Fetch Financial Statements (Fallback)
        Yahoo-->>Worker: JSON Statements
    end
    Worker->>Repo: Upsert FinancialStatements
    
    Worker->>IndianAPI: Fetch Institutional Shareholding (Promoter, FII, DII)
    IndianAPI-->>Worker: JSON Shareholding Data

    Worker->>Worker: Calculate FundamentalMetrics snapshot (P/E, ROE, etc.)
    Worker->>Repo: Insert FundamentalMetrics
    
    Worker->>Worker: Calculate Intrinsic Value (Fair Value, Graham Number)
    Worker->>Repo: Upsert IntrinsicValuation

    Worker->>Worker: Assess Quality (Piotroski, Altman Z, Shareholding)
    Worker->>Repo: Upsert QualityMetric

    %% Technicals
    Worker->>Repo: Get Last 400 Calendar Days Prices (Adjusted Close)
    Repo-->>Worker: StockPrice List
    Worker->>Worker: Calculate TechnicalIndicators (RSI, MACD, etc.)
    Worker->>Repo: Upsert TechnicalIndicators
    
    %% Sentiment (With Rate Limit Protection)
    opt If Daily API limit not exceeded and API Key exists
        Worker->>GNews: Fetch News Headlines
        GNews-->>Worker: JSON Headlines
    end
    opt On GNews Failure or No Key
        Worker->>GoogleNewsRSS: Fetch Headlines (Free)
        GoogleNewsRSS-->>Worker: XML Headlines
    end
    opt On Google News Failure
        Worker->>Yahoo: Fetch Headlines (Search Fallback)
        Yahoo-->>Worker: JSON Headlines
    end
    Worker->>Worker: Filter Relevant Headlines
    Worker->>Worker: Score Headlines (Bullish/Bearish)
    Worker->>Repo: Insert SentimentAnalysis
    
    Worker->>Worker: Wait 1000ms (Rate Limit Protection)
    Worker->>Worker: Next Stock...
    
    %% Post-processing
    Worker->>Worker: Compact ScoreHistory (keep 1/week for >90d, 1/month for >1yr)
    
    %% Analysis phase
    Worker->>Engine: RecalculateAllAsync()
    Engine-->>Worker: Done
    
    %% Notifications
    Worker->>Worker: Check for new Alerts
    Worker->>Expo: Send Push Notifications for Buy/Strong Buy
```

## Upsert Strategy
Because the process runs repeatedly, it is crucial not to duplicate data or violate primary keys. The `StockRepository` implements a careful "Upsert" (Update or Insert) pattern. It checks if a record for a specific Stock and Date exists; if it does, it manually updates the fields rather than attempting an EF Core `SetValues` on the primary key.

> **Note on Scheduling:** Instead of a simple `Task.Delay` by a static number of hours, the worker intelligently schedules the next iteration for the upcoming **00:00 UTC (5:30 AM IST)**. This ensures data is perfectly synchronized with the market's daily close, and correctly handles application restarts on the Render free tier (smartly skipping if a refresh already occurred since midnight).

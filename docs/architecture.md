# System Architecture

The Nifty50 Stock Analyzer is built using a modern decoupled architecture. The backend is a .NET 10 Web API that uses Entity Framework Core to interface with a PostgreSQL database. The frontend is a React application built with Vite and Tailwind CSS.

## High-Level Overview

```mermaid
graph TD
    subgraph Frontend [Client Applications]
        UI[React Web Application] --> API_Client[Axios API Client]
        API_Client --> Charts[Lightweight Charts & Recharts]
        Mobile[React Native Mobile App] --> API_Client
    end

    subgraph Backend [.NET 8 Web API]
        Controllers[API Controllers] --> Core_Services[Core Business Logic]
        Core_Services --> EF_Core[Entity Framework Core]
        
        BackgroundWorker[DataRefreshService Worker] --> Data_Services[Data Fetching Services]
        Data_Services --> Analysis_Engine[Analysis & Scoring Engine]
        Analysis_Engine --> EF_Core
    end

    subgraph External APIs
        Data_Services -- "Metadata, Prices & Financials" --> IndianAPI[IndianAPI.in]
        Data_Services -- "Metadata & Prices (Fallback)" --> YahooFinance[Yahoo Finance API]
        Data_Services -- "Sentiment (Primary)" --> GNews[GNews API]
        Data_Services -- "Sentiment (Free Fallback)" --> GoogleNewsRSS[Google News RSS]
        Data_Services -- "Sentiment (Last Resort)" --> YahooFinance
    end

    subgraph Database
        EF_Core <--> PostgreSQL[(PostgreSQL DB)]
    end

    API_Client <== "REST (JSON)" ==> Controllers
    
    classDef default fill:#1e293b,stroke:#3b82f6,stroke-width:2px,color:#f8fafc;
    classDef external fill:#0f172a,stroke:#f59e0b,stroke-width:2px,color:#f8fafc;
    classDef database fill:#0f172a,stroke:#10b981,stroke-width:2px,color:#f8fafc;
    
    class IndianAPI,YahooFinance,GNews,GoogleNewsRSS external;
    class PostgreSQL database;
```

## Component Details

1. **Frontend Applications**: Built with React (Web) and React Native (Mobile), providing responsive dashboards. They poll the backend API to retrieve stock prices, historical metrics, and sentiment analysis.
2. **Backend Controllers**: Exposes a clean RESTful API (`/api/stocks`, `/api/dashboard`, `/api/scoring-profiles`) consumed by the frontends.
3. **Background Worker**: The `DataRefreshService` is an `IHostedService` that runs continuously in the background (on a configurable interval, e.g. every 24 hours), ensuring the database is always up to date with the latest market data without blocking the main API threads.
4. **Data Services**: Handlers like `IndianApiService` and `GNewsSentimentService` manage HTTP requests, API rate limits, and JSON parsing. The primary `IndianApiService` fetches metadata, real-time prices, deep fundamentals, and institutional shareholding patterns. Sentiment analysis uses a robust 3-tier fallback strategy (`GNews` → `GoogleNewsRSS` → `YahooFinance`) to guarantee relevant headlines even without an API key.
5. **Analysis Engine**: The `StockAnalysisEngine` calculates a final 0-100 score based on 6 independent dimensions (Technical, Fundamental, Valuation, Quality, Sentiment, Dividend). It applies configurable `ScoringProfile` weights to generate SEBI-compliant signals (Accumulate/Hold/Reduce).
6. **Admin & Monitoring**: An in-memory `ApiMonitorService` tracks all external requests, providing real-time success rates, latencies, and error logs directly in the React Admin panel, where users can also manually trigger data refreshes.

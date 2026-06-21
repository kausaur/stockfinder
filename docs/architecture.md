# System Architecture

The Nifty50 Stock Analyzer is built using a modern decoupled architecture. The backend is a .NET 8 Web API that uses Entity Framework Core to interface with a PostgreSQL database. The frontend is a React application built with Vite and Tailwind CSS.

## High-Level Overview

```mermaid
graph TD
    subgraph Frontend [React Web Application]
        UI[User Interface] --> API_Client[Axios API Client]
        API_Client --> Charts[Lightweight Charts & Recharts]
    end

    subgraph Backend [.NET 8 Web API]
        Controllers[API Controllers] --> Core_Services[Core Business Logic]
        Core_Services --> EF_Core[Entity Framework Core]
        
        BackgroundWorker[DataRefreshService Worker] --> Data_Services[Data Fetching Services]
        Data_Services --> Analysis_Engine[Analysis & Scoring Engine]
        Analysis_Engine --> EF_Core
    end

    subgraph External APIs
        Data_Services -- "Metadata, Prices & Financials" --> YahooFinance[Yahoo Finance API]
        Data_Services -- "News Sentiment" --> GNews[GNews API]
    end

    subgraph Database
        EF_Core <--> PostgreSQL[(PostgreSQL DB)]
    end

    API_Client <== "REST (JSON)" ==> Controllers
    
    classDef default fill:#1e293b,stroke:#3b82f6,stroke-width:2px,color:#f8fafc;
    classDef external fill:#0f172a,stroke:#f59e0b,stroke-width:2px,color:#f8fafc;
    classDef database fill:#0f172a,stroke:#10b981,stroke-width:2px,color:#f8fafc;
    
    class YahooFinance,GNews external;
    class PostgreSQL database;
```

## Component Details

1. **Frontend**: Built with React, it features a responsive dashboard. It polls the backend API to retrieve stock prices, historical metrics, and sentiment analysis.
2. **Backend Controllers**: Exposes a clean RESTful API (`/api/stocks`, `/api/dashboard`, `/api/scoring-profiles`) consumed by the frontend.
3. **Background Worker**: The `DataRefreshService` is an `IHostedService` that runs continuously in the background (on a configurable interval, e.g. every 24 hours), ensuring the database is always up to date with the latest market data without blocking the main API threads.
4. **Data Services**: Handlers like `YahooFinanceService`, `YahooMetadataService`, and `GNewsSentimentService` manage HTTP requests, API rate limits, and custom Cookie/Crumb extraction to interface with third-party providers. The metadata service ensures critical fields like Market Cap, Sector, and Shares Outstanding are always populated.

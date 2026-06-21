# Analysis Engine Flow

The Analysis Engine (`StockAnalysisEngine`) is the final step in the data pipeline. Once all raw historical data, fundamentals, technical indicators, and sentiment have been fetched and calculated, the engine applies user-defined weightings (from a `ScoringProfile`) to generate actionable Buy/Sell/Hold signals.

## Engine Workflow

```mermaid
graph TD
    Trigger[Trigger: DataRefresh Complete or Manual Recalculation] --> GetProfile
    
    GetProfile[Fetch Active ScoringProfile] --> IterateStocks
    
    IterateStocks[For Each Stock in DB] --> FetchData
    
    subgraph Data Aggregation
        FetchData[Fetch Latest Data Points]
        FetchData --> TData[Latest TechnicalIndicator]
        FetchData --> FData[Latest FundamentalMetric]
        FetchData --> SData[Latest SentimentAnalysis]
        FetchData --> DData[Latest Dividends]
    end
    
    subgraph Scoring Logic
        TData --> TScore[Calculate Technical Score<br>0-100]
        FData --> FScore[Calculate Fundamental Score<br>0-100]
        SData --> SScore[Calculate Sentiment Score<br>0-100]
        DData --> DScore[Calculate Dividend Score<br>0-100]
    end
    
    subgraph Weighting & Signals
        TScore --> Weighting
        FScore --> Weighting
        SScore --> Weighting
        DScore --> Weighting
        
        Profile[Active Scoring Profile Weights] -.-> Weighting
        
        Weighting[Weighted Average Calculation] --> FinalScore[Overall Score<br>0-100]
        FinalScore --> Thresholds{Check Signal Thresholds}
        
        Thresholds -->|Score > 80| StrongBuy(Strong Buy)
        Thresholds -->|Score > 65| Buy(Buy)
        Thresholds -->|Score > 45| Hold(Hold)
        Thresholds -->|Score > 30| Sell(Sell)
        Thresholds -->|Score < 30| StrongSell(Strong Sell)
    end
    
    StrongBuy --> Alert{Is Alert Threshold Met?}
    Buy --> Alert
    Hold --> Alert
    Sell --> Alert
    StrongSell --> Alert
    
    Alert -->|Yes| SaveAlert[Generate Alert Reason]
    Alert -->|No| SaveNormal[No Alert Required]
    
    SaveAlert --> DB[(PostgreSQL: StockAnalyses)]
    SaveNormal --> DB
```

## Scoring Profiles
The system allows users to define custom `ScoringProfiles`. A profile dictates the percentage weight assigned to each category. 

For example, a **"Value Investor"** profile might be configured as:
- Fundamental Weight: 60%
- Dividend Weight: 20%
- Technical Weight: 10%
- Sentiment Weight: 10%

Additionally, there are **sub-weights** within the Technical and Fundamental categories. For instance, the Technical category has internal weights for RSI, MACD, Moving Averages, Bollinger Bands, ADX, and Volume. 

When the engine runs, it first calculates each category's score using its internal sub-weights. Then, it multiplies the raw 0-100 score of each category by the top-level percentages to determine the stock's final `OverallScore`.

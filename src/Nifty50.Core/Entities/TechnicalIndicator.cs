namespace Nifty50.Core.Entities;

public class TechnicalIndicator : BaseEntity
{
    public Guid StockId { get; set; }
    public DateTime Date { get; set; }

    // Moving Averages
    public decimal? SMA20 { get; set; }
    public decimal? SMA50 { get; set; }
    public decimal? SMA200 { get; set; }
    public decimal? EMA12 { get; set; }
    public decimal? EMA26 { get; set; }

    // RSI
    public decimal? RSI14 { get; set; }

    // MACD
    public decimal? MACD { get; set; }
    public decimal? MACDSignal { get; set; }
    public decimal? MACDHistogram { get; set; }

    // Bollinger Bands
    public decimal? BollingerUpper { get; set; }
    public decimal? BollingerMiddle { get; set; }
    public decimal? BollingerLower { get; set; }

    // Volatility & Trend
    public decimal? ATR14 { get; set; }
    public decimal? ADX14 { get; set; }

    // Stochastic
    public decimal? StochK { get; set; }
    public decimal? StochD { get; set; }

    // Volume
    public decimal? OBV { get; set; }
    public decimal? VWAP { get; set; }
    public decimal? MFI14 { get; set; }

    // Additional
    public decimal? CCI20 { get; set; }
    public decimal? WilliamsR14 { get; set; }
    public decimal? ParabolicSar { get; set; }

    // Ichimoku
    public decimal? IchimokuTenkan { get; set; }
    public decimal? IchimokuKijun { get; set; }
    public decimal? IchimokuSenkouA { get; set; }
    public decimal? IchimokuSenkouB { get; set; }

    public Stock Stock { get; set; } = null!;
}

using Nifty50.Core.Entities;
using Nifty50.Core.Interfaces;
using Skender.Stock.Indicators;

namespace Nifty50.Infrastructure.Services;

public class TechnicalAnalysisService : ITechnicalAnalysisService
{
    public List<TechnicalIndicator> CalculateIndicators(Guid stockId, List<StockPrice> prices)
    {
        if (prices.Count < 30) return new List<TechnicalIndicator>();

        var quotes = prices.OrderBy(p => p.Date).Select(p => new Quote
        {
            Date = p.Date,
            Open = p.Open,
            High = p.High,
            Low = p.Low,
            Close = p.Close,
            Volume = p.Volume
        }).ToList();

        var sma20 = quotes.GetSma(20).ToList();
        var sma50 = quotes.GetSma(50).ToList();
        var sma200 = quotes.GetSma(200).ToList();
        var ema12 = quotes.GetEma(12).ToList();
        var ema26 = quotes.GetEma(26).ToList();
        var rsi = quotes.GetRsi(14).ToList();
        var macd = quotes.GetMacd(12, 26, 9).ToList();
        var bb = quotes.GetBollingerBands(20, 2).ToList();
        var atr = quotes.GetAtr(14).ToList();
        var adx = quotes.GetAdx(14).ToList();
        var stoch = quotes.GetStoch(14, 3, 3).ToList();
        var obv = quotes.GetObv().ToList();
        var mfi = quotes.GetMfi(14).ToList();
        var cci = quotes.GetCci(20).ToList();
        var willR = quotes.GetWilliamsR(14).ToList();
        var psar = quotes.GetParabolicSar().ToList();

        IReadOnlyList<IchimokuResult>? ichimoku = null;
        try { ichimoku = quotes.GetIchimoku().ToList(); } catch { /* May fail with insufficient data */ }

        var indicators = new List<TechnicalIndicator>();
        for (int i = 0; i < quotes.Count; i++)
        {
            indicators.Add(new TechnicalIndicator
            {
                StockId = stockId,
                Date = quotes[i].Date,
                SMA20 = (decimal?)sma20[i].Sma,
                SMA50 = (decimal?)sma50[i].Sma,
                SMA200 = i < sma200.Count ? (decimal?)sma200[i].Sma : null,
                EMA12 = (decimal?)ema12[i].Ema,
                EMA26 = (decimal?)ema26[i].Ema,
                RSI14 = (decimal?)rsi[i].Rsi,
                MACD = (decimal?)macd[i].Macd,
                MACDSignal = (decimal?)macd[i].Signal,
                MACDHistogram = (decimal?)macd[i].Histogram,
                BollingerUpper = (decimal?)bb[i].UpperBand,
                BollingerMiddle = (decimal?)bb[i].Sma,
                BollingerLower = (decimal?)bb[i].LowerBand,
                ATR14 = (decimal?)atr[i].Atr,
                ADX14 = (decimal?)adx[i].Adx,
                StochK = (decimal?)stoch[i].K,
                StochD = (decimal?)stoch[i].D,
                OBV = (decimal?)obv[i].Obv,
                MFI14 = (decimal?)mfi[i].Mfi,
                CCI20 = (decimal?)cci[i].Cci,
                WilliamsR14 = (decimal?)willR[i].WilliamsR,
                ParabolicSar = (decimal?)psar[i].Sar,
                IchimokuTenkan = ichimoku != null && i < ichimoku.Count ? (decimal?)ichimoku[i].TenkanSen : null,
                IchimokuKijun = ichimoku != null && i < ichimoku.Count ? (decimal?)ichimoku[i].KijunSen : null,
                IchimokuSenkouA = ichimoku != null && i < ichimoku.Count ? (decimal?)ichimoku[i].SenkouSpanA : null,
                IchimokuSenkouB = ichimoku != null && i < ichimoku.Count ? (decimal?)ichimoku[i].SenkouSpanB : null,
            });
        }
        return indicators;
    }
}

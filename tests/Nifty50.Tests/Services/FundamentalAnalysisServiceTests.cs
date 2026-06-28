using System;
using System.Collections.Generic;
using Nifty50.Core.Entities;
using Nifty50.Core.Enums;
using Nifty50.Infrastructure.Services;
using Xunit;

namespace Nifty50.Tests.Services;

public class FundamentalAnalysisServiceTests
{
    private readonly FundamentalAnalysisService _service;

    public FundamentalAnalysisServiceTests()
    {
        _service = new FundamentalAnalysisService();
    }

    [Fact]
    public void CalculateMetrics_WithValidStatements_CalculatesCorrectly()
    {
        var stockId = Guid.NewGuid();
        var statements = new List<FinancialStatement>
        {
            new FinancialStatement
            {
                StatementType = StatementType.IncomeStatement,
                Period = PeriodType.Annual,
                PeriodEndDate = new DateTime(2025, 12, 31),
                TotalRevenue = 100000,
                GrossProfit = 60000,
                OperatingIncome = 30000,
                NetIncome = 20000,
                EarningsPerShare = 10
            },
            new FinancialStatement
            {
                StatementType = StatementType.IncomeStatement,
                Period = PeriodType.Annual,
                PeriodEndDate = new DateTime(2024, 12, 31),
                TotalRevenue = 90000,
                NetIncome = 15000
            },
            new FinancialStatement
            {
                StatementType = StatementType.BalanceSheet,
                Period = PeriodType.Annual,
                PeriodEndDate = new DateTime(2025, 12, 31),
                TotalAssets = 500000,
                TotalLiabilities = 200000,
                TotalEquity = 300000,
                TotalDebt = 100000,
                CurrentAssets = 150000,
                CurrentLiabilities = 50000,
                Inventory = 20000,
                CashAndEquivalents = 30000
            }
        };

        var result = _service.CalculateMetrics(stockId, statements, currentPrice: 150);

        Assert.NotNull(result);
        Assert.Equal(15m, result.PERatio); // 150 / 10
        Assert.Equal(60m, result.GrossProfitMargin); // 60000 / 100000 * 100
        Assert.Equal(20m, result.NetProfitMargin); // 20000 / 100000 * 100
        Assert.Equal(3m, result.CurrentRatio); // 150000 / 50000
        Assert.Equal(2.6m, result.QuickRatio); // (150000 - 20000) / 50000
    }
}

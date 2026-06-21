using Nifty50.Core.Enums;

namespace Nifty50.Core.Entities;

public class FinancialStatement : BaseEntity
{
    public Guid StockId { get; set; }
    public StatementType StatementType { get; set; }
    public PeriodType Period { get; set; }
    public DateTime PeriodEndDate { get; set; }

    // Balance Sheet
    public decimal? TotalAssets { get; set; }
    public decimal? TotalLiabilities { get; set; }
    public decimal? TotalEquity { get; set; }
    public decimal? CurrentAssets { get; set; }
    public decimal? CurrentLiabilities { get; set; }
    public decimal? CashAndEquivalents { get; set; }
    public decimal? TotalDebt { get; set; }
    public decimal? NetDebt { get; set; }
    public decimal? Inventory { get; set; }
    public decimal? AccountsReceivable { get; set; }
    public decimal? AccountsPayable { get; set; }

    // Income Statement
    public decimal? TotalRevenue { get; set; }
    public decimal? GrossProfit { get; set; }
    public decimal? OperatingIncome { get; set; }
    public decimal? NetIncome { get; set; }
    public decimal? EBITDA { get; set; }
    public decimal? EarningsPerShare { get; set; }
    public decimal? DilutedEPS { get; set; }
    public decimal? CostOfRevenue { get; set; }
    public decimal? OperatingExpenses { get; set; }
    public decimal? InterestExpense { get; set; }
    public decimal? TaxProvision { get; set; }

    // Cash Flow
    public decimal? OperatingCashFlow { get; set; }
    public decimal? CapitalExpenditures { get; set; }
    public decimal? FreeCashFlow { get; set; }
    public decimal? DividendsPaid { get; set; }
    public decimal? ShareRepurchases { get; set; }

    public Stock Stock { get; set; } = null!;
}

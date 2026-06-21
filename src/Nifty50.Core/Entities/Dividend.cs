namespace Nifty50.Core.Entities;

public class Dividend : BaseEntity
{
    public Guid StockId { get; set; }
    public DateTime ExDate { get; set; }
    public decimal Amount { get; set; }

    public Stock Stock { get; set; } = null!;
}

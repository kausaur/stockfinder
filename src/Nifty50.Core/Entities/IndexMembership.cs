namespace Nifty50.Core.Entities;

public class IndexMembership : BaseEntity
{
    public Guid StockId { get; set; }
    public string IndexName { get; set; } = "NIFTY50";
    public DateTime AddedDate { get; set; }
    public DateTime? RemovedDate { get; set; }
    public bool IsActive { get; set; } = true;
    
    public Stock Stock { get; set; } = null!;
}

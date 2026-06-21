namespace Nifty50.Core.Entities;

/// <summary>
/// Base class for all domain entities providing common audit fields.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>UTC timestamp when the entity was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when the entity was last updated.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Soft-delete flag.</summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>UTC timestamp when the entity was soft-deleted.</summary>
    public DateTime? DeletedAt { get; set; }
}

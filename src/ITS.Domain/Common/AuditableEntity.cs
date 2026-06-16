namespace ITS.Domain.Common;

public abstract class AuditableEntity<TId> : BaseEntity<TId>
{
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid UpdatedByUserId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedByUserId { get; private set; }

    // EF Core concurrency token — mapped to SQL Server ROWVERSION
    public byte[] RowVersion { get; private set; } = [];

    // Called by the SaveChanges interceptor, not by application code directly
    public void SetAuditFieldsOnCreate(Guid userId, DateTime utcNow)
    {
        CreatedAt = utcNow;
        CreatedByUserId = userId;
        UpdatedAt = utcNow;
        UpdatedByUserId = userId;
    }

    public void SetAuditFieldsOnUpdate(Guid userId, DateTime utcNow)
    {
        UpdatedAt = utcNow;
        UpdatedByUserId = userId;
    }

    public void SoftDelete(Guid deletedByUserId, DateTime utcNow)
    {
        IsDeleted = true;
        DeletedAt = utcNow;
        DeletedByUserId = deletedByUserId;
        SetAuditFieldsOnUpdate(deletedByUserId, utcNow);
    }

    public void Restore(Guid restoredByUserId, DateTime utcNow)
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedByUserId = null;
        SetAuditFieldsOnUpdate(restoredByUserId, utcNow);
    }
}

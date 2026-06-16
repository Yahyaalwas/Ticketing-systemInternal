using ITS.Domain.Common;

namespace ITS.Domain.Entities.Content;

public class Attachment : AuditableEntity<Guid>
{
    private Attachment() { }

    public static Attachment Create(
        Guid ticketId,
        Guid? commentId,
        Guid uploaderUserId,
        string fileName,
        string storageKey,
        string contentType,
        long fileSizeBytes,
        string? checksum,
        int retentionDays = 90)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (fileSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "File size must be positive.");

        return new Attachment
        {
            Id = Guid.CreateVersion7(),
            TicketId = ticketId,
            CommentId = commentId,
            UploaderUserId = uploaderUserId,
            FileName = fileName,
            StorageKey = storageKey,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            Checksum = checksum,
            _retentionDays = retentionDays
        };
    }

    private int _retentionDays;

    public Guid TicketId { get; private set; }
    public Guid? CommentId { get; private set; }
    public Guid UploaderUserId { get; private set; }
    public string FileName { get; private set; } = default!;
    public string StorageKey { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long FileSizeBytes { get; private set; }
    public string? Checksum { get; private set; }
    public string? ThumbnailKey { get; private set; }
    public DateTime? PurgeAfterDate { get; private set; }

    public void SetThumbnail(string thumbnailKey)
        => ThumbnailKey = thumbnailKey;

    public new void SoftDelete(Guid deletedByUserId, DateTime utcNow)
    {
        base.SoftDelete(deletedByUserId, utcNow);
        PurgeAfterDate = utcNow.AddDays(_retentionDays);
    }
}

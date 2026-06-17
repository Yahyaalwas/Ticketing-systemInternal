using ITS.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Content;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments", "content");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("AttachmentId").HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(a => a.FileName).IsRequired().HasMaxLength(512);
        builder.Property(a => a.StorageKey).IsRequired().HasMaxLength(1024);
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(256);
        builder.Property(a => a.Checksum).HasMaxLength(128);
        builder.Property(a => a.ThumbnailKey).HasMaxLength(1024);

        builder.Property(a => a.CreatedAt).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(a => a.UpdatedAt).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(a => a.DeletedAt).HasColumnType("datetime2(7)");
        builder.Property(a => a.PurgeAfterDate).HasColumnType("datetime2(7)");
        builder.Property(a => a.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(a => a.StorageKey)
            .IsUnique()
            .HasDatabaseName("UX_Attachments_StorageKey");

        builder.HasIndex(a => a.TicketId)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Attachments_TicketId");

        builder.HasIndex(a => a.PurgeAfterDate)
            .HasFilter("[PurgeAfterDate] IS NOT NULL AND [IsDeleted] = 1")
            .HasDatabaseName("IX_Attachments_PurgeAfterDate");

        builder.HasOne<ITS.Domain.Entities.Tickets.Ticket>()
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}

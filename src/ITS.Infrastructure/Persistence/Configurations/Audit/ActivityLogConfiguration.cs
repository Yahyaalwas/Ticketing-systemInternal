using ITS.Domain.Entities.Audit;
using ITS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Audit;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs", "audit");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("ActivityId")
            .UseIdentityColumn<long>();

        builder.Property(a => a.ActivityType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.EntityType).IsRequired().HasMaxLength(50);
        builder.Property(a => a.EntityId).IsRequired().HasMaxLength(100);
        builder.Property(a => a.FieldName).HasMaxLength(100);
        builder.Property(a => a.OldValue).HasColumnType("nvarchar(max)");
        builder.Property(a => a.NewValue).HasColumnType("nvarchar(max)");
        builder.Property(a => a.OccurredAt).HasColumnType("datetime2(7)");

        builder.HasIndex(a => new { a.TicketId, a.OccurredAt })
            .HasDatabaseName("IX_ActivityLogs_TicketId_OccurredAt");

        builder.HasIndex(a => new { a.ProjectId, a.OccurredAt })
            .HasDatabaseName("IX_ActivityLogs_ProjectId_OccurredAt");

        builder.HasIndex(a => new { a.ActorUserId, a.OccurredAt })
            .HasDatabaseName("IX_ActivityLogs_ActorUserId_OccurredAt");
    }
}

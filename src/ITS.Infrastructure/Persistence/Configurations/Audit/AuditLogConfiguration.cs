using ITS.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Audit;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", "audit");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("AuditId")
            .UseIdentityColumn<long>();

        builder.Property(a => a.Operation)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.OccurredAt).HasColumnType("datetime2(7)");
        builder.Property(a => a.ActorIpAddress).HasMaxLength(45);
        builder.Property(a => a.ActorUserAgent).HasMaxLength(512);
        builder.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityId).IsRequired().HasMaxLength(100);
        builder.Property(a => a.BeforeState).HasColumnType("nvarchar(max)");
        builder.Property(a => a.AfterState).HasColumnType("nvarchar(max)");
        builder.Property(a => a.FailureReason).HasMaxLength(1024);
        builder.Property(a => a.CorrelationId).HasMaxLength(100);

        builder.HasIndex(a => a.OccurredAt)
            .HasDatabaseName("IX_AuditLogs_OccurredAt");

        builder.HasIndex(a => new { a.ActorUserId, a.OccurredAt })
            .HasDatabaseName("IX_AuditLogs_ActorUserId_OccurredAt");

        builder.HasIndex(a => new { a.EntityType, a.EntityId, a.OccurredAt })
            .HasDatabaseName("IX_AuditLogs_EntityType_EntityId_OccurredAt");

        builder.HasIndex(a => new { a.Operation, a.OccurredAt })
            .HasDatabaseName("IX_AuditLogs_Operation_OccurredAt");
    }
}

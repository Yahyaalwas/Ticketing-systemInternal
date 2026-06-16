using ITS.Domain.Entities.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Tickets;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets", "tickets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("TicketId")
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(t => t.Title).IsRequired().HasMaxLength(500);
        builder.Property(t => t.Description).HasColumnType("nvarchar(max)");
        builder.Property(t => t.DescriptionHtml).HasColumnType("nvarchar(max)");
        builder.Property(t => t.StoryPoints).HasColumnType("decimal(5,1)");
        builder.Property(t => t.EstimatedHours).HasColumnType("decimal(8,2)");
        builder.Property(t => t.ActualHours).HasColumnType("decimal(8,2)");
        builder.Property(t => t.DueDate).HasColumnType("date");

        builder.Property(t => t.CreatedAt).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(t => t.UpdatedAt).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(t => t.DeletedAt).HasColumnType("datetime2(7)");
        builder.Property(t => t.ResolvedAt).HasColumnType("datetime2(7)");
        builder.Property(t => t.SlaBreachAt).HasColumnType("datetime2(7)");

        builder.Property(t => t.RowVersion).IsRowVersion().IsConcurrencyToken();

        // Unique constraint: ticket key is unique per project
        builder.HasIndex(t => new { t.ProjectId, t.TicketNumber })
            .IsUnique()
            .HasDatabaseName("UX_Tickets_ProjectNumber");

        // Kanban board primary query index
        builder.HasIndex(t => new { t.ProjectId, t.StatusId })
            .HasDatabaseName("IX_Tickets_ProjectId_Status")
            .IncludeProperties(t => new { t.Title, t.AssigneeUserId, t.PriorityId, t.DueDate, t.StoryPoints });

        builder.HasIndex(t => t.AssigneeUserId)
            .HasFilter("[IsDeleted] = 0 AND [AssigneeUserId] IS NOT NULL")
            .HasDatabaseName("IX_Tickets_AssigneeUserId");

        builder.HasIndex(t => t.ReporterUserId)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Tickets_ReporterUserId");

        builder.HasIndex(t => t.ParentTicketId)
            .HasFilter("[ParentTicketId] IS NOT NULL")
            .HasDatabaseName("IX_Tickets_ParentTicketId");

        builder.HasIndex(t => t.EpicTicketId)
            .HasFilter("[EpicTicketId] IS NOT NULL")
            .HasDatabaseName("IX_Tickets_EpicTicketId");

        builder.HasIndex(t => t.DueDate)
            .HasFilter("[IsDeleted] = 0 AND [DueDate] IS NOT NULL")
            .HasDatabaseName("IX_Tickets_DueDate");

        builder.HasIndex(t => t.SlaBreachAt)
            .HasFilter("[IsDeleted] = 0 AND [SlaBreachAt] IS NOT NULL")
            .HasDatabaseName("IX_Tickets_SlaBreachAt");

        builder.HasIndex(t => new { t.ProjectId, t.CreatedAt })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Tickets_CreatedAt");

        builder.HasIndex(t => t.UpdatedAt)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Tickets_UpdatedAt");

        // Self-referencing relationships
        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(t => t.ParentTicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(t => t.EpicTicketId)
            .OnDelete(DeleteBehavior.Restrict);

        // Collections
        builder.HasMany(t => t.Labels)
            .WithOne()
            .HasForeignKey(tl => tl.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Watchers)
            .WithOne()
            .HasForeignKey(tw => tw.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.OutboundLinks)
            .WithOne()
            .HasForeignKey(tl => tl.SourceTicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.InboundLinks)
            .WithOne()
            .HasForeignKey(tl => tl.TargetTicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.CustomFieldValues)
            .WithOne()
            .HasForeignKey(cfv => cfv.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global query filter for soft deletes
        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}

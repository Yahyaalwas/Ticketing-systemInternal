using ITS.Domain.Entities.Tickets;
using ITS.Domain.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Tickets;

public class CustomFieldValueConfiguration : IEntityTypeConfiguration<CustomFieldValue>
{
    public void Configure(EntityTypeBuilder<CustomFieldValue> builder)
    {
        builder.ToTable("CustomFieldValues", "tickets");
        builder.HasKey(cfv => cfv.Id);
        builder.Property(cfv => cfv.Id).HasColumnName("CustomFieldValueId").UseIdentityColumn<long>();

        builder.Property(cfv => cfv.ValueText).HasColumnType("nvarchar(max)");
        builder.Property(cfv => cfv.ValueNumber).HasColumnType("decimal(18,4)");
        builder.Property(cfv => cfv.ValueDate).HasColumnType("datetime2(7)");

        // Unique constraint: one value per field per ticket
        builder.HasIndex(cfv => new { cfv.TicketId, cfv.CustomFieldId })
            .IsUnique()
            .HasDatabaseName("UX_CustomFieldValues_TicketId_FieldId");

        builder.HasOne<CustomFieldDefinition>()
            .WithMany()
            .HasForeignKey(cfv => cfv.CustomFieldId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

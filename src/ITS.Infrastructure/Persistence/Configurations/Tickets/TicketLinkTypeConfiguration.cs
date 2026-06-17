using ITS.Domain.Entities.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Tickets;

public class TicketLinkTypeConfiguration : IEntityTypeConfiguration<TicketLinkType>
{
    public void Configure(EntityTypeBuilder<TicketLinkType> builder)
    {
        builder.ToTable("TicketLinkTypes", "tickets");
        builder.HasKey(lt => lt.Id);
        builder.Property(lt => lt.Id).HasColumnName("TicketLinkTypeId").UseIdentityColumn();

        builder.Property(lt => lt.Name).IsRequired().HasMaxLength(100);
        builder.Property(lt => lt.InwardName).IsRequired().HasMaxLength(100);
        builder.Property(lt => lt.OutwardName).IsRequired().HasMaxLength(100);

        builder.HasIndex(lt => lt.Name)
            .IsUnique()
            .HasDatabaseName("UX_TicketLinkTypes_Name");
    }
}

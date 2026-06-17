using ITS.Domain.Entities.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Tickets;

public class TicketLinkConfiguration : IEntityTypeConfiguration<TicketLink>
{
    public void Configure(EntityTypeBuilder<TicketLink> builder)
    {
        builder.ToTable("TicketLinks", "tickets");
        builder.HasKey(tl => tl.Id);
        builder.Property(tl => tl.Id).HasColumnName("TicketLinkId").HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(tl => tl.CreatedAt).HasColumnType("datetime2(7)").IsRequired();

        builder.HasIndex(tl => new { tl.SourceTicketId, tl.TargetTicketId, tl.LinkTypeId })
            .IsUnique()
            .HasDatabaseName("UX_TicketLinks_Source_Target_Type");

        builder.HasIndex(tl => tl.TargetTicketId)
            .HasDatabaseName("IX_TicketLinks_TargetTicketId");

        builder.HasOne(tl => tl.LinkType)
            .WithMany()
            .HasForeignKey(tl => tl.LinkTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

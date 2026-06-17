using ITS.Domain.Entities.Tickets;
using ITS.Domain.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Tickets;

public class TicketLabelConfiguration : IEntityTypeConfiguration<TicketLabel>
{
    public void Configure(EntityTypeBuilder<TicketLabel> builder)
    {
        builder.ToTable("TicketLabels", "tickets");
        builder.HasKey(tl => new { tl.TicketId, tl.LabelId });

        builder.HasOne<Label>()
            .WithMany()
            .HasForeignKey(tl => tl.LabelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

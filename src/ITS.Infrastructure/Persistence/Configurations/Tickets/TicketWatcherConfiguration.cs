using ITS.Domain.Entities.Tickets;
using ITS.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Tickets;

public class TicketWatcherConfiguration : IEntityTypeConfiguration<TicketWatcher>
{
    public void Configure(EntityTypeBuilder<TicketWatcher> builder)
    {
        builder.ToTable("TicketWatchers", "tickets");
        builder.HasKey(tw => new { tw.TicketId, tw.UserId });

        builder.Property(tw => tw.AddedAt).HasColumnType("datetime2(7)").IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(tw => tw.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

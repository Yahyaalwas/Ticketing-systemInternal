using ITS.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Content;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments", "content");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("CommentId").HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(c => c.Body).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(c => c.BodyHtml).HasColumnType("nvarchar(max)");

        builder.Property(c => c.CreatedAt).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(c => c.UpdatedAt).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(c => c.DeletedAt).HasColumnType("datetime2(7)");
        builder.Property(c => c.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(c => c.TicketId)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Comments_TicketId");

        builder.HasIndex(c => c.AuthorUserId)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Comments_AuthorUserId");

        builder.HasOne<ITS.Domain.Entities.Tickets.Ticket>()
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Comment>()
            .WithMany()
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.History)
            .WithOne()
            .HasForeignKey(ch => ch.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Mentions)
            .WithOne()
            .HasForeignKey(cm => cm.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Attachments)
            .WithOne()
            .HasForeignKey(a => a.CommentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

using ITS.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Content;

public class CommentHistoryConfiguration : IEntityTypeConfiguration<CommentHistory>
{
    public void Configure(EntityTypeBuilder<CommentHistory> builder)
    {
        builder.ToTable("CommentHistories", "content");
        builder.HasKey(ch => ch.Id);
        builder.Property(ch => ch.Id).HasColumnName("CommentHistoryId").UseIdentityColumn<long>();

        builder.Property(ch => ch.Body).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(ch => ch.EditedAt).HasColumnType("datetime2(7)").IsRequired();

        builder.HasIndex(ch => ch.CommentId)
            .HasDatabaseName("IX_CommentHistories_CommentId");
    }
}

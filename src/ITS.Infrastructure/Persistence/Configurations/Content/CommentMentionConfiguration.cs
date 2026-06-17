using ITS.Domain.Entities.Content;
using ITS.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Content;

public class CommentMentionConfiguration : IEntityTypeConfiguration<CommentMention>
{
    public void Configure(EntityTypeBuilder<CommentMention> builder)
    {
        builder.ToTable("CommentMentions", "content");
        builder.HasKey(cm => new { cm.CommentId, cm.MentionedUserId });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(cm => cm.MentionedUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

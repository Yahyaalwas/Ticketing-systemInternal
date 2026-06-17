using ITS.Domain.Entities.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Config;

public class ProjectSequenceConfiguration : IEntityTypeConfiguration<ProjectSequence>
{
    public void Configure(EntityTypeBuilder<ProjectSequence> builder)
    {
        builder.ToTable("ProjectSequences", "config");
        builder.HasKey(ps => ps.ProjectId);

        builder.Property(ps => ps.ProjectId).HasColumnName("ProjectId");
        builder.Property(ps => ps.CurrentNumber).IsRequired().HasDefaultValue(0);
        builder.Property(ps => ps.UpdatedAt).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
    }
}

using ITS.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Identity;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments", "identity");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("DepartmentId").UseIdentityColumn();

        builder.Property(d => d.Name).IsRequired().HasMaxLength(256);
        builder.Property(d => d.Description).HasMaxLength(1024);
        builder.Property(d => d.AdOuDistinguishedName).HasMaxLength(512);

        builder.Property(d => d.CreatedAt).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(d => d.UpdatedAt).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(d => d.DeletedAt).HasColumnType("datetime2(7)");
        builder.Property(d => d.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(d => d.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Departments_Name");

        builder.HasOne(d => d.ParentDepartment)
            .WithMany(d => d.ChildDepartments)
            .HasForeignKey(d => d.ParentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

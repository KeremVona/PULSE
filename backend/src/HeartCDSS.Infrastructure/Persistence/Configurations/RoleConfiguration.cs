using HeartCDSS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeartCDSS.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.RoleName)
            .HasColumnName("role_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(r => r.RoleName)
            .IsUnique()
            .HasDatabaseName("idx_roles_name");

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasMaxLength(255);

        builder.Property(r => r.MadeAt)
            .HasColumnName("made_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(r => r.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(r => r.DeletedAt)
            .HasColumnName("deleted_at");

        // Seed Core Roles
        builder.HasData(
            new Role(Guid.Parse("11111111-1111-1111-1111-111111111111"), Role.Physician, "Cardiologists and attending physicians with clinical order and assessment authority"),
            new Role(Guid.Parse("22222222-2222-2222-2222-222222222222"), Role.ClinicalStaff, "Nurses and medical assistants authorized to intake vitals and view patient rosters"),
            new Role(Guid.Parse("33333333-3333-3333-3333-333333333333"), Role.Admin, "System administrators managing user accounts and model registries"),
            new Role(Guid.Parse("44444444-4444-4444-4444-444444444444"), Role.Auditor, "Compliance auditors with read-only access to prediction logs and audit trails")
        );
    }
}

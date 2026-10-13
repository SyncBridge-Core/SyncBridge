using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Configurations;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUserRow>
{
    public void Configure(EntityTypeBuilder<AppUserRow> builder)
    {
        builder.ToTable("AppUser", "dbo");

        builder.HasKey(row => row.UserId)
            .HasName("PK_AppUser");

        builder.Property(row => row.UserId)
            .HasColumnType("nvarchar(64)")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(row => row.UserName)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(row => row.DisplayName)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(row => row.IsActive)
            .HasColumnType("bit")
            .IsRequired();

        builder.HasIndex(row => row.UserName)
            .IsUnique()
            .HasDatabaseName("UQ_AppUser_UserName");
    }
}

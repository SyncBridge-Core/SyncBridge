using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Configurations;

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<RecipeRow>
{
    public void Configure(EntityTypeBuilder<RecipeRow> builder)
    {
        builder.ToTable("Recipe", "dbo");

        builder.HasKey(row => row.RecipeId)
            .HasName("PK_Recipe");

        builder.Property(row => row.RecipeId)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();

        builder.Property(row => row.RecipeCode)
            .HasColumnType("nvarchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(row => row.Name)
            .HasColumnType("nvarchar(160)")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(row => row.Version)
            .HasColumnType("nvarchar(32)")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(row => row.IsApproved)
            .HasColumnType("bit")
            .IsRequired();

        builder.HasIndex(row => row.RecipeCode)
            .IsUnique()
            .HasDatabaseName("UQ_Recipe_RecipeCode");
    }
}

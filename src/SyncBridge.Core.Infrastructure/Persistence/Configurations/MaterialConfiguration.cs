using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Configurations;

internal sealed class MaterialConfiguration : IEntityTypeConfiguration<MaterialRow>
{
    public void Configure(EntityTypeBuilder<MaterialRow> builder)
    {
        builder.ToTable("Material", "dbo");

        builder.HasKey(row => row.MaterialId)
            .HasName("PK_Material");

        builder.Property(row => row.MaterialId)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();

        builder.Property(row => row.MaterialCode)
            .HasColumnType("nvarchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(row => row.Name)
            .HasColumnType("nvarchar(160)")
            .HasMaxLength(160)
            .IsRequired();

        builder.HasIndex(row => row.MaterialCode)
            .IsUnique()
            .HasDatabaseName("UQ_Material_MaterialCode");
    }
}

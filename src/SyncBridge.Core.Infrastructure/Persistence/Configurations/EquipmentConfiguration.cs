using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Configurations;

internal sealed class EquipmentConfiguration : IEntityTypeConfiguration<EquipmentRow>
{
    public void Configure(EntityTypeBuilder<EquipmentRow> builder)
    {
        builder.ToTable("Equipment", "dbo");

        builder.HasKey(row => row.EquipmentId)
            .HasName("PK_Equipment");

        builder.Property(row => row.EquipmentId)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();

        builder.Property(row => row.EquipmentCode)
            .HasColumnType("nvarchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(row => row.Name)
            .HasColumnType("nvarchar(160)")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(row => row.CalibrationDueDate)
            .HasColumnType("date")
            .IsRequired(false);

        builder.HasIndex(row => row.EquipmentCode)
            .IsUnique()
            .HasDatabaseName("UQ_Equipment_EquipmentCode");
    }
}

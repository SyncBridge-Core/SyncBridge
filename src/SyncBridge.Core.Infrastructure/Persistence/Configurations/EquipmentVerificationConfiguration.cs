using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Configurations;

internal sealed class EquipmentVerificationConfiguration : IEntityTypeConfiguration<EquipmentVerificationRow>
{
    public void Configure(EntityTypeBuilder<EquipmentVerificationRow> builder)
    {
        builder.ToTable("EquipmentVerification", "dbo");
        builder.HasKey(row => row.VerificationId).HasName("PK_EquipmentVerification");
        builder.Property(row => row.VerificationId).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(row => row.BatchId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(row => row.EquipmentId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(row => row.IsReady).HasColumnType("bit").IsRequired();
        builder.Property(row => row.CalibrationValid).HasColumnType("bit").IsRequired(false);
        builder.Property(row => row.VerifiedByUserId).HasColumnType("nvarchar(64)").HasMaxLength(64).IsRequired();
        builder.Property(row => row.VerifiedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(row => row.Comments).HasColumnType("nvarchar(1000)").HasMaxLength(1000).IsRequired(false);
        builder.HasIndex(row => new { row.BatchId, row.EquipmentId }).IsUnique()
            .HasDatabaseName("UQ_EquipmentVerification_BatchId_EquipmentId");
        builder.HasIndex(row => row.EquipmentId).HasDatabaseName("IX_EquipmentVerification_Equipment");
        builder.HasIndex(row => row.VerifiedByUserId).HasDatabaseName("IX_EquipmentVerification_VerifiedByUser");
        builder.HasOne<BatchRow>().WithMany().HasForeignKey(row => row.BatchId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EquipmentVerification_Batch");
        builder.HasOne<EquipmentRow>().WithMany().HasForeignKey(row => row.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EquipmentVerification_Equipment");
        builder.HasOne<AppUserRow>().WithMany().HasForeignKey(row => row.VerifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EquipmentVerification_AppUser");
    }
}

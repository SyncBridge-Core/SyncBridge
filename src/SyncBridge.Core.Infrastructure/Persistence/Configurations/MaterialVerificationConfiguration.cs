using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Configurations;

internal sealed class MaterialVerificationConfiguration : IEntityTypeConfiguration<MaterialVerificationRow>
{
    public void Configure(EntityTypeBuilder<MaterialVerificationRow> builder)
    {
        builder.ToTable("MaterialVerification", "dbo");
        builder.HasKey(row => row.VerificationId).HasName("PK_MaterialVerification");
        builder.Property(row => row.VerificationId).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(row => row.BatchId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(row => row.MaterialId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(row => row.LotNumber).HasColumnType("nvarchar(80)").HasMaxLength(80).IsRequired(false);
        builder.Property(row => row.IsAccepted).HasColumnType("bit").IsRequired();
        builder.Property(row => row.VerifiedByUserId).HasColumnType("nvarchar(64)").HasMaxLength(64).IsRequired();
        builder.Property(row => row.VerifiedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(row => row.Comments).HasColumnType("nvarchar(1000)").HasMaxLength(1000).IsRequired(false);
        builder.HasIndex(row => new { row.BatchId, row.MaterialId }).IsUnique()
            .HasDatabaseName("UQ_MaterialVerification_BatchId_MaterialId");
        builder.HasIndex(row => row.MaterialId).HasDatabaseName("IX_MaterialVerification_Material");
        builder.HasIndex(row => row.VerifiedByUserId).HasDatabaseName("IX_MaterialVerification_VerifiedByUser");
        builder.HasOne<BatchRow>().WithMany().HasForeignKey(row => row.BatchId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_MaterialVerification_Batch");
        builder.HasOne<MaterialRow>().WithMany().HasForeignKey(row => row.MaterialId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_MaterialVerification_Material");
        builder.HasOne<AppUserRow>().WithMany().HasForeignKey(row => row.VerifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_MaterialVerification_AppUser");
    }
}

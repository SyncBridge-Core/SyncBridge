using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBridge.Core.Infrastructure.Persistence.Conversion;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Configurations;

internal sealed class ElectronicSignatureConfiguration : IEntityTypeConfiguration<ElectronicSignatureRow>
{
    public void Configure(EntityTypeBuilder<ElectronicSignatureRow> builder)
    {
        builder.ToTable("ElectronicSignature", "dbo");
        builder.HasKey(row => row.SignatureId).HasName("PK_ElectronicSignature");
        builder.Property(row => row.SignatureId).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(row => row.BatchId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(row => row.RelatedRecordType).HasColumnType("nvarchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(row => row.RelatedRecordId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(row => row.UserId).HasColumnType("nvarchar(64)").HasMaxLength(64).IsRequired();
        builder.Property(row => row.Meaning).HasConversion(DbdsEnumConverters.ElectronicSignatureMeaning)
            .HasColumnType("nvarchar(200)").HasMaxLength(200).IsRequired();
        builder.Property(row => row.SignedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.HasIndex(row => row.BatchId).HasDatabaseName("IX_ElectronicSignature_Batch");
        builder.HasIndex(row => row.UserId).HasDatabaseName("IX_ElectronicSignature_User");
        builder.HasOne<BatchRow>().WithMany().HasForeignKey(row => row.BatchId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ElectronicSignature_Batch");
        builder.HasOne<AppUserRow>().WithMany().HasForeignKey(row => row.UserId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ElectronicSignature_AppUser");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBridge.Core.Infrastructure.Persistence.Conversion;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Configurations;

internal sealed class BatchConfiguration : IEntityTypeConfiguration<BatchRow>
{
    public void Configure(EntityTypeBuilder<BatchRow> builder)
    {
        builder.ToTable("Batch", "dbo");
        builder.HasKey(row => row.BatchId).HasName("PK_Batch");
        builder.Property(row => row.BatchId).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(row => row.BatchNumber).HasColumnType("nvarchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(row => row.OrderId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(row => row.Status).HasConversion(DbdsEnumConverters.BatchStatus)
            .HasColumnType("nvarchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(row => row.CreatedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(row => row.StartedAt).HasColumnType("datetimeoffset(7)").IsRequired(false);
        builder.Property(row => row.CompletedAt).HasColumnType("datetimeoffset(7)").IsRequired(false);
        builder.Property(row => row.FinalDisposition).HasColumnType("nvarchar(1000)").HasMaxLength(1000).IsRequired(false);
        builder.HasIndex(row => row.BatchNumber).IsUnique().HasDatabaseName("UQ_Batch_BatchNumber");
        builder.HasIndex(row => row.Status).HasDatabaseName("IX_Batch_Status");
        builder.HasIndex(row => row.OrderId).HasDatabaseName("IX_Batch_ProductionOrder");
        builder.HasOne<ProductionOrderRow>().WithMany().HasForeignKey(row => row.OrderId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Batch_ProductionOrder");
    }
}

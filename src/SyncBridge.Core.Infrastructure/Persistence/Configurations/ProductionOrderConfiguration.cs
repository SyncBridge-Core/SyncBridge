using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBridge.Core.Infrastructure.Persistence.Conversion;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Configurations;

internal sealed class ProductionOrderConfiguration : IEntityTypeConfiguration<ProductionOrderRow>
{
    public void Configure(EntityTypeBuilder<ProductionOrderRow> builder)
    {
        builder.ToTable("ProductionOrder", "dbo");
        builder.HasKey(row => row.OrderId).HasName("PK_ProductionOrder");
        builder.Property(row => row.OrderId).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(row => row.OrderNumber).HasColumnType("nvarchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(row => row.ProductCode).HasColumnType("nvarchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(row => row.ProductName).HasColumnType("nvarchar(160)").HasMaxLength(160).IsRequired();
        builder.Property(row => row.RecipeId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(row => row.Quantity).HasColumnType("decimal(18,3)").HasPrecision(18, 3).IsRequired();
        builder.Property(row => row.Status).HasConversion(DbdsEnumConverters.ProductionOrderStatus)
            .HasColumnType("nvarchar(30)").HasMaxLength(30).IsRequired();
        builder.Property(row => row.CreatedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.HasIndex(row => row.OrderNumber).IsUnique().HasDatabaseName("UQ_ProductionOrder_OrderNumber");
        builder.HasIndex(row => row.RecipeId).HasDatabaseName("IX_ProductionOrder_Recipe");
        builder.HasOne<RecipeRow>().WithMany().HasForeignKey(row => row.RecipeId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ProductionOrder_Recipe");
    }
}

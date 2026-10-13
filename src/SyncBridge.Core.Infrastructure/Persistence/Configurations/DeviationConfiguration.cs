using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBridge.Core.Infrastructure.Persistence.Conversion;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Configurations;

internal sealed class DeviationConfiguration : IEntityTypeConfiguration<DeviationRow>
{
    public void Configure(EntityTypeBuilder<DeviationRow> builder)
    {
        builder.ToTable("Deviation", "dbo");
        builder.HasKey(row => row.DeviationId).HasName("PK_Deviation");
        builder.Property(row => row.DeviationId).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(row => row.DeviationCode).HasColumnType("nvarchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(row => row.BatchId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(row => row.StepExecutionId).HasColumnType("uniqueidentifier").IsRequired(false);
        builder.Property(row => row.Description).HasColumnType("nvarchar(2000)").HasMaxLength(2000).IsRequired();
        builder.Property(row => row.Status).HasConversion(DbdsEnumConverters.DeviationStatus)
            .HasColumnType("nvarchar(30)").HasMaxLength(30).IsRequired();
        builder.Property(row => row.ReportedByUserId).HasColumnType("nvarchar(64)").HasMaxLength(64).IsRequired();
        builder.Property(row => row.OpenedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(row => row.ReviewComments).HasColumnType("nvarchar(2000)").HasMaxLength(2000).IsRequired(false);
        builder.Property(row => row.FinalDisposition).HasColumnType("nvarchar(1000)").HasMaxLength(1000).IsRequired(false);
        builder.Property(row => row.ResolvedAt).HasColumnType("datetimeoffset(7)").IsRequired(false);
        builder.HasIndex(row => row.DeviationCode).IsUnique().HasDatabaseName("UQ_Deviation_DeviationCode");
        builder.HasIndex(row => new { row.BatchId, row.Status }).HasDatabaseName("IX_Deviation_Batch_Status");
        builder.HasIndex(row => row.StepExecutionId).HasDatabaseName("IX_Deviation_StepExecution");
        builder.HasIndex(row => row.ReportedByUserId).HasDatabaseName("IX_Deviation_ReportedByUser");
        builder.HasOne<BatchRow>().WithMany().HasForeignKey(row => row.BatchId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Deviation_Batch");
        builder.HasOne<BatchStepExecutionRow>().WithMany().HasForeignKey(row => row.StepExecutionId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Deviation_BatchStepExecution");
        builder.HasOne<AppUserRow>().WithMany().HasForeignKey(row => row.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Deviation_AppUser");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBridge.Core.Infrastructure.Persistence.Conversion;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Configurations;

internal sealed class BatchStepExecutionConfiguration : IEntityTypeConfiguration<BatchStepExecutionRow>
{
    public void Configure(EntityTypeBuilder<BatchStepExecutionRow> builder)
    {
        builder.ToTable("BatchStepExecution", "dbo");
        builder.HasKey(row => row.StepExecutionId).HasName("PK_BatchStepExecution");
        builder.Property(row => row.StepExecutionId).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(row => row.BatchId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(row => row.RecipeStepId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(row => row.StepNumber).HasColumnType("int").IsRequired();
        builder.Property(row => row.Status).HasConversion(DbdsEnumConverters.StepExecutionStatus)
            .HasColumnType("nvarchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(row => row.StartedAt).HasColumnType("datetimeoffset(7)").IsRequired(false);
        builder.Property(row => row.CompletedAt).HasColumnType("datetimeoffset(7)").IsRequired(false);
        builder.Property(row => row.CompletedByUserId).HasColumnType("nvarchar(64)").HasMaxLength(64).IsRequired(false);
        builder.Property(row => row.RecordedData).HasColumnType("nvarchar(2000)").HasMaxLength(2000).IsRequired(false);
        builder.HasIndex(row => new { row.BatchId, row.StepNumber }).IsUnique()
            .HasDatabaseName("UQ_BatchStepExecution_BatchId_StepNumber");
        builder.HasIndex(row => row.RecipeStepId).HasDatabaseName("IX_BatchStepExecution_RecipeStep");
        builder.HasIndex(row => row.CompletedByUserId).HasDatabaseName("IX_BatchStepExecution_CompletedByUser");
        builder.HasOne<BatchRow>().WithMany().HasForeignKey(row => row.BatchId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_BatchStepExecution_Batch");
        builder.HasOne<RecipeStepRow>().WithMany().HasForeignKey(row => row.RecipeStepId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_BatchStepExecution_RecipeStep");
        builder.HasOne<AppUserRow>().WithMany().HasForeignKey(row => row.CompletedByUserId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_BatchStepExecution_AppUser");
    }
}

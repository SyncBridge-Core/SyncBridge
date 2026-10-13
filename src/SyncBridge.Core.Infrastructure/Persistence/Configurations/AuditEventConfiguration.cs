using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBridge.Core.Infrastructure.Persistence.Conversion;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Configurations;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEventRow>
{
    public void Configure(EntityTypeBuilder<AuditEventRow> builder)
    {
        builder.ToTable("AuditEvent", "dbo");
        builder.HasKey(row => row.AuditEventId).HasName("PK_AuditEvent");
        builder.Property(row => row.AuditEventId).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(row => row.BatchId).HasColumnType("uniqueidentifier").IsRequired(false);
        builder.Property(row => row.UserId).HasColumnType("nvarchar(64)").HasMaxLength(64).IsRequired();
        builder.Property(row => row.Action).HasConversion(DbdsEnumConverters.AuditEventType)
            .HasColumnType("nvarchar(100)").HasMaxLength(100).IsRequired();
        builder.Property(row => row.AffectedEntity).HasColumnType("nvarchar(60)").HasMaxLength(60).IsRequired();
        builder.Property(row => row.AffectedEntityId).HasColumnType("nvarchar(256)").HasMaxLength(256).IsRequired();
        builder.Property(row => row.OccurredAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(row => row.ResultingState).HasColumnType("nvarchar(60)").HasMaxLength(60).IsRequired(false);
        builder.Property(row => row.Details).HasColumnType("nvarchar(2000)").HasMaxLength(2000).IsRequired(false);
        builder.HasIndex(row => new { row.BatchId, row.OccurredAt }).HasDatabaseName("IX_AuditEvent_Batch_Time");
        builder.HasIndex(row => row.UserId).HasDatabaseName("IX_AuditEvent_User");
        builder.HasOne<BatchRow>().WithMany().HasForeignKey(row => row.BatchId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_AuditEvent_Batch");
        builder.HasOne<AppUserRow>().WithMany().HasForeignKey(row => row.UserId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_AuditEvent_AppUser");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Configurations;

internal sealed class RecipeEquipmentRequirementConfiguration :
    IEntityTypeConfiguration<RecipeEquipmentRequirementRow>
{
    public void Configure(EntityTypeBuilder<RecipeEquipmentRequirementRow> builder)
    {
        builder.ToTable("RecipeEquipmentRequirement", "dbo");

        builder.HasKey(row => new { row.RecipeId, row.EquipmentId })
            .HasName("PK_RecipeEquipmentRequirement");

        builder.Property(row => row.RecipeId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();

        builder.Property(row => row.EquipmentId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();

        builder.HasIndex(row => row.EquipmentId)
            .HasDatabaseName("IX_RecipeEquipmentRequirement_Equipment");

        builder.HasOne<RecipeRow>()
            .WithMany()
            .HasForeignKey(row => row.RecipeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RecipeEquipmentRequirement_Recipe");

        builder.HasOne<EquipmentRow>()
            .WithMany()
            .HasForeignKey(row => row.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RecipeEquipmentRequirement_Equipment");
    }
}

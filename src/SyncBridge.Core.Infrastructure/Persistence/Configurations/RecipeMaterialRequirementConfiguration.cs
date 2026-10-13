using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Configurations;

internal sealed class RecipeMaterialRequirementConfiguration :
    IEntityTypeConfiguration<RecipeMaterialRequirementRow>
{
    public void Configure(EntityTypeBuilder<RecipeMaterialRequirementRow> builder)
    {
        builder.ToTable("RecipeMaterialRequirement", "dbo");

        builder.HasKey(row => new { row.RecipeId, row.MaterialId })
            .HasName("PK_RecipeMaterialRequirement");

        builder.Property(row => row.RecipeId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();

        builder.Property(row => row.MaterialId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();

        builder.Property(row => row.RequiredQuantity)
            .HasColumnType("decimal(18,3)")
            .HasPrecision(18, 3)
            .IsRequired(false);

        builder.HasIndex(row => row.MaterialId)
            .HasDatabaseName("IX_RecipeMaterialRequirement_Material");

        builder.HasOne<RecipeRow>()
            .WithMany()
            .HasForeignKey(row => row.RecipeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RecipeMaterialRequirement_Recipe");

        builder.HasOne<MaterialRow>()
            .WithMany()
            .HasForeignKey(row => row.MaterialId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RecipeMaterialRequirement_Material");
    }
}

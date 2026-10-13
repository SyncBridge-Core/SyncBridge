using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Configurations;

internal sealed class RecipeStepConfiguration : IEntityTypeConfiguration<RecipeStepRow>
{
    public void Configure(EntityTypeBuilder<RecipeStepRow> builder)
    {
        builder.ToTable("RecipeStep", "dbo");

        builder.HasKey(row => row.RecipeStepId)
            .HasName("PK_RecipeStep");

        builder.Property(row => row.RecipeStepId)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();

        builder.Property(row => row.RecipeId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();

        builder.Property(row => row.StepNumber)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(row => row.Name)
            .HasColumnType("nvarchar(160)")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(row => row.Instructions)
            .HasColumnType("nvarchar(2000)")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(row => row.RequiresSignature)
            .HasColumnType("bit")
            .IsRequired();

        builder.HasIndex(row => new { row.RecipeId, row.StepNumber })
            .IsUnique()
            .HasDatabaseName("UQ_RecipeStep_RecipeId_StepNumber");

        builder.HasOne<RecipeRow>()
            .WithMany()
            .HasForeignKey(row => row.RecipeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RecipeStep_Recipe");
    }
}

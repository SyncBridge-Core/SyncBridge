using Microsoft.EntityFrameworkCore;
using SyncBridge.Core.Infrastructure.Persistence.Configurations;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence;

/// <summary>
/// Provides the Entity Framework Core session for SyncBridge Core persistence.
/// </summary>
public sealed class SyncBridgeDbContext : DbContext
{
    /// <summary>
    /// Initializes a new SyncBridge Core database context.
    /// </summary>
    /// <param name="options">The configured database context options.</param>
    public SyncBridgeDbContext(DbContextOptions<SyncBridgeDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the application-user identities.
    /// </summary>
    public DbSet<AppUserRow> AppUsers => Set<AppUserRow>();

    /// <summary>
    /// Gets the recipe definitions.
    /// </summary>
    public DbSet<RecipeRow> Recipes => Set<RecipeRow>();

    /// <summary>
    /// Gets the ordered recipe steps.
    /// </summary>
    public DbSet<RecipeStepRow> RecipeSteps => Set<RecipeStepRow>();

    /// <summary>
    /// Gets the recipe material requirements.
    /// </summary>
    public DbSet<RecipeMaterialRequirementRow> RecipeMaterialRequirements =>
        Set<RecipeMaterialRequirementRow>();

    /// <summary>
    /// Gets the recipe equipment requirements.
    /// </summary>
    public DbSet<RecipeEquipmentRequirementRow> RecipeEquipmentRequirements =>
        Set<RecipeEquipmentRequirementRow>();

    /// <summary>
    /// Gets the material reference records.
    /// </summary>
    public DbSet<MaterialRow> Materials => Set<MaterialRow>();

    /// <summary>
    /// Gets the equipment reference records.
    /// </summary>
    public DbSet<EquipmentRow> Equipment => Set<EquipmentRow>();

    /// <summary>Gets the production orders.</summary>
    public DbSet<ProductionOrderRow> ProductionOrders => Set<ProductionOrderRow>();

    /// <summary>Gets the production batches.</summary>
    public DbSet<BatchRow> Batches => Set<BatchRow>();

    /// <summary>Gets the batch step executions.</summary>
    public DbSet<BatchStepExecutionRow> BatchStepExecutions => Set<BatchStepExecutionRow>();

    /// <summary>Gets the material-verification evidence.</summary>
    public DbSet<MaterialVerificationRow> MaterialVerifications => Set<MaterialVerificationRow>();

    /// <summary>Gets the equipment-verification evidence.</summary>
    public DbSet<EquipmentVerificationRow> EquipmentVerifications => Set<EquipmentVerificationRow>();

    /// <summary>Gets the manufacturing deviations.</summary>
    public DbSet<DeviationRow> Deviations => Set<DeviationRow>();

    /// <summary>Gets the immutable electronic signatures.</summary>
    public DbSet<ElectronicSignatureRow> ElectronicSignatures => Set<ElectronicSignatureRow>();

    /// <summary>Gets the append-only audit events.</summary>
    public DbSet<AuditEventRow> AuditEvents => Set<AuditEventRow>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AppUserConfiguration());
        modelBuilder.ApplyConfiguration(new RecipeConfiguration());
        modelBuilder.ApplyConfiguration(new RecipeStepConfiguration());
        modelBuilder.ApplyConfiguration(new RecipeMaterialRequirementConfiguration());
        modelBuilder.ApplyConfiguration(new RecipeEquipmentRequirementConfiguration());
        modelBuilder.ApplyConfiguration(new MaterialConfiguration());
        modelBuilder.ApplyConfiguration(new EquipmentConfiguration());
        modelBuilder.ApplyConfiguration(new ProductionOrderConfiguration());
        modelBuilder.ApplyConfiguration(new BatchConfiguration());
        modelBuilder.ApplyConfiguration(new BatchStepExecutionConfiguration());
        modelBuilder.ApplyConfiguration(new MaterialVerificationConfiguration());
        modelBuilder.ApplyConfiguration(new EquipmentVerificationConfiguration());
        modelBuilder.ApplyConfiguration(new DeviationConfiguration());
        modelBuilder.ApplyConfiguration(new ElectronicSignatureConfiguration());
        modelBuilder.ApplyConfiguration(new AuditEventConfiguration());
    }
}

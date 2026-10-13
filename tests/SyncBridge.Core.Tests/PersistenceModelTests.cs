using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using SyncBridge.Core.Application.Contracts.Persistence;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Infrastructure.Persistence;
using SyncBridge.Core.Infrastructure.Persistence.Exceptions;
using SyncBridge.Core.Infrastructure.Persistence.Rows;
using SyncBridge.Core.Infrastructure.Persistence.Seeding;

namespace SyncBridge.Core.Tests;

public sealed class PersistenceModelTests
{
    private const string MetadataOnlyConnectionString =
        "Server=not-used.invalid;Database=SyncBridgeCore;Integrated Security=True;TrustServerCertificate=True";

    [Fact]
    public void Model_ContainsExactlyFifteenDbdsTablesInDboSchema()
    {
        using var context = CreateContext();

        var tables = context.Model.GetEntityTypes()
            .Select(entityType => (entityType.GetTableName(), entityType.GetSchema()))
            .OrderBy(table => table.Item1)
            .ToArray();

        Assert.Equal(15, tables.Length);
        Assert.Equal(
            [
                ("AppUser", "dbo"),
                ("AuditEvent", "dbo"),
                ("Batch", "dbo"),
                ("BatchStepExecution", "dbo"),
                ("Deviation", "dbo"),
                ("ElectronicSignature", "dbo"),
                ("Equipment", "dbo"),
                ("EquipmentVerification", "dbo"),
                ("Material", "dbo"),
                ("MaterialVerification", "dbo"),
                ("ProductionOrder", "dbo"),
                ("Recipe", "dbo"),
                ("RecipeEquipmentRequirement", "dbo"),
                ("RecipeMaterialRequirement", "dbo"),
                ("RecipeStep", "dbo")
            ],
            tables);
    }

    [Fact]
    public void ProductionOrderMapping_MatchesDbds()
    {
        using var context = CreateContext();
        var entity = GetEntity<ProductionOrderRow>(context);

        AssertPrimaryKey(entity, "PK_ProductionOrder", nameof(ProductionOrderRow.OrderId));
        AssertProperty(entity, nameof(ProductionOrderRow.OrderId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(ProductionOrderRow.OrderNumber), "nvarchar(40)", false, 40);
        AssertProperty(entity, nameof(ProductionOrderRow.ProductCode), "nvarchar(40)", false, 40);
        AssertProperty(entity, nameof(ProductionOrderRow.ProductName), "nvarchar(160)", false, 160);
        AssertProperty(entity, nameof(ProductionOrderRow.RecipeId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(ProductionOrderRow.Quantity), "decimal(18,3)", false, precision: 18, scale: 3);
        AssertProperty(entity, nameof(ProductionOrderRow.Status), "nvarchar(30)", false, 30);
        AssertProperty(entity, nameof(ProductionOrderRow.CreatedAt), "datetimeoffset(7)", false);
        AssertIndex(entity, "UQ_ProductionOrder_OrderNumber", true, nameof(ProductionOrderRow.OrderNumber));
        AssertForeignKey(entity, "FK_ProductionOrder_Recipe", typeof(RecipeRow), nameof(ProductionOrderRow.RecipeId));
    }

    [Fact]
    public void BatchMapping_MatchesDbdsAndContainsNoQuantityOrUnitColumn()
    {
        using var context = CreateContext();
        var entity = GetEntity<BatchRow>(context);

        AssertPrimaryKey(entity, "PK_Batch", nameof(BatchRow.BatchId));
        AssertProperty(entity, nameof(BatchRow.BatchId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(BatchRow.BatchNumber), "nvarchar(40)", false, 40);
        AssertProperty(entity, nameof(BatchRow.OrderId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(BatchRow.Status), "nvarchar(20)", false, 20);
        AssertProperty(entity, nameof(BatchRow.CreatedAt), "datetimeoffset(7)", false);
        AssertProperty(entity, nameof(BatchRow.StartedAt), "datetimeoffset(7)", true);
        AssertProperty(entity, nameof(BatchRow.CompletedAt), "datetimeoffset(7)", true);
        AssertProperty(entity, nameof(BatchRow.FinalDisposition), "nvarchar(1000)", true, 1000);
        Assert.Null(entity.FindProperty("Quantity"));
        Assert.Null(entity.FindProperty("UnitOfMeasure"));
        AssertIndex(entity, "UQ_Batch_BatchNumber", true, nameof(BatchRow.BatchNumber));
        AssertIndex(entity, "IX_Batch_Status", false, nameof(BatchRow.Status));
        AssertForeignKey(entity, "FK_Batch_ProductionOrder", typeof(ProductionOrderRow), nameof(BatchRow.OrderId));
    }

    [Fact]
    public void BatchStepExecutionMapping_MatchesDbds()
    {
        using var context = CreateContext();
        var entity = GetEntity<BatchStepExecutionRow>(context);

        AssertPrimaryKey(entity, "PK_BatchStepExecution", nameof(BatchStepExecutionRow.StepExecutionId));
        AssertProperty(entity, nameof(BatchStepExecutionRow.StepExecutionId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(BatchStepExecutionRow.BatchId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(BatchStepExecutionRow.RecipeStepId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(BatchStepExecutionRow.StepNumber), "int", false);
        AssertProperty(entity, nameof(BatchStepExecutionRow.Status), "nvarchar(20)", false, 20);
        AssertProperty(entity, nameof(BatchStepExecutionRow.StartedAt), "datetimeoffset(7)", true);
        AssertProperty(entity, nameof(BatchStepExecutionRow.CompletedAt), "datetimeoffset(7)", true);
        AssertProperty(entity, nameof(BatchStepExecutionRow.CompletedByUserId), "nvarchar(64)", true, 64);
        AssertProperty(entity, nameof(BatchStepExecutionRow.RecordedData), "nvarchar(2000)", true, 2000);
        AssertIndex(entity, "UQ_BatchStepExecution_BatchId_StepNumber", true,
            nameof(BatchStepExecutionRow.BatchId), nameof(BatchStepExecutionRow.StepNumber));
        AssertForeignKey(entity, "FK_BatchStepExecution_Batch", typeof(BatchRow), nameof(BatchStepExecutionRow.BatchId));
        AssertForeignKey(entity, "FK_BatchStepExecution_RecipeStep", typeof(RecipeStepRow), nameof(BatchStepExecutionRow.RecipeStepId));
        AssertForeignKey(entity, "FK_BatchStepExecution_AppUser", typeof(AppUserRow), nameof(BatchStepExecutionRow.CompletedByUserId));
    }

    [Fact]
    public void MaterialVerificationMapping_MatchesDbds()
    {
        using var context = CreateContext();
        var entity = GetEntity<MaterialVerificationRow>(context);

        AssertPrimaryKey(entity, "PK_MaterialVerification", nameof(MaterialVerificationRow.VerificationId));
        AssertProperty(entity, nameof(MaterialVerificationRow.VerificationId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(MaterialVerificationRow.BatchId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(MaterialVerificationRow.MaterialId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(MaterialVerificationRow.LotNumber), "nvarchar(80)", true, 80);
        AssertProperty(entity, nameof(MaterialVerificationRow.IsAccepted), "bit", false);
        AssertProperty(entity, nameof(MaterialVerificationRow.VerifiedByUserId), "nvarchar(64)", false, 64);
        AssertProperty(entity, nameof(MaterialVerificationRow.VerifiedAt), "datetimeoffset(7)", false);
        AssertProperty(entity, nameof(MaterialVerificationRow.Comments), "nvarchar(1000)", true, 1000);
        AssertIndex(entity, "UQ_MaterialVerification_BatchId_MaterialId", true,
            nameof(MaterialVerificationRow.BatchId), nameof(MaterialVerificationRow.MaterialId));
        AssertForeignKey(entity, "FK_MaterialVerification_Batch", typeof(BatchRow), nameof(MaterialVerificationRow.BatchId));
        AssertForeignKey(entity, "FK_MaterialVerification_Material", typeof(MaterialRow), nameof(MaterialVerificationRow.MaterialId));
        AssertForeignKey(entity, "FK_MaterialVerification_AppUser", typeof(AppUserRow), nameof(MaterialVerificationRow.VerifiedByUserId));
    }

    [Fact]
    public void EquipmentVerificationMapping_MatchesDbds()
    {
        using var context = CreateContext();
        var entity = GetEntity<EquipmentVerificationRow>(context);

        AssertPrimaryKey(entity, "PK_EquipmentVerification", nameof(EquipmentVerificationRow.VerificationId));
        AssertProperty(entity, nameof(EquipmentVerificationRow.BatchId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(EquipmentVerificationRow.EquipmentId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(EquipmentVerificationRow.IsReady), "bit", false);
        AssertProperty(entity, nameof(EquipmentVerificationRow.CalibrationValid), "bit", true);
        AssertProperty(entity, nameof(EquipmentVerificationRow.VerifiedByUserId), "nvarchar(64)", false, 64);
        AssertProperty(entity, nameof(EquipmentVerificationRow.VerifiedAt), "datetimeoffset(7)", false);
        AssertProperty(entity, nameof(EquipmentVerificationRow.Comments), "nvarchar(1000)", true, 1000);
        AssertIndex(entity, "UQ_EquipmentVerification_BatchId_EquipmentId", true,
            nameof(EquipmentVerificationRow.BatchId), nameof(EquipmentVerificationRow.EquipmentId));
        AssertForeignKey(entity, "FK_EquipmentVerification_Batch", typeof(BatchRow), nameof(EquipmentVerificationRow.BatchId));
        AssertForeignKey(entity, "FK_EquipmentVerification_Equipment", typeof(EquipmentRow), nameof(EquipmentVerificationRow.EquipmentId));
        AssertForeignKey(entity, "FK_EquipmentVerification_AppUser", typeof(AppUserRow), nameof(EquipmentVerificationRow.VerifiedByUserId));
    }

    [Fact]
    public void DeviationMapping_ReflectsDbdsAmendmentOne()
    {
        using var context = CreateContext();
        var entity = GetEntity<DeviationRow>(context);

        AssertPrimaryKey(entity, "PK_Deviation", nameof(DeviationRow.DeviationId));
        AssertProperty(entity, nameof(DeviationRow.DeviationCode), "nvarchar(40)", false, 40);
        AssertProperty(entity, nameof(DeviationRow.BatchId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(DeviationRow.StepExecutionId), "uniqueidentifier", true);
        AssertProperty(entity, nameof(DeviationRow.Description), "nvarchar(2000)", false, 2000);
        AssertProperty(entity, nameof(DeviationRow.Status), "nvarchar(30)", false, 30);
        AssertProperty(entity, nameof(DeviationRow.ReportedByUserId), "nvarchar(64)", false, 64);
        AssertProperty(entity, nameof(DeviationRow.OpenedAt), "datetimeoffset(7)", false);
        AssertProperty(entity, nameof(DeviationRow.ReviewComments), "nvarchar(2000)", true, 2000);
        AssertProperty(entity, nameof(DeviationRow.FinalDisposition), "nvarchar(1000)", true, 1000);
        AssertProperty(entity, nameof(DeviationRow.ResolvedAt), "datetimeoffset(7)", true);
        AssertIndex(entity, "UQ_Deviation_DeviationCode", true, nameof(DeviationRow.DeviationCode));
        AssertIndex(entity, "IX_Deviation_Batch_Status", false, nameof(DeviationRow.BatchId), nameof(DeviationRow.Status));
        AssertForeignKey(entity, "FK_Deviation_Batch", typeof(BatchRow), nameof(DeviationRow.BatchId));
        AssertForeignKey(entity, "FK_Deviation_BatchStepExecution", typeof(BatchStepExecutionRow), nameof(DeviationRow.StepExecutionId));
        AssertForeignKey(entity, "FK_Deviation_AppUser", typeof(AppUserRow), nameof(DeviationRow.ReportedByUserId));
    }

    [Fact]
    public void ElectronicSignatureMapping_MatchesDbdsWithoutPolymorphicForeignKey()
    {
        using var context = CreateContext();
        var entity = GetEntity<ElectronicSignatureRow>(context);

        AssertPrimaryKey(entity, "PK_ElectronicSignature", nameof(ElectronicSignatureRow.SignatureId));
        AssertProperty(entity, nameof(ElectronicSignatureRow.BatchId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(ElectronicSignatureRow.RelatedRecordType), "nvarchar(40)", false, 40);
        AssertProperty(entity, nameof(ElectronicSignatureRow.RelatedRecordId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(ElectronicSignatureRow.UserId), "nvarchar(64)", false, 64);
        AssertProperty(entity, nameof(ElectronicSignatureRow.Meaning), "nvarchar(200)", false, 200);
        AssertProperty(entity, nameof(ElectronicSignatureRow.SignedAt), "datetimeoffset(7)", false);
        Assert.Equal(2, entity.GetForeignKeys().Count());
        AssertForeignKey(entity, "FK_ElectronicSignature_Batch", typeof(BatchRow), nameof(ElectronicSignatureRow.BatchId));
        AssertForeignKey(entity, "FK_ElectronicSignature_AppUser", typeof(AppUserRow), nameof(ElectronicSignatureRow.UserId));
    }

    [Fact]
    public void AuditEventMapping_ReflectsDbdsAmendmentTwo()
    {
        using var context = CreateContext();
        var entity = GetEntity<AuditEventRow>(context);

        AssertPrimaryKey(entity, "PK_AuditEvent", nameof(AuditEventRow.AuditEventId));
        AssertProperty(entity, nameof(AuditEventRow.BatchId), "uniqueidentifier", true);
        AssertProperty(entity, nameof(AuditEventRow.UserId), "nvarchar(64)", false, 64);
        AssertProperty(entity, nameof(AuditEventRow.Action), "nvarchar(100)", false, 100);
        AssertProperty(entity, nameof(AuditEventRow.AffectedEntity), "nvarchar(60)", false, 60);
        AssertProperty(entity, nameof(AuditEventRow.AffectedEntityId), "nvarchar(256)", false, 256);
        AssertProperty(entity, nameof(AuditEventRow.OccurredAt), "datetimeoffset(7)", false);
        AssertProperty(entity, nameof(AuditEventRow.ResultingState), "nvarchar(60)", true, 60);
        AssertProperty(entity, nameof(AuditEventRow.Details), "nvarchar(2000)", true, 2000);
        AssertIndex(entity, "IX_AuditEvent_Batch_Time", false, nameof(AuditEventRow.BatchId), nameof(AuditEventRow.OccurredAt));
        AssertForeignKey(entity, "FK_AuditEvent_Batch", typeof(BatchRow), nameof(AuditEventRow.BatchId));
        AssertForeignKey(entity, "FK_AuditEvent_AppUser", typeof(AppUserRow), nameof(AuditEventRow.UserId));
    }

    [Fact]
    public void EnumMappings_UseApprovedClosedDatabaseRepresentations()
    {
        using var context = CreateContext();

        AssertProviderValue<BatchRow>(context, nameof(BatchRow.Status), BatchStatus.InProgress, "In Progress");
        AssertProviderValue<BatchStepExecutionRow>(context, nameof(BatchStepExecutionRow.Status), StepExecutionStatus.InProgress, "In Progress");
        AssertProviderValue<ProductionOrderRow>(context, nameof(ProductionOrderRow.Status), ProductionOrderStatus.InProcess, "In Process");
        AssertProviderValue<DeviationRow>(context, nameof(DeviationRow.Status), DeviationStatus.UnderReview, "Under Review");
        AssertProviderValue<ElectronicSignatureRow>(context, nameof(ElectronicSignatureRow.Meaning), ElectronicSignatureMeaning.Completion, "Completion");
        AssertProviderValue<AuditEventRow>(context, nameof(AuditEventRow.Action), AuditEventType.BatchCompleted, "BatchCompleted");
    }

    [Fact]
    public void AppUserMapping_MatchesDbds()
    {
        using var context = CreateContext();
        var entity = GetEntity<AppUserRow>(context);

        AssertPrimaryKey(entity, "PK_AppUser", nameof(AppUserRow.UserId));
        AssertProperty(entity, nameof(AppUserRow.UserId), "nvarchar(64)", false, 64);
        AssertProperty(entity, nameof(AppUserRow.UserName), "nvarchar(100)", false, 100);
        AssertProperty(entity, nameof(AppUserRow.DisplayName), "nvarchar(100)", false, 100);
        AssertProperty(entity, nameof(AppUserRow.IsActive), "bit", false);
        AssertIndex(entity, "UQ_AppUser_UserName", true, nameof(AppUserRow.UserName));
    }

    [Fact]
    public void RecipeMapping_MatchesDbds()
    {
        using var context = CreateContext();
        var entity = GetEntity<RecipeRow>(context);

        AssertPrimaryKey(entity, "PK_Recipe", nameof(RecipeRow.RecipeId));
        AssertProperty(entity, nameof(RecipeRow.RecipeId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(RecipeRow.RecipeCode), "nvarchar(40)", false, 40);
        AssertProperty(entity, nameof(RecipeRow.Name), "nvarchar(160)", false, 160);
        AssertProperty(entity, nameof(RecipeRow.Version), "nvarchar(32)", false, 32);
        AssertProperty(entity, nameof(RecipeRow.IsApproved), "bit", false);
        AssertIndex(entity, "UQ_Recipe_RecipeCode", true, nameof(RecipeRow.RecipeCode));
    }

    [Fact]
    public void RecipeStepMapping_MatchesDbds()
    {
        using var context = CreateContext();
        var entity = GetEntity<RecipeStepRow>(context);

        AssertPrimaryKey(entity, "PK_RecipeStep", nameof(RecipeStepRow.RecipeStepId));
        AssertProperty(entity, nameof(RecipeStepRow.RecipeStepId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(RecipeStepRow.RecipeId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(RecipeStepRow.StepNumber), "int", false);
        AssertProperty(entity, nameof(RecipeStepRow.Name), "nvarchar(160)", false, 160);
        AssertProperty(entity, nameof(RecipeStepRow.Instructions), "nvarchar(2000)", false, 2000);
        AssertProperty(entity, nameof(RecipeStepRow.RequiresSignature), "bit", false);
        AssertIndex(
            entity,
            "UQ_RecipeStep_RecipeId_StepNumber",
            true,
            nameof(RecipeStepRow.RecipeId),
            nameof(RecipeStepRow.StepNumber));
        AssertForeignKey(
            entity,
            "FK_RecipeStep_Recipe",
            typeof(RecipeRow),
            nameof(RecipeStepRow.RecipeId));
    }

    [Fact]
    public void RecipeMaterialRequirementMapping_MatchesDbds()
    {
        using var context = CreateContext();
        var entity = GetEntity<RecipeMaterialRequirementRow>(context);

        AssertPrimaryKey(
            entity,
            "PK_RecipeMaterialRequirement",
            nameof(RecipeMaterialRequirementRow.RecipeId),
            nameof(RecipeMaterialRequirementRow.MaterialId));
        AssertProperty(
            entity,
            nameof(RecipeMaterialRequirementRow.RecipeId),
            "uniqueidentifier",
            false);
        AssertProperty(
            entity,
            nameof(RecipeMaterialRequirementRow.MaterialId),
            "uniqueidentifier",
            false);
        AssertProperty(
            entity,
            nameof(RecipeMaterialRequirementRow.RequiredQuantity),
            "decimal(18,3)",
            true,
            precision: 18,
            scale: 3);
        AssertIndex(
            entity,
            "IX_RecipeMaterialRequirement_Material",
            false,
            nameof(RecipeMaterialRequirementRow.MaterialId));
        AssertForeignKey(
            entity,
            "FK_RecipeMaterialRequirement_Recipe",
            typeof(RecipeRow),
            nameof(RecipeMaterialRequirementRow.RecipeId));
        AssertForeignKey(
            entity,
            "FK_RecipeMaterialRequirement_Material",
            typeof(MaterialRow),
            nameof(RecipeMaterialRequirementRow.MaterialId));
    }

    [Fact]
    public void RecipeEquipmentRequirementMapping_MatchesDbds()
    {
        using var context = CreateContext();
        var entity = GetEntity<RecipeEquipmentRequirementRow>(context);

        AssertPrimaryKey(
            entity,
            "PK_RecipeEquipmentRequirement",
            nameof(RecipeEquipmentRequirementRow.RecipeId),
            nameof(RecipeEquipmentRequirementRow.EquipmentId));
        AssertProperty(
            entity,
            nameof(RecipeEquipmentRequirementRow.RecipeId),
            "uniqueidentifier",
            false);
        AssertProperty(
            entity,
            nameof(RecipeEquipmentRequirementRow.EquipmentId),
            "uniqueidentifier",
            false);
        AssertIndex(
            entity,
            "IX_RecipeEquipmentRequirement_Equipment",
            false,
            nameof(RecipeEquipmentRequirementRow.EquipmentId));
        AssertForeignKey(
            entity,
            "FK_RecipeEquipmentRequirement_Recipe",
            typeof(RecipeRow),
            nameof(RecipeEquipmentRequirementRow.RecipeId));
        AssertForeignKey(
            entity,
            "FK_RecipeEquipmentRequirement_Equipment",
            typeof(EquipmentRow),
            nameof(RecipeEquipmentRequirementRow.EquipmentId));
    }

    [Fact]
    public void MaterialMapping_MatchesDbds()
    {
        using var context = CreateContext();
        var entity = GetEntity<MaterialRow>(context);

        AssertPrimaryKey(entity, "PK_Material", nameof(MaterialRow.MaterialId));
        AssertProperty(entity, nameof(MaterialRow.MaterialId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(MaterialRow.MaterialCode), "nvarchar(40)", false, 40);
        AssertProperty(entity, nameof(MaterialRow.Name), "nvarchar(160)", false, 160);
        AssertIndex(
            entity,
            "UQ_Material_MaterialCode",
            true,
            nameof(MaterialRow.MaterialCode));
    }

    [Fact]
    public void EquipmentMapping_MatchesDbds()
    {
        using var context = CreateContext();
        var entity = GetEntity<EquipmentRow>(context);

        AssertPrimaryKey(entity, "PK_Equipment", nameof(EquipmentRow.EquipmentId));
        AssertProperty(entity, nameof(EquipmentRow.EquipmentId), "uniqueidentifier", false);
        AssertProperty(entity, nameof(EquipmentRow.EquipmentCode), "nvarchar(40)", false, 40);
        AssertProperty(entity, nameof(EquipmentRow.Name), "nvarchar(160)", false, 160);
        AssertProperty(entity, nameof(EquipmentRow.CalibrationDueDate), "date", true);
        Assert.Equal(
            typeof(DateOnly),
            entity.FindProperty(nameof(EquipmentRow.CalibrationDueDate))!.ClrType.UnwrapNullableType());
        AssertIndex(
            entity,
            "UQ_Equipment_EquipmentCode",
            true,
            nameof(EquipmentRow.EquipmentCode));
    }

    [Fact]
    public void AddSyncBridgePersistence_WithBlankConnectionString_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddSyncBridgePersistence(" "));
    }

    [Fact]
    public void AddSyncBridgePersistence_RegistersDbContextAsScoped()
    {
        var services = new ServiceCollection();

        services.AddSyncBridgePersistence(MetadataOnlyConnectionString);

        var registration = Assert.Single(
            services,
            service => service.ServiceType == typeof(SyncBridgeDbContext));
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
    }

    [Fact]
    public void AddSyncBridgePersistence_RegistersRepositoriesAndUnitOfWorkAsScoped()
    {
        var services = new ServiceCollection();

        services.AddSyncBridgePersistence(MetadataOnlyConnectionString);

        AssertScoped<IRecipeRepository>(services);
        AssertScoped<IProductionOrderRepository>(services);
        AssertScoped<IBatchRepository>(services);
        AssertScoped<IApplicationUnitOfWork>(services);
        AssertScoped<DevelopmentDataSeeder>(services);
    }

    [Fact]
    public async Task UnitOfWork_RejectsAuditEventUpdatesBeforeDatabaseAccess()
    {
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>();
        var row = new AuditEventRow
        {
            AuditEventId = Guid.NewGuid(),
            UserId = "demo.operator",
            AffectedEntity = "Batch",
            AffectedEntityId = "BATCH-001",
            Action = AuditEventType.BatchCreated,
            OccurredAt = DateTimeOffset.UtcNow
        };
        context.Attach(row);
        context.Entry(row).State = EntityState.Modified;

        await Assert.ThrowsAsync<PersistenceIntegrityException>(() => unitOfWork.CommitAsync());
    }

    [Fact]
    public async Task UnitOfWork_RejectsCompletedBatchUpdatesBeforeDatabaseAccess()
    {
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>();
        var row = new BatchRow
        {
            BatchId = Guid.NewGuid(),
            BatchNumber = "BATCH-001",
            OrderId = Guid.NewGuid(),
            Status = BatchStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Attach(row);
        row.FinalDisposition = "Attempted change";

        await Assert.ThrowsAsync<PersistenceIntegrityException>(() => unitOfWork.CommitAsync());
    }

    private static SyncBridgeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SyncBridgeDbContext>()
            .UseSqlServer(MetadataOnlyConnectionString)
            .Options;

        return new SyncBridgeDbContext(options);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSyncBridgePersistence(MetadataOnlyConnectionString);
        return services.BuildServiceProvider();
    }

    private static void AssertScoped<TService>(IServiceCollection services)
    {
        var registration = Assert.Single(
            services,
            service => service.ServiceType == typeof(TService));
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
    }

    private static IEntityType GetEntity<TRow>(SyncBridgeDbContext context)
    {
        return context.Model.FindEntityType(typeof(TRow))!;
    }

    private static void AssertPrimaryKey(
        IEntityType entity,
        string expectedName,
        params string[] expectedProperties)
    {
        var primaryKey = entity.FindPrimaryKey()!;

        Assert.Equal(expectedName, primaryKey.GetName());
        Assert.Equal(expectedProperties, primaryKey.Properties.Select(property => property.Name));
    }

    private static void AssertProperty(
        IEntityType entity,
        string propertyName,
        string columnType,
        bool nullable,
        int? maxLength = null,
        int? precision = null,
        int? scale = null)
    {
        var property = entity.FindProperty(propertyName)!;

        Assert.Equal(columnType, property.GetColumnType());
        Assert.Equal(nullable, property.IsNullable);
        Assert.Equal(maxLength, property.GetMaxLength());
        Assert.Equal(precision, property.GetPrecision());
        Assert.Equal(scale, property.GetScale());
    }

    private static void AssertIndex(
        IEntityType entity,
        string expectedName,
        bool unique,
        params string[] expectedProperties)
    {
        var index = Assert.Single(
            entity.GetIndexes(),
            candidate => candidate.GetDatabaseName() == expectedName);

        Assert.Equal(unique, index.IsUnique);
        Assert.Equal(expectedProperties, index.Properties.Select(property => property.Name));
    }

    private static void AssertForeignKey(
        IEntityType entity,
        string expectedName,
        Type principalType,
        params string[] expectedProperties)
    {
        var foreignKey = Assert.Single(
            entity.GetForeignKeys(),
            candidate => candidate.GetConstraintName() == expectedName);

        Assert.Equal(principalType, foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        Assert.Equal(expectedProperties, foreignKey.Properties.Select(property => property.Name));
    }

    private static void AssertProviderValue<TRow>(
        SyncBridgeDbContext context,
        string propertyName,
        object modelValue,
        object expectedProviderValue)
    {
        var property = GetEntity<TRow>(context).FindProperty(propertyName)!;
        var converter = property.GetTypeMapping().Converter!;

        Assert.Equal(expectedProviderValue, converter.ConvertToProvider(modelValue));
    }
}

internal static class TypeExtensions
{
    public static Type UnwrapNullableType(this Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }
}

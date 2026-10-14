using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SyncBridge.Core.Application.Contracts.Persistence;
using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;
using SyncBridge.Core.Infrastructure.Persistence;
using SyncBridge.Core.Infrastructure.Persistence.Exceptions;
using SyncBridge.Core.Infrastructure.Persistence.Rows;
using SyncBridge.Core.Infrastructure.Persistence.Seeding;

namespace SyncBridge.Core.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlServerRecoveryCollection
{
    public const string Name = "SQL Server recovery";
}

[Collection(SqlServerRecoveryCollection.Name)]
[Trait("Category", "LiveSqlServer")]
public sealed class SqlServerRecoveryIntegrationTests
{
    private const string Operator = "demo.operator";
    private static readonly DateTimeOffset Baseline =
        new(2026, 10, 12, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DevelopmentSeed_IsRepeatableAndCreatesNoManufacturingHistory()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();
        var context = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();

        var firstExecutionCount = await seeder.SeedAsync();
        var secondExecutionCount = await seeder.SeedAsync();

        Assert.InRange(firstExecutionCount, 0, 11);
        Assert.Equal(0, secondExecutionCount);
        Assert.Equal(2, await context.AppUsers.CountAsync(row => row.UserId.StartsWith("demo.")));
        Assert.Equal(1, await context.Materials.CountAsync(row => row.MaterialCode == "MAT-DEMO-001"));
        Assert.Equal(1, await context.Equipment.CountAsync(row => row.EquipmentCode == "EQ-DEMO-001"));
        Assert.Equal(1, await context.Recipes.CountAsync(row => row.RecipeCode == "RCP-DEMO-001"));
        Assert.Equal(2, await context.ProductionOrders.CountAsync(row => row.OrderNumber.StartsWith("PO-DEMO-")));
        Assert.Empty(await context.Batches.ToArrayAsync());
        Assert.Empty(await context.AuditEvents.ToArrayAsync());
        Assert.Empty(await context.ElectronicSignatures.ToArrayAsync());
        Assert.Empty(await context.Deviations.ToArrayAsync());
    }

    [Fact]
    public async Task RecipeAndProductionOrderRepositories_RoundTripThroughSqlServer()
    {
        await using var provider = CreateProvider();
        await EnsureSeededAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var recipes = scope.ServiceProvider.GetRequiredService<IRecipeRepository>();
        var orders = scope.ServiceProvider.GetRequiredService<IProductionOrderRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>();
        var recipe = await recipes.GetByCodeAsync(new RecipeCode("RCP-DEMO-001"));
        Assert.NotNull(recipe);
        Assert.Equal([1, 2], recipe.Steps.Select(step => step.StepNumber));
        Assert.Single(recipe.MaterialRequirements);
        Assert.Single(recipe.EquipmentRequirements);

        var suffix = Guid.NewGuid().ToString("N")[..12];
        var order = new ProductionOrder(
            new ProductionOrderId(Guid.NewGuid()),
            new ProductionOrderNumber($"IT-PO-{suffix}"),
            new ProductCode("IT-PRODUCT"),
            "Integration product 🧪",
            recipe.RecipeId,
            180m,
            Baseline);

        await orders.AddAsync(order);
        Assert.Equal(1, await unitOfWork.CommitAsync());
        var restored = await orders.GetByNumberAsync(order.ProductionOrderNumber);

        Assert.NotNull(restored);
        Assert.Equal(order.ProductionOrderId, restored.ProductionOrderId);
        Assert.Equal(order.RecipeId, restored.RecipeId);
        Assert.Equal(180m, restored.Quantity);
        Assert.Equal(ProductionOrderStatus.Planned, restored.ProductionOrderStatus);
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task BatchRepository_AddsAndReconstructsWithoutGeneratingEvidence()
    {
        await using var provider = CreateProvider();
        await EnsureSeededAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IProductionOrderRepository>();
        var batchRepository = scope.ServiceProvider.GetRequiredService<IBatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>();
        var order = await orderRepository.GetByNumberAsync(new ProductionOrderNumber("PO-DEMO-001"));
        Assert.NotNull(order);
        var batch = new Batch(
            new BatchNumber($"IT-B-{Guid.NewGuid():N}"[..20]),
            order,
            new RecipeCode("RCP-DEMO-001"),
            new OperatorId(Operator),
            Baseline);

        await batchRepository.AddAsync(batch);
        await unitOfWork.CommitAsync();
        var persistedId = await context.Batches
            .Where(row => row.BatchNumber == batch.BatchNumber.Value)
            .Select(row => row.BatchId)
            .SingleAsync();
        var eventIdsBefore = await context.AuditEvents
            .Where(row => row.BatchId == persistedId)
            .Select(row => row.AuditEventId)
            .ToArrayAsync();

        var restored = await batchRepository.GetExecutionAggregateAsync(batch.BatchNumber);
        var eventIdsAfter = await context.AuditEvents
            .Where(row => row.BatchId == persistedId)
            .Select(row => row.AuditEventId)
            .ToArrayAsync();

        Assert.NotNull(restored);
        Assert.Equal(BatchStatus.Created, restored.BatchStatus);
        Assert.Equal(order.Quantity, restored.PlannedQuantity);
        Assert.Equal(batch.AuditEvents.Select(item => item.AuditEventId), restored.AuditEvents.Select(item => item.AuditEventId));
        Assert.Equal(eventIdsBefore, eventIdsAfter);
        Assert.Equal(persistedId, await context.Batches
            .Where(row => row.BatchNumber == batch.BatchNumber.Value)
            .Select(row => row.BatchId)
            .SingleAsync());
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task BatchRepository_ReconstructsAllFiveStatesAndRichEvidence()
    {
        await using var provider = CreateProvider();
        await EnsureSeededAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var repository = scope.ServiceProvider.GetRequiredService<IBatchRepository>();
        var fixtures = new Dictionary<BatchStatus, BatchNumber>();

        foreach (var status in Enum.GetValues<BatchStatus>())
        {
            var value = $"IT-{status}-{Guid.NewGuid():N}";
            var batchNumber = new BatchNumber(value[..Math.Min(40, value.Length)]);
            fixtures[status] = batchNumber;
            await AddPersistedBatchAsync(context, batchNumber.Value, status, status == BatchStatus.InProgress);
        }

        await context.SaveChangesAsync();
        var auditCountBefore = await context.AuditEvents.CountAsync(row => row.BatchId != null);

        foreach (var fixture in fixtures)
        {
            var restored = await repository.GetExecutionAggregateAsync(fixture.Value);
            Assert.NotNull(restored);
            Assert.Equal(fixture.Key, restored.BatchStatus);
        }

        var rich = await repository.GetExecutionAggregateAsync(fixtures[BatchStatus.InProgress]);
        Assert.NotNull(rich);
        Assert.Single(rich.StepExecutions);
        Assert.Equal(StepExecutionStatus.Completed, rich.StepExecutions[0].StepExecutionStatus);
        Assert.Contains(rich.AuditEvents, item => item.Action == AuditEventType.MaterialVerified);
        Assert.Contains(rich.AuditEvents, item => item.Action == AuditEventType.EquipmentVerified);
        Assert.Contains(rich.AuditEvents, item => item.Action == AuditEventType.ElectronicSignatureApplied);
        Assert.Equal(auditCountBefore, await context.AuditEvents.CountAsync(row => row.BatchId != null));
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task SqlServerConstraintsAndUnitOfWork_RejectInvalidAndPartialChanges()
    {
        await using var provider = CreateProvider();
        await EnsureSeededAsync(provider);

        await AssertRejectedAsync(provider, async context =>
        {
            context.Materials.Add(new MaterialRow
            {
                MaterialId = Guid.NewGuid(),
                MaterialCode = "MAT-DEMO-001",
                Name = "Duplicate"
            });
            await context.SaveChangesAsync();
        });

        await AssertRejectedAsync(provider, async context =>
        {
            context.Materials.Add(new MaterialRow
            {
                MaterialId = Guid.NewGuid(),
                MaterialCode = $"IT-NULL-{Guid.NewGuid():N}"[..39],
                Name = null!
            });
            await context.SaveChangesAsync();
        });

        await AssertRejectedAsync(provider, async context =>
        {
            var recipeId = await context.Recipes
                .Where(row => row.RecipeCode == "RCP-DEMO-001")
                .Select(row => row.RecipeId)
                .SingleAsync();
            var materialId = await context.Materials
                .Where(row => row.MaterialCode == "MAT-DEMO-001")
                .Select(row => row.MaterialId)
                .SingleAsync();
            context.RecipeMaterialRequirements.Add(new RecipeMaterialRequirementRow
            {
                RecipeId = recipeId,
                MaterialId = materialId,
                RequiredQuantity = 1m
            });
            await context.SaveChangesAsync();
        });

        await AssertRejectedAsync(provider, async context =>
        {
            var recipe = await context.Recipes.SingleAsync(row => row.RecipeCode == "RCP-DEMO-001");
            context.Recipes.Remove(recipe);
            await context.SaveChangesAsync();
        });

        await AssertRejectedAsync(provider, async context =>
        {
            var orderId = await context.ProductionOrders
                .Where(row => row.OrderNumber == "PO-DEMO-001")
                .Select(row => row.OrderId)
                .SingleAsync();
            var batchId = Guid.NewGuid();
            context.Batches.Add(new BatchRow
            {
                BatchId = batchId,
                BatchNumber = $"IT-DUP-{Guid.NewGuid():N}"[..39],
                OrderId = orderId,
                Status = BatchStatus.Created,
                CreatedAt = Baseline
            });
            var duplicateCode = $"DEV-DUP-{Guid.NewGuid():N}"[..39];
            context.Deviations.AddRange(
                new DeviationRow
                {
                    DeviationId = Guid.NewGuid(),
                    DeviationCode = duplicateCode,
                    BatchId = batchId,
                    Description = "First",
                    Status = DeviationStatus.Open,
                    ReportedByUserId = Operator,
                    OpenedAt = Baseline
                },
                new DeviationRow
                {
                    DeviationId = Guid.NewGuid(),
                    DeviationCode = duplicateCode,
                    BatchId = batchId,
                    Description = "Second",
                    Status = DeviationStatus.Open,
                    ReportedByUserId = Operator,
                    OpenedAt = Baseline
                });
            await context.SaveChangesAsync();
        });

        await AssertRejectedAsync(provider, async context =>
        {
            context.AuditEvents.Add(new AuditEventRow
            {
                AuditEventId = Guid.NewGuid(),
                UserId = Operator,
                Action = AuditEventType.MaterialVerified,
                AffectedEntity = "BoundaryEvidence",
                AffectedEntityId = new string('X', 257),
                OccurredAt = Baseline
            });
            await context.SaveChangesAsync();
        });

        await AssertRejectedAsync(provider, async context =>
        {
            context.Materials.Add(new MaterialRow
            {
                MaterialId = Guid.NewGuid(),
                MaterialCode = new string('X', 41),
                Name = "Overlength"
            });
            await context.SaveChangesAsync();
        });

        var marker = $"it.rollback.{Guid.NewGuid():N}";
        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>();
            await using var transaction = await context.Database.BeginTransactionAsync();
            context.AppUsers.Add(new AppUserRow
            {
                UserId = marker,
                UserName = marker,
                DisplayName = "Rollback marker",
                IsActive = true
            });
            context.ProductionOrders.Add(new ProductionOrderRow
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = $"IT-INVALID-{Guid.NewGuid():N}"[..39],
                ProductCode = "IT",
                ProductName = "Invalid foreign key",
                RecipeId = Guid.NewGuid(),
                Quantity = 1m,
                Status = ProductionOrderStatus.Planned,
                CreatedAt = Baseline
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.CommitAsync());
            await transaction.RollbackAsync();
        }

        await using var verificationScope = provider.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        Assert.False(await verificationContext.AppUsers.AnyAsync(row => row.UserId == marker));
    }

    [Fact]
    public async Task DbdsAmendmentsAndOptionalFields_RoundTripWithoutTruncationOrOverwrite()
    {
        await using var provider = CreateProvider();
        await EnsureSeededAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var repository = scope.ServiceProvider.GetRequiredService<IBatchRepository>();
        var batchNumber = $"IT-OPT-{Guid.NewGuid():N}"[..24];
        var fixture = await AddPersistedBatchAsync(context, batchNumber, BatchStatus.InProgress, true);
        var deviationCode = $"DEV-🧪-{Guid.NewGuid():N}"[..30];
        var deviationId = Guid.NewGuid();
        context.Deviations.Add(new DeviationRow
        {
            DeviationId = deviationId,
            DeviationCode = deviationCode,
            BatchId = fixture.BatchId,
            Description = "Unicode deviation 🧪",
            Status = DeviationStatus.Closed,
            ReportedByUserId = Operator,
            OpenedAt = Baseline.AddMinutes(4),
            ReviewComments = "Reviewed",
            FinalDisposition = "Closed",
            ResolvedAt = Baseline.AddMinutes(5)
        });
        foreach (var (action, minute, state) in new[]
        {
            (AuditEventType.DeviationOpened, 4, "Open"),
            (AuditEventType.DeviationReviewStarted, 5, "UnderReview"),
            (AuditEventType.DeviationApproved, 6, "Approved"),
            (AuditEventType.DeviationResolved, 7, "Closed")
        })
        {
            context.AuditEvents.Add(CreateAudit(fixture.BatchId, action, "Deviation", deviationCode, Baseline.AddMinutes(minute), state));
        }

        var identifier180 = "Å漢" + string.Concat(Enumerable.Repeat("🧪", 89));
        context.AuditEvents.Add(CreateAudit(fixture.BatchId, AuditEventType.MaterialVerified, "BoundaryEvidence", identifier180, Baseline.AddMinutes(8), "Passed"));
        await context.SaveChangesAsync();
        var optionalBefore = await context.BatchStepExecutions
            .Where(row => row.BatchId == fixture.BatchId)
            .Select(row => row.RecordedData)
            .SingleAsync();

        var restored = await repository.GetExecutionAggregateAsync(new BatchNumber(batchNumber));

        Assert.NotNull(restored);
        Assert.Contains(restored.Deviations, item => item.DeviationCode.Value == deviationCode);
        Assert.Equal(deviationId, await context.Deviations
            .Where(row => row.DeviationCode == deviationCode)
            .Select(row => row.DeviationId)
            .SingleAsync());
        Assert.Equal(identifier180, await context.AuditEvents
            .Where(row => row.AffectedEntityId == identifier180)
            .Select(row => row.AffectedEntityId)
            .SingleAsync());
        Assert.Equal(optionalBefore, await context.BatchStepExecutions
            .Where(row => row.BatchId == fixture.BatchId)
            .Select(row => row.RecordedData)
            .SingleAsync());
        Assert.Equal("Retain final disposition", await context.Batches
            .Where(row => row.BatchId == fixture.BatchId)
            .Select(row => row.FinalDisposition)
            .SingleAsync());

        var signature = await context.ElectronicSignatures
            .SingleAsync(row => row.BatchId == fixture.BatchId);
        signature.RelatedRecordId = Guid.NewGuid();
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<PersistenceIntegrityException>(
            () => repository.GetExecutionAggregateAsync(new BatchNumber(batchNumber)));
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task RemainingUniqueAndCompositeConstraints_AreEnforcedBySqlServer()
    {
        await using var provider = CreateProvider();
        await EnsureSeededAsync(provider);

        await AssertRejectedAsync(provider, async context =>
        {
            context.AppUsers.Add(new AppUserRow
            {
                UserId = Operator,
                UserName = $"it-duplicate-{Guid.NewGuid():N}",
                DisplayName = "Duplicate primary key",
                IsActive = true
            });
            await context.SaveChangesAsync();
        });

        await AssertRejectedAsync(provider, async context =>
        {
            context.Recipes.Add(new RecipeRow
            {
                RecipeId = Guid.NewGuid(),
                RecipeCode = "RCP-DEMO-001",
                Name = "Duplicate recipe code",
                Version = "1.0",
                IsApproved = true
            });
            await context.SaveChangesAsync();
        });

        await AssertRejectedAsync(provider, async context =>
        {
            context.Equipment.Add(new EquipmentRow
            {
                EquipmentId = Guid.NewGuid(),
                EquipmentCode = "EQ-DEMO-001",
                Name = "Duplicate equipment code"
            });
            await context.SaveChangesAsync();
        });

        await AssertRejectedAsync(provider, async context =>
        {
            var recipeId = await context.Recipes
                .Where(row => row.RecipeCode == "RCP-DEMO-001")
                .Select(row => row.RecipeId)
                .SingleAsync();
            context.ProductionOrders.Add(new ProductionOrderRow
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = "PO-DEMO-001",
                ProductCode = "IT",
                ProductName = "Duplicate order number",
                RecipeId = recipeId,
                Quantity = 1m,
                Status = ProductionOrderStatus.Planned,
                CreatedAt = Baseline
            });
            await context.SaveChangesAsync();
        });

        await AssertRejectedAsync(provider, async context =>
        {
            var orderId = await context.ProductionOrders
                .Where(row => row.OrderNumber == "PO-DEMO-001")
                .Select(row => row.OrderId)
                .SingleAsync();
            context.Batches.AddRange(
                new BatchRow
                {
                    BatchId = Guid.NewGuid(),
                    BatchNumber = "IT-DUPLICATE-BATCH",
                    OrderId = orderId,
                    Status = BatchStatus.Created,
                    CreatedAt = Baseline
                },
                new BatchRow
                {
                    BatchId = Guid.NewGuid(),
                    BatchNumber = "IT-DUPLICATE-BATCH",
                    OrderId = orderId,
                    Status = BatchStatus.Created,
                    CreatedAt = Baseline
                });
            await context.SaveChangesAsync();
        });

        await AssertRejectedAsync(provider, async context =>
        {
            var recipe = await context.Recipes.SingleAsync(row => row.RecipeCode == "RCP-DEMO-001");
            context.RecipeSteps.Add(new RecipeStepRow
            {
                RecipeStepId = Guid.NewGuid(),
                RecipeId = recipe.RecipeId,
                StepNumber = 1,
                Name = "Duplicate sequence",
                Instructions = "Rejected by unique Recipe/StepNumber constraint.",
                RequiresSignature = false
            });
            await context.SaveChangesAsync();
        });

        await AssertRejectedAsync(provider, async context =>
        {
            var fixture = await AddPersistedBatchAsync(
                context,
                $"IT-STEP-{Guid.NewGuid():N}"[..28],
                BatchStatus.Created,
                false);
            var recipeStepId = await context.RecipeSteps
                .OrderBy(row => row.StepNumber)
                .Select(row => row.RecipeStepId)
                .FirstAsync();
            context.BatchStepExecutions.AddRange(
                CreatePendingExecution(fixture.BatchId, recipeStepId, 1),
                CreatePendingExecution(fixture.BatchId, recipeStepId, 1));
            await context.SaveChangesAsync();
        });

        await AssertRejectedAsync(provider, async context =>
        {
            var fixture = await AddPersistedBatchAsync(
                context,
                $"IT-MAT-{Guid.NewGuid():N}"[..27],
                BatchStatus.Created,
                false);
            var materialId = await context.Materials
                .Where(row => row.MaterialCode == "MAT-DEMO-001")
                .Select(row => row.MaterialId)
                .SingleAsync();
            context.MaterialVerifications.AddRange(
                CreateMaterialVerification(fixture.BatchId, materialId),
                CreateMaterialVerification(fixture.BatchId, materialId));
            await context.SaveChangesAsync();
        });

        await AssertRejectedAsync(provider, async context =>
        {
            var fixture = await AddPersistedBatchAsync(
                context,
                $"IT-EQP-{Guid.NewGuid():N}"[..27],
                BatchStatus.Created,
                false);
            var equipmentId = await context.Equipment
                .Where(row => row.EquipmentCode == "EQ-DEMO-001")
                .Select(row => row.EquipmentId)
                .SingleAsync();
            context.EquipmentVerifications.AddRange(
                CreateEquipmentVerification(fixture.BatchId, equipmentId),
                CreateEquipmentVerification(fixture.BatchId, equipmentId));
            await context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task ScalarBoundariesAndAllProductionOrderStatuses_RoundTripExactly()
    {
        await using var provider = CreateProvider();
        await EnsureSeededAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IProductionOrderRepository>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var recipeId = await context.Recipes
            .Where(row => row.RecipeCode == "RCP-DEMO-001")
            .Select(row => row.RecipeId)
            .SingleAsync();
        var timestamp = new DateTimeOffset(2026, 10, 12, 19, 18, 17, 123, TimeSpan.FromHours(-4));
        var statusRows = Enum.GetValues<ProductionOrderStatus>()
            .Select(status => new ProductionOrderRow
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = $"IT-PO-{status}-{Guid.NewGuid():N}"[..39],
                ProductCode = "IT-ROUNDTRIP",
                ProductName = "Status round-trip",
                RecipeId = recipeId,
                Quantity = 1.2346m,
                Status = status,
                CreatedAt = timestamp
            })
            .ToArray();
        var equipment = new EquipmentRow
        {
            EquipmentId = Guid.NewGuid(),
            EquipmentCode = $"IT-EQ-{Guid.NewGuid():N}",
            Name = "Date round-trip",
            CalibrationDueDate = new DateOnly(2031, 2, 3)
        };
        context.ProductionOrders.AddRange(statusRows);
        context.Equipment.Add(equipment);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        foreach (var expected in statusRows)
        {
            var restored = await repository.GetByNumberAsync(new ProductionOrderNumber(expected.OrderNumber));
            Assert.NotNull(restored);
            Assert.Equal(expected.OrderId, restored.ProductionOrderId.Value);
            Assert.Equal(expected.Status, restored.ProductionOrderStatus);
            Assert.Equal(1.235m, restored.Quantity);
            Assert.Equal(timestamp, restored.CreatedAt);
        }

        Assert.Equal(new DateOnly(2031, 2, 3), await context.Equipment
            .Where(row => row.EquipmentId == equipment.EquipmentId)
            .Select(row => row.CalibrationDueDate)
            .SingleAsync());
        await transaction.RollbackAsync();

        await AssertRejectedAsync(provider, async invalidContext =>
        {
            invalidContext.ProductionOrders.Add(new ProductionOrderRow
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = $"IT-PREC-{Guid.NewGuid():N}"[..39],
                ProductCode = "IT",
                ProductName = "Precision overflow",
                RecipeId = recipeId,
                Quantity = 1234567890123456.123m,
                Status = ProductionOrderStatus.Planned,
                CreatedAt = Baseline
            });
            await invalidContext.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task ApprovedUnitOfWork_RejectsEvidenceAndCompletedBatchMutation()
    {
        await using var provider = CreateProvider();
        await EnsureSeededAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var fixture = await AddPersistedBatchAsync(
            context,
            $"IT-IMM-{Guid.NewGuid():N}"[..27],
            BatchStatus.Completed,
            true);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var signature = await context.ElectronicSignatures.SingleAsync(row => row.BatchId == fixture.BatchId);
        signature.SignedAt = signature.SignedAt.AddSeconds(1);
        await Assert.ThrowsAsync<PersistenceIntegrityException>(() => unitOfWork.CommitAsync());
        context.ChangeTracker.Clear();

        var audit = await context.AuditEvents.FirstAsync(row => row.BatchId == fixture.BatchId);
        audit.Details = "Unauthorized change";
        await Assert.ThrowsAsync<PersistenceIntegrityException>(() => unitOfWork.CommitAsync());
        context.ChangeTracker.Clear();

        var completed = await context.Batches.SingleAsync(row => row.BatchId == fixture.BatchId);
        completed.FinalDisposition = "Unauthorized change";
        await Assert.ThrowsAsync<PersistenceIntegrityException>(() => unitOfWork.CommitAsync());
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task RepresentativePersistenceOperations_ProduceBoundedBaselineMeasurements()
    {
        await using var provider = CreateProvider();
        await EnsureSeededAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        var orders = scope.ServiceProvider.GetRequiredService<IProductionOrderRepository>();
        var recipes = scope.ServiceProvider.GetRequiredService<IRecipeRepository>();
        var batches = scope.ServiceProvider.GetRequiredService<IBatchRepository>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var batchNumber = $"IT-PERF-{Guid.NewGuid():N}"[..28];
        await AddPersistedBatchAsync(context, batchNumber, BatchStatus.InProgress, true);
        var saveTimer = Stopwatch.StartNew();
        await context.SaveChangesAsync();
        saveTimer.Stop();
        context.ChangeTracker.Clear();

        var orderTimer = Stopwatch.StartNew();
        var order = await orders.GetByNumberAsync(new ProductionOrderNumber("PO-DEMO-001"));
        orderTimer.Stop();
        var recipeTimer = Stopwatch.StartNew();
        var recipe = await recipes.GetByCodeAsync(new RecipeCode("RCP-DEMO-001"));
        recipeTimer.Stop();
        var batchTimer = Stopwatch.StartNew();
        var batch = await batches.GetExecutionAggregateAsync(new BatchNumber(batchNumber));
        batchTimer.Stop();

        Assert.NotNull(order);
        Assert.NotNull(recipe);
        Assert.NotNull(batch);
        Assert.True(saveTimer.Elapsed >= TimeSpan.Zero);
        Assert.True(orderTimer.Elapsed >= TimeSpan.Zero);
        Assert.True(recipeTimer.Elapsed >= TimeSpan.Zero);
        Assert.True(batchTimer.Elapsed >= TimeSpan.Zero);
        Console.WriteLine(
            "PERF|rows=batch:1,steps:1,material:1,equipment:1,signature:1,audit:4|" +
            $"save_ms={saveTimer.Elapsed.TotalMilliseconds:F3}|" +
            $"order_lookup_ms={orderTimer.Elapsed.TotalMilliseconds:F3}|" +
            $"recipe_load_ms={recipeTimer.Elapsed.TotalMilliseconds:F3}|" +
            $"batch_load_ms={batchTimer.Elapsed.TotalMilliseconds:F3}");
        await transaction.RollbackAsync();
    }

    private static ServiceProvider CreateProvider()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SyncBridgeCore");
        Assert.False(string.IsNullOrWhiteSpace(connectionString),
            "ConnectionStrings__SyncBridgeCore is required for SQL Server integration tests.");
        var services = new ServiceCollection();
        services.AddSyncBridgePersistence(connectionString);
        return services.BuildServiceProvider();
    }

    private static async Task EnsureSeededAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
    }

    private static async Task AssertRejectedAsync(
        ServiceProvider provider,
        Func<SyncBridgeDbContext, Task> action)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        await Assert.ThrowsAsync<DbUpdateException>(() => action(context));
        await transaction.RollbackAsync();
    }

    private static async Task<(Guid BatchId, Guid ExecutionId)> AddPersistedBatchAsync(
        SyncBridgeDbContext context,
        string batchNumber,
        BatchStatus status,
        bool richEvidence)
    {
        var order = await context.ProductionOrders.SingleAsync(row => row.OrderNumber == "PO-DEMO-001");
        var batchId = Guid.NewGuid();
        var createdAt = Baseline;
        var startedAt = status is BatchStatus.InProgress or BatchStatus.Exception or BatchStatus.Completed
            ? Baseline.AddMinutes(1)
            : (DateTimeOffset?)null;
        DateTimeOffset? completedAt = status == BatchStatus.Completed
            ? Baseline.AddMinutes(10)
            : null;
        context.Batches.Add(new BatchRow
        {
            BatchId = batchId,
            BatchNumber = batchNumber,
            OrderId = order.OrderId,
            Status = status,
            CreatedAt = createdAt,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            FinalDisposition = richEvidence ? "Retain final disposition" : null
        });
        context.AuditEvents.Add(CreateAudit(batchId, AuditEventType.BatchCreated, "Batch", batchNumber, createdAt, "Created"));
        if (status == BatchStatus.Ready)
        {
            context.AuditEvents.Add(CreateAudit(batchId, AuditEventType.BatchMarkedReady, "Batch", batchNumber, Baseline.AddMinutes(1), "Ready"));
        }

        if (startedAt is not null)
        {
            context.AuditEvents.Add(CreateAudit(batchId, AuditEventType.BatchProcessingStarted, "Batch", batchNumber, startedAt.Value, "InProgress"));
        }

        if (status == BatchStatus.Exception)
        {
            var deviationCode = $"DEV-{Guid.NewGuid():N}"[..30];
            context.Deviations.Add(new DeviationRow
            {
                DeviationId = Guid.NewGuid(),
                DeviationCode = deviationCode,
                BatchId = batchId,
                Description = "Integration exception",
                Status = DeviationStatus.Open,
                ReportedByUserId = Operator,
                OpenedAt = Baseline.AddMinutes(2)
            });
            context.AuditEvents.Add(CreateAudit(batchId, AuditEventType.DeviationOpened, "Deviation", deviationCode, Baseline.AddMinutes(2), "Open"));
            context.AuditEvents.Add(CreateAudit(batchId, AuditEventType.BatchExceptionEntered, "Batch", batchNumber, Baseline.AddMinutes(2), "Exception"));
        }

        if (completedAt is not null)
        {
            context.AuditEvents.Add(CreateAudit(batchId, AuditEventType.BatchCompleted, "Batch", batchNumber, completedAt.Value, "Completed"));
        }

        var executionId = Guid.Empty;
        if (richEvidence)
        {
            var recipeStep = await context.RecipeSteps
                .Where(row => row.RecipeId == order.RecipeId)
                .OrderBy(row => row.StepNumber)
                .FirstAsync();
            var material = await context.Materials.SingleAsync(row => row.MaterialCode == "MAT-DEMO-001");
            var equipment = await context.Equipment.SingleAsync(row => row.EquipmentCode == "EQ-DEMO-001");
            executionId = Guid.NewGuid();
            var materialAt = Baseline.AddMinutes(2);
            var equipmentAt = Baseline.AddMinutes(3);
            var signatureId = Guid.NewGuid();
            context.BatchStepExecutions.Add(new BatchStepExecutionRow
            {
                StepExecutionId = executionId,
                BatchId = batchId,
                RecipeStepId = recipeStep.RecipeStepId,
                StepNumber = recipeStep.StepNumber,
                Status = StepExecutionStatus.Completed,
                StartedAt = Baseline.AddMinutes(4),
                CompletedAt = Baseline.AddMinutes(5),
                CompletedByUserId = Operator,
                RecordedData = "Retain recorded data 🧪"
            });
            context.MaterialVerifications.Add(new MaterialVerificationRow
            {
                VerificationId = Guid.NewGuid(),
                BatchId = batchId,
                MaterialId = material.MaterialId,
                LotNumber = "LOT-RETAIN",
                IsAccepted = true,
                VerifiedByUserId = Operator,
                VerifiedAt = materialAt,
                Comments = "Retain material comments"
            });
            context.EquipmentVerifications.Add(new EquipmentVerificationRow
            {
                VerificationId = Guid.NewGuid(),
                BatchId = batchId,
                EquipmentId = equipment.EquipmentId,
                IsReady = true,
                CalibrationValid = true,
                VerifiedByUserId = Operator,
                VerifiedAt = equipmentAt,
                Comments = "Retain equipment comments"
            });
            context.ElectronicSignatures.Add(new ElectronicSignatureRow
            {
                SignatureId = signatureId,
                BatchId = batchId,
                RelatedRecordType = RelatedRecordType.BatchStepExecution.Value,
                RelatedRecordId = executionId,
                UserId = Operator,
                Meaning = ElectronicSignatureMeaning.Completion,
                SignedAt = Baseline.AddMinutes(5)
            });
            context.AuditEvents.Add(CreateAudit(batchId, AuditEventType.MaterialVerified, "MaterialVerification", $"{batchNumber}:MAT-DEMO-001:{Operator}:{materialAt:O}", materialAt, "Passed"));
            context.AuditEvents.Add(CreateAudit(batchId, AuditEventType.EquipmentVerified, "EquipmentVerification", $"{batchNumber}:EQ-DEMO-001:{Operator}:{equipmentAt:O}", equipmentAt, "Passed"));
            context.AuditEvents.Add(CreateAudit(batchId, AuditEventType.ElectronicSignatureApplied, "ElectronicSignature", signatureId.ToString("D"), Baseline.AddMinutes(5), "Completion"));
        }

        return (batchId, executionId);
    }

    private static BatchStepExecutionRow CreatePendingExecution(
        Guid batchId,
        Guid recipeStepId,
        int stepNumber) =>
        new()
        {
            StepExecutionId = Guid.NewGuid(),
            BatchId = batchId,
            RecipeStepId = recipeStepId,
            StepNumber = stepNumber,
            Status = StepExecutionStatus.Pending
        };

    private static MaterialVerificationRow CreateMaterialVerification(Guid batchId, Guid materialId) =>
        new()
        {
            VerificationId = Guid.NewGuid(),
            BatchId = batchId,
            MaterialId = materialId,
            IsAccepted = true,
            VerifiedByUserId = Operator,
            VerifiedAt = Baseline
        };

    private static EquipmentVerificationRow CreateEquipmentVerification(Guid batchId, Guid equipmentId) =>
        new()
        {
            VerificationId = Guid.NewGuid(),
            BatchId = batchId,
            EquipmentId = equipmentId,
            IsReady = true,
            VerifiedByUserId = Operator,
            VerifiedAt = Baseline
        };

    private static AuditEventRow CreateAudit(
        Guid batchId,
        AuditEventType action,
        string affectedEntity,
        string affectedEntityId,
        DateTimeOffset occurredAt,
        string resultingState) =>
        new()
        {
            AuditEventId = Guid.NewGuid(),
            BatchId = batchId,
            UserId = Operator,
            Action = action,
            AffectedEntity = affectedEntity,
            AffectedEntityId = affectedEntityId,
            OccurredAt = occurredAt,
            ResultingState = resultingState
        };
}

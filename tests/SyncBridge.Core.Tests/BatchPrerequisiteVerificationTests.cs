using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

public sealed class BatchPrerequisiteVerificationTests
{
    [Fact]
    public void StartProcessing_WhenAllMultiplePrerequisitesPasses_TransitionsToInProgress()
    {
        var batch = CreateReadyBatch();
        var recipe = CreateRecipe();
        AddMaterial(recipe, "MAT-001");
        AddMaterial(recipe, "MAT-002");
        AddEquipment(recipe, "EQ-001");
        AddEquipment(recipe, "EQ-002");

        batch.StartProcessing(
            recipe,
            [
                CreateMaterialVerification(batch.BatchNumber, "MAT-001", VerificationResult.Passed),
                CreateMaterialVerification(batch.BatchNumber, "MAT-002", VerificationResult.Passed)
            ],
            [
                CreateEquipmentVerification(batch.BatchNumber, "EQ-001", VerificationResult.Passed),
                CreateEquipmentVerification(batch.BatchNumber, "EQ-002", VerificationResult.Passed)
            ]);

        Assert.Equal(BatchStatus.InProgress, batch.BatchStatus);
    }

    [Fact]
    public void StartProcessing_WhenMaterialVerificationMissing_RejectsAndRemainsReady()
    {
        var batch = CreateReadyBatch();
        var recipe = CreateRecipe();
        AddMaterial(recipe, "MAT-001");

        AssertPrerequisiteFailure(batch, recipe, [], []);
    }

    [Fact]
    public void StartProcessing_WhenMaterialVerificationFailed_RejectsAndRemainsReady()
    {
        var batch = CreateReadyBatch();
        var recipe = CreateRecipe();
        AddMaterial(recipe, "MAT-001");
        MaterialVerification[] evidence =
        [
            CreateMaterialVerification(batch.BatchNumber, "MAT-001", VerificationResult.Failed)
        ];

        AssertPrerequisiteFailure(batch, recipe, evidence, []);
    }

    [Fact]
    public void StartProcessing_WhenMaterialVerificationBelongsToAnotherBatch_RejectsAndRemainsReady()
    {
        var batch = CreateReadyBatch();
        var recipe = CreateRecipe();
        AddMaterial(recipe, "MAT-001");
        MaterialVerification[] evidence =
        [
            CreateMaterialVerification(
                new BatchNumber("BATCH-OTHER"),
                "MAT-001",
                VerificationResult.Passed)
        ];

        AssertPrerequisiteFailure(batch, recipe, evidence, []);
    }

    [Fact]
    public void StartProcessing_WhenOnlyUnrelatedMaterialIsVerified_RejectsAndRemainsReady()
    {
        var batch = CreateReadyBatch();
        var recipe = CreateRecipe();
        AddMaterial(recipe, "MAT-001");
        MaterialVerification[] evidence =
        [
            CreateMaterialVerification(batch.BatchNumber, "MAT-OTHER", VerificationResult.Passed)
        ];

        AssertPrerequisiteFailure(batch, recipe, evidence, []);
    }

    [Fact]
    public void StartProcessing_WhenEquipmentVerificationMissing_RejectsAndRemainsReady()
    {
        var batch = CreateReadyBatch();
        var recipe = CreateRecipe();
        AddEquipment(recipe, "EQ-001");

        AssertPrerequisiteFailure(batch, recipe, [], []);
    }

    [Fact]
    public void StartProcessing_WhenEquipmentVerificationFailed_RejectsAndRemainsReady()
    {
        var batch = CreateReadyBatch();
        var recipe = CreateRecipe();
        AddEquipment(recipe, "EQ-001");
        EquipmentVerification[] evidence =
        [
            CreateEquipmentVerification(batch.BatchNumber, "EQ-001", VerificationResult.Failed)
        ];

        AssertPrerequisiteFailure(batch, recipe, [], evidence);
    }

    [Fact]
    public void StartProcessing_WhenEquipmentVerificationBelongsToAnotherBatch_RejectsAndRemainsReady()
    {
        var batch = CreateReadyBatch();
        var recipe = CreateRecipe();
        AddEquipment(recipe, "EQ-001");
        EquipmentVerification[] evidence =
        [
            CreateEquipmentVerification(
                new BatchNumber("BATCH-OTHER"),
                "EQ-001",
                VerificationResult.Passed)
        ];

        AssertPrerequisiteFailure(batch, recipe, [], evidence);
    }

    [Fact]
    public void StartProcessing_WhenOnlyUnrelatedEquipmentIsVerified_RejectsAndRemainsReady()
    {
        var batch = CreateReadyBatch();
        var recipe = CreateRecipe();
        AddEquipment(recipe, "EQ-001");
        EquipmentVerification[] evidence =
        [
            CreateEquipmentVerification(batch.BatchNumber, "EQ-OTHER", VerificationResult.Passed)
        ];

        AssertPrerequisiteFailure(batch, recipe, [], evidence);
    }

    [Fact]
    public void StartProcessing_WhenOnlySubsetOfMultipleRequirementsPasses_RejectsAndRemainsReady()
    {
        var batch = CreateReadyBatch();
        var recipe = CreateRecipe();
        AddMaterial(recipe, "MAT-001");
        AddMaterial(recipe, "MAT-002");
        AddEquipment(recipe, "EQ-001");
        AddEquipment(recipe, "EQ-002");
        MaterialVerification[] materialEvidence =
        [
            CreateMaterialVerification(batch.BatchNumber, "MAT-001", VerificationResult.Passed)
        ];
        EquipmentVerification[] equipmentEvidence =
        [
            CreateEquipmentVerification(batch.BatchNumber, "EQ-001", VerificationResult.Passed)
        ];

        AssertPrerequisiteFailure(batch, recipe, materialEvidence, equipmentEvidence);
    }

    [Fact]
    public void StartProcessing_WithNoMaterialRequirementsAndVerifiedEquipment_TransitionsToInProgress()
    {
        var batch = CreateReadyBatch();
        var recipe = CreateRecipe();
        AddEquipment(recipe, "EQ-001");

        batch.StartProcessing(
            recipe,
            [],
            [CreateEquipmentVerification(batch.BatchNumber, "EQ-001", VerificationResult.Passed)]);

        Assert.Equal(BatchStatus.InProgress, batch.BatchStatus);
    }

    [Fact]
    public void StartProcessing_WithNoEquipmentRequirementsAndVerifiedMaterial_TransitionsToInProgress()
    {
        var batch = CreateReadyBatch();
        var recipe = CreateRecipe();
        AddMaterial(recipe, "MAT-001");

        batch.StartProcessing(
            recipe,
            [CreateMaterialVerification(batch.BatchNumber, "MAT-001", VerificationResult.Passed)],
            []);

        Assert.Equal(BatchStatus.InProgress, batch.BatchStatus);
    }

    [Fact]
    public void StartProcessing_WithNoRequirements_TransitionsToInProgress()
    {
        var batch = CreateReadyBatch();

        batch.StartProcessing(CreateRecipe(), [], []);

        Assert.Equal(BatchStatus.InProgress, batch.BatchStatus);
    }

    [Fact]
    public void StartProcessing_WithDifferentRecipe_RejectsAndRemainsReady()
    {
        var batch = CreateReadyBatch();
        var recipe = new Recipe(
            new RecipeId("RECIPE-ID-OTHER"),
            new RecipeCode("RECIPE-OTHER"),
            "Other Recipe",
            "1.0",
            true);

        AssertPrerequisiteFailure(batch, recipe, [], []);
    }

    [Fact]
    public void StartProcessing_AfterPrerequisiteFailure_CanSucceedWithCompleteEvidence()
    {
        var batch = CreateReadyBatch();
        var recipe = CreateRecipe();
        AddMaterial(recipe, "MAT-001");

        AssertPrerequisiteFailure(batch, recipe, [], []);

        batch.StartProcessing(
            recipe,
            [CreateMaterialVerification(batch.BatchNumber, "MAT-001", VerificationResult.Passed)],
            []);

        Assert.Equal(BatchStatus.InProgress, batch.BatchStatus);
    }

    private static void AssertPrerequisiteFailure(
        Batch batch,
        Recipe recipe,
        IEnumerable<MaterialVerification> materialEvidence,
        IEnumerable<EquipmentVerification> equipmentEvidence)
    {
        Assert.Throws<BatchPrerequisiteVerificationException>(
            () => batch.StartProcessing(recipe, materialEvidence, equipmentEvidence));
        Assert.Equal(BatchStatus.Ready, batch.BatchStatus);
    }

    private static Batch CreateReadyBatch()
    {
        var batch = new Batch(
            new BatchNumber("BATCH-001"),
            new ProductionOrderNumber("PO-001"),
            new RecipeCode("RECIPE-001"),
            100m,
            new UnitOfMeasure("kg"),
            new OperatorId("OP-TEST"),
            new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero));
        batch.MarkReady();
        return batch;
    }

    private static Recipe CreateRecipe()
    {
        return new Recipe(
            new RecipeId("RECIPE-ID-001"),
            new RecipeCode("RECIPE-001"),
            "Tablet Blend",
            "1.0",
            true);
    }

    private static void AddMaterial(Recipe recipe, string materialCode)
    {
        recipe.AddMaterialRequirement(
            new RecipeMaterialRequirement(recipe.RecipeId, new MaterialCode(materialCode), null));
    }

    private static void AddEquipment(Recipe recipe, string equipmentCode)
    {
        recipe.AddEquipmentRequirement(
            new RecipeEquipmentRequirement(recipe.RecipeId, new EquipmentCode(equipmentCode)));
    }

    private static MaterialVerification CreateMaterialVerification(
        BatchNumber batchNumber,
        string materialCode,
        VerificationResult result)
    {
        return new MaterialVerification(
            batchNumber,
            new MaterialCode(materialCode),
            new OperatorId("OP-001"),
            new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero),
            result);
    }

    private static EquipmentVerification CreateEquipmentVerification(
        BatchNumber batchNumber,
        string equipmentCode,
        VerificationResult result)
    {
        return new EquipmentVerification(
            batchNumber,
            new EquipmentCode(equipmentCode),
            new OperatorId("OP-001"),
            new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero),
            result);
    }
}

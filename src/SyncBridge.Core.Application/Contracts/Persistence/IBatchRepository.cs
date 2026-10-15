using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Application.Contracts.Persistence;

/// <summary>
/// Defines Batch aggregate persistence operations required by execution use cases.
/// </summary>
public interface IBatchRepository
{
    Task<IReadOnlyList<BatchNumber>> GetNumbersAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Batch batch, CancellationToken cancellationToken = default);
    Task<BatchEvidence> GetEvidenceAsync(Batch batch, CancellationToken cancellationToken = default);
    Task SaveMaterialVerificationAsync(MaterialVerification verification, CancellationToken cancellationToken = default);
    Task SaveEquipmentVerificationAsync(EquipmentVerification verification, CancellationToken cancellationToken = default);
    Task SaveSignatureAsync(ElectronicSignature signature, CancellationToken cancellationToken = default);
    Task RecordStepDataAsync(BatchStepExecutionId executionId, string data, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BatchNumber>> GetReviewCandidatesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets a Batch execution aggregate with its required execution and evidence associations.
    /// </summary>
    /// <param name="batchNumber">The Batch business number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching Batch aggregate, or null when none exists.</returns>
    Task<Batch?> GetExecutionAggregateAsync(
        BatchNumber batchNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new Batch aggregate to the current persistence scope.
    /// </summary>
    /// <param name="batch">The Batch to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddAsync(Batch batch, CancellationToken cancellationToken = default);
}

public sealed record BatchEvidence(
    IReadOnlyList<MaterialVerification> Materials,
    IReadOnlyList<EquipmentVerification> Equipment,
    IReadOnlyList<ElectronicSignature> Signatures,
    IReadOnlyDictionary<string, string?> RecordedData);

using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Application.Contracts.Persistence;

/// <summary>
/// Defines Recipe retrieval operations required by application use cases.
/// </summary>
public interface IRecipeRepository
{
    /// <summary>
    /// Gets an approved Recipe definition by its identity.
    /// </summary>
    /// <param name="recipeId">The Recipe identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching Recipe, or null when none exists.</returns>
    Task<Recipe?> GetByIdAsync(
        RecipeId recipeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a Recipe definition by its business code.
    /// </summary>
    /// <param name="recipeCode">The Recipe business code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching Recipe, or null when none exists.</returns>
    Task<Recipe?> GetByCodeAsync(
        RecipeCode recipeCode,
        CancellationToken cancellationToken = default);
}

namespace FoodDiary.Persistence.Abstractions;

/// <summary>Checks the caller's scoped persistence state before independently committed work.</summary>
public interface IModuleScopeGuard {
    /// <summary>
    /// Rejects pending changes in the shared context or any registered module context,
    /// and an active shared relational transaction. Checks live state on every call;
    /// does not save, discard changes, or modify post-commit actions.
    /// </summary>
    void EnsureCleanEntry();
}

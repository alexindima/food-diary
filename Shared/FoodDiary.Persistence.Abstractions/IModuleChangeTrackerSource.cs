using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FoodDiary.Persistence.Abstractions;

/// <summary>Provides live owner tracking entries to coordinated save interceptors without exposing a shared context.</summary>
public interface IModuleChangeTrackerSource {
    /// <summary>Detects changes in registered contexts of the requested owner type, in registration order.</summary>
    IReadOnlyList<EntityEntry> GetModuleEntries<TContext>() where TContext : DbContext;
}

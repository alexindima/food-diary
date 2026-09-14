using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Persistence.Abstractions;

/// <summary>Supplies configured options for existing module operations that own their context lifetime.</summary>
public interface IIndependentModuleContextOptionsFactory {
    /// <summary>
    /// Copies configured provider and core options without consulting the live scoped context
    /// or registering a participant in its unit of work. Preserves configured interceptors and retries.
    /// Connection ownership follows the original provider configuration; this does not detach an explicitly supplied connection.
    /// </summary>
    DbContextOptions<TContext> CreateOptions<TContext>() where TContext : DbContext;
}

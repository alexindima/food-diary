using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Persistence.Abstractions;

/// <summary>Creates owner contexts registered with the current scoped persistence coordinator.</summary>
public interface IModuleContextFactory {
    TContext CreateModuleContext<TContext>(Func<DbContextOptions<TContext>, TContext> factory, int saveOrder = 100)
        where TContext : DbContext;
}

using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Shared;

internal sealed class EfIndependentModuleContextOptionsFactory(DbContextOptions options)
    : IIndependentModuleContextOptionsFactory {
    public DbContextOptions<TContext> CreateOptions<TContext>() where TContext : DbContext =>
        new(options.Extensions.ToDictionary(extension => extension.GetType(), extension => extension));
}

using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed class SharedRuntimeDbContext(DbContextOptions<SharedPersistenceDbContext> options) : SharedPersistenceDbContext(options);

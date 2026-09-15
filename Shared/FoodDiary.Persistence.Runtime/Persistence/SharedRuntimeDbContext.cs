using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Persistence.Runtime.Persistence;

public sealed class SharedRuntimeDbContext(DbContextOptions<SharedPersistenceDbContext> options) : SharedPersistenceDbContext(options);

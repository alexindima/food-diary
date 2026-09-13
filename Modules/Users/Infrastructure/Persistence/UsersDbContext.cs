using FoodDiary.Infrastructure;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options) {
    public DbSet<WeightGoal> WeightGoals => Set<WeightGoal>();
    public DbSet<WaistGoal> WaistGoals => Set<WaistGoal>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserRoleAuditEvent> UserRoleAuditEvents => Set<UserRoleAuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyUsersPersistenceModel();
}

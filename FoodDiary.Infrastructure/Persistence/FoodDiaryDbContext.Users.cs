using FoodDiary.Domain.Entities.Admin;
using FoodDiary.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public partial class FoodDiaryDbContext {
    public DbSet<User> Users => Set<User>();
    public DbSet<UserLoginEvent> UserLoginEvents => Set<UserLoginEvent>();
    public DbSet<UserRefreshTokenSession> UserRefreshTokenSessions => Set<UserRefreshTokenSession>();
    public DbSet<UserRoleAuditEvent> UserRoleAuditEvents => Set<UserRoleAuditEvent>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<AdminImpersonationSession> AdminImpersonationSessions => Set<AdminImpersonationSession>();
}

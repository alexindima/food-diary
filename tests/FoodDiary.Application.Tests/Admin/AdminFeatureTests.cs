using FoodDiary.Application.Admin.Commands.UpdateAdminUser;
using FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;
using FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Admin;

public class AdminFeatureTests {
    [Fact]
    public async Task UpdateAdminUserValidator_WithInvalidRole_Fails() {
        var validator = new UpdateAdminUserCommandValidator();
        var command = new UpdateAdminUserCommand(
            UserId.New(),
            IsActive: null,
            IsEmailConfirmed: null,
            Roles: ["Unknown"],
            Language: null,
            AiInputTokenLimit: null,
            AiOutputTokenLimit: null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Unknown role.");
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithUnknownRoleFromRepository_Fails() {
        var user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(user, availableRoles: [RoleNames.Admin]);
        var handler = new UpdateAdminUserCommandHandler(userRepository);
        var command = new UpdateAdminUserCommand(
            user.Id,
            IsActive: null,
            IsEmailConfirmed: null,
            Roles: [RoleNames.Admin, RoleNames.Support],
            Language: null,
            AiInputTokenLimit: null,
            AiOutputTokenLimit: null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("roles", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithNullRoles_DoesNotChangeRoles() {
        var user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin, RoleNames.Premium]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        var handler = new UpdateAdminUserCommandHandler(userRepository);
        var beforeRoles = user.UserRoles.Select(r => r.Role.Name).OrderBy(x => x).ToArray();

        var result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        var afterRoles = user.UserRoles.Select(r => r.Role.Name).OrderBy(x => x).ToArray();

        Assert.True(result.IsSuccess);
        Assert.Equal(beforeRoles, afterRoles);
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithEmptyRoles_ClearsRoles() {
        var user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin, RoleNames.Premium]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        var handler = new UpdateAdminUserCommandHandler(userRepository);

        var result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: Array.Empty<string>(),
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(user.UserRoles);
    }

    [Fact]
    public async Task UpsertAdminEmailTemplateValidator_WithInvalidLocale_Fails() {
        var validator = new UpsertAdminEmailTemplateCommandValidator();
        var command = new UpsertAdminEmailTemplateCommand(
            Key: "verify_email",
            Locale: "de",
            Subject: "Subject",
            HtmlBody: "<b>Body</b>",
            TextBody: "Body",
            IsActive: true);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("supported codes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAdminAiUsageSummaryQueryValidator_WithInvalidRange_Fails() {
        var validator = new GetAdminAiUsageSummaryQueryValidator();

        var result = await validator.ValidateAsync(
            new GetAdminAiUsageSummaryQuery(
                From: new DateOnly(2026, 2, 10),
                To: new DateOnly(2026, 2, 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("From", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAdminAiUsageSummaryQueryValidator_WithValidRange_Passes() {
        var validator = new GetAdminAiUsageSummaryQueryValidator();

        var result = await validator.ValidateAsync(
            new GetAdminAiUsageSummaryQuery(
                From: new DateOnly(2026, 2, 1),
                To: new DateOnly(2026, 2, 10)));

        Assert.True(result.IsValid);
    }

    private static User CreateUserWithRoles(string email, IReadOnlyList<string> roleNames) {
        var user = User.Create(email, "hash");
        var roles = roleNames.Select(name => Role.Create(name)).ToArray();
        foreach (var role in roles) {
            var userRole = new UserRole(user.Id, role.Id);
            user.UserRoles.Add(userRole);
            role.UserRoles.Add(userRole);
            SetNavigation(userRole, user, role);
        }

        return user;
    }

    private static void SetNavigation(UserRole userRole, User user, Role role) {
        typeof(UserRole).GetProperty(nameof(UserRole.User))!.SetValue(userRole, user);
        typeof(UserRole).GetProperty(nameof(UserRole.Role))!.SetValue(userRole, role);
    }

    private sealed class InMemoryUserRepository : IUserRepository {
        private readonly User _user;
        private readonly Dictionary<string, Role> _roles;

        public InMemoryUserRepository(User user, IEnumerable<string> availableRoles) {
            _user = user;
            _roles = availableRoles.ToDictionary(name => name, Role.Create, StringComparer.Ordinal);
        }

        public Task<User?> GetByEmailAsync(string email) => throw new NotSupportedException();

        public Task<User?> GetByEmailIncludingDeletedAsync(string email) => throw new NotSupportedException();

        public Task<User?> GetByIdAsync(UserId id) => Task.FromResult<User?>(_user.Id == id ? _user : null);

        public Task<User?> GetByIdIncludingDeletedAsync(UserId id) => Task.FromResult<User?>(_user.Id == id ? _user : null);

        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId) => throw new NotSupportedException();

        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId) => throw new NotSupportedException();

        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted) =>
            throw new NotSupportedException();

        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names) {
            var found = names.Where(name => _roles.ContainsKey(name)).Select(name => _roles[name]).ToList();
            return Task.FromResult<IReadOnlyList<Role>>(found);
        }

        public Task<User> AddAsync(User user) => throw new NotSupportedException();

        public Task UpdateAsync(User user) => Task.CompletedTask;
    }
}

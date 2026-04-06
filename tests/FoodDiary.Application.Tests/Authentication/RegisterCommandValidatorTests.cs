using FluentValidation.TestHelper;
using FoodDiary.Application.Authentication.Commands.Register;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Authentication;

public class RegisterCommandValidatorTests {
    [Fact]
    public async Task Register_WithEmptyEmail_HasError() {
        var validator = new RegisterCommandValidator(new StubUserRepository());
        var result = await validator.TestValidateAsync(new RegisterCommand("", "password1", null));
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_HasError() {
        var validator = new RegisterCommandValidator(new StubUserRepository());
        var result = await validator.TestValidateAsync(new RegisterCommand("not-email", "password1", null));
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public async Task Register_WithEmptyPassword_HasError() {
        var validator = new RegisterCommandValidator(new StubUserRepository());
        var result = await validator.TestValidateAsync(new RegisterCommand("user@test.com", "", null));
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public async Task Register_WithShortPassword_HasError() {
        var validator = new RegisterCommandValidator(new StubUserRepository());
        var result = await validator.TestValidateAsync(new RegisterCommand("user@test.com", "12345", null));
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public async Task Register_WithValidData_NoErrors() {
        var validator = new RegisterCommandValidator(new StubUserRepository());
        var result = await validator.TestValidateAsync(new RegisterCommand("user@test.com", "password1", "en"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_HasConflictError() {
        var existingUser = User.Create("taken@test.com", "hashed");
        var repo = new StubUserRepository();
        repo.SeedIncludingDeleted(existingUser);

        var validator = new RegisterCommandValidator(repo);
        var result = await validator.TestValidateAsync(new RegisterCommand("taken@test.com", "password1", null));

        result.ShouldHaveValidationErrorFor(c => c.Email)
            .WithErrorCode("Validation.Conflict");
    }

    [Fact]
    public async Task Register_WhenEmailBelongsToDeletedAccount_HasDeletedError() {
        var deletedUser = User.Create("deleted@test.com", "hashed");
        deletedUser.MarkDeleted(DateTime.UtcNow);
        var repo = new StubUserRepository();
        repo.SeedIncludingDeleted(deletedUser);

        var validator = new RegisterCommandValidator(repo);
        var result = await validator.TestValidateAsync(new RegisterCommand("deleted@test.com", "password1", null));

        result.ShouldHaveValidationErrorFor(c => c.Email)
            .WithErrorCode("Authentication.AccountDeleted");
    }

    private sealed class StubUserRepository : IUserRepository {
        private readonly List<User> _allUsers = [];

        public void SeedIncludingDeleted(User user) => _allUsers.Add(user);

        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(_allUsers.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken ct = default) => throw new NotSupportedException();
    }
}

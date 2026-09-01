using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Authentication;

public sealed partial class AuthenticationCommandHandlerTests {
    [Fact]
    public async Task AuthenticatePasswordAsync_WithLegacyHash_UpgradesHashAfterSuccessfulVerification() {
        const string password = "legacy-password";
        var user = User.Create("legacy-hash@example.com", $"legacy:{password}");
        user.RequirePasswordChange();
        var repository = new StubUserRepository(user);
        var hasher = new MigratingPasswordHasher();
        UserAuthenticationIdentityService service = CreateUserAuthenticationIdentityService(repository, hasher);

        Result<UserAuthenticationPrincipalModel> result = await service.AuthenticatePasswordAsync(
            user.Email,
            password,
            IdentityCoverageNow,
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal($"enhanced:{password}", user.Password),
            () => Assert.True(user.MustChangePassword),
            () => Assert.Equal(1, hasher.HashCallCount));
    }

    [Fact]
    public async Task AuthenticatePasswordAsync_WithInvalidPassword_DoesNotUpgradeHash() {
        var user = User.Create("legacy-invalid@example.com", "legacy:correct-password");
        var hasher = new MigratingPasswordHasher();
        UserAuthenticationIdentityService service = CreateUserAuthenticationIdentityService(
            new StubUserRepository(user),
            hasher);

        Result<UserAuthenticationPrincipalModel> result = await service.AuthenticatePasswordAsync(
            user.Email,
            "wrong-password",
            IdentityCoverageNow,
            CancellationToken.None);

        Assert.Equal("Authentication.InvalidCredentials", ResultAssert.Failure(result).Code);
        Assert.Multiple(
            () => Assert.Equal("legacy:correct-password", user.Password),
            () => Assert.Equal(0, hasher.HashCallCount));
    }

    [Fact]
    public async Task RestoreAccountAsync_WithLegacyHash_RestoresAndUpgradesHash() {
        const string password = "legacy-restore-password";
        var user = User.Create("legacy-restore@example.com", $"legacy:{password}");
        user.MarkDeleted(IdentityCoverageNow.AddDays(-1));
        var hasher = new MigratingPasswordHasher();
        UserAuthenticationIdentityService service = CreateUserAuthenticationIdentityService(
            new StubUserRepository(user),
            hasher);

        Result<UserAuthenticationPrincipalModel> result = await service.RestoreAccountAsync(
            user.Email,
            password,
            IdentityCoverageNow,
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Null(user.DeletedAt),
            () => Assert.True(user.IsActive),
            () => Assert.Equal($"enhanced:{password}", user.Password),
            () => Assert.Equal(1, hasher.HashCallCount));
    }

    [ExcludeFromCodeCoverage]
    private sealed class MigratingPasswordHasher : IPasswordHasher {
        public int HashCallCount { get; private set; }

        public string Hash(string password) {
            HashCallCount++;
            return $"enhanced:{password}";
        }

        public bool Verify(string password, string hashedPassword) =>
            string.Equals(hashedPassword, $"legacy:{password}", StringComparison.Ordinal) ||
            string.Equals(hashedPassword, $"enhanced:{password}", StringComparison.Ordinal);

        public bool NeedsRehash(string hashedPassword) =>
            hashedPassword.StartsWith("legacy:", StringComparison.Ordinal);
    }
}

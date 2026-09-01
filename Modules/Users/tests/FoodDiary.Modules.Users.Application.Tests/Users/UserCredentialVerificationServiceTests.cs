using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Users;

[ExcludeFromCodeCoverage]
public sealed class UserCredentialVerificationServiceTests {
    [Fact]
    public async Task VerifyPasswordAsync_WithMissingUser_ReturnsNotFound() {
        IUserLookupRepository users = Substitute.For<IUserLookupRepository>();
        var service = new UserCredentialVerificationService(users, Substitute.For<IPasswordHasher>());

        Result result = await service.VerifyPasswordAsync(UserId.New(), "password", CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("User.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task VerifyPasswordAsync_WithoutPassword_ReturnsPasswordNotSet() {
        var user = User.Create("passwordless@example.com", "placeholder", hasPassword: false);
        IUserLookupRepository users = CreateLookup(user);
        var service = new UserCredentialVerificationService(users, Substitute.For<IPasswordHasher>());

        Result result = await service.VerifyPasswordAsync(user.Id, "password", CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("User.PasswordNotSet", result.Error.Code);
    }

    [Fact]
    public async Task VerifyPasswordAsync_ReturnsInvalidPasswordOrSuccessWithoutExposingUser() {
        var user = User.Create("verified@example.com", "stored-hash");
        IUserLookupRepository users = CreateLookup(user);
        IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Verify("correct-password", "stored-hash").Returns(returnThis: true);
        var service = new UserCredentialVerificationService(users, passwordHasher);

        Result invalid = await service.VerifyPasswordAsync(user.Id, "wrong", CancellationToken.None);
        Result valid = await service.VerifyPasswordAsync(user.Id, "correct-password", CancellationToken.None);

        ResultAssert.Failure(invalid);
        Assert.Equal("User.InvalidPassword", invalid.Error.Code);
        ResultAssert.Success(valid);
    }

    private static IUserLookupRepository CreateLookup(User user) {
        IUserLookupRepository users = Substitute.For<IUserLookupRepository>();
        users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        return users;
    }
}

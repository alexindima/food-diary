using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Users;

[ExcludeFromCodeCoverage]
public sealed class TelegramIdentityRecoveryTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LinkGoogle_PasswordlessAccountClaimsOnlyAnAvailableVerifiedEmail(bool emailTaken) {
        var user = User.CreateTelegram(123, "hash");
        IUserLookupRepository lookup = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        lookup.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        if (emailTaken) {
            lookup.GetByEmailIncludingDeletedAsync("google@example.com", Arg.Any<CancellationToken>()).Returns(User.Create("google@example.com", "hash"));
        }
        var service = new UserAuthenticationIdentityService(lookup, writer, Substitute.For<IUserGoogleIdentityRepository>(), Substitute.For<IPasswordHasher>());

        Result<UserModel> result = await service.LinkGoogleAsync(user.Id, "google@example.com", "https://accounts.google.com", "subject", CancellationToken.None);

        Assert.Equal(!emailTaken, result.IsSuccess);
        if (emailTaken) {
            Assert.Equal("User.EmailAlreadyExists", result.Error.Code);
            Assert.Null(user.Email);
            Assert.Null(user.GoogleSubject);
        } else {
            Assert.Equal("google@example.com", user.Email);
            Assert.True(user.IsEmailConfirmed);
            Assert.Equal("subject", user.GoogleSubject);
            Assert.False(user.HasPassword);
        }
        await writer.Received(emailTaken ? 0 : 1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PasswordlessAccount_RequiresEmailBeforeRequestingVerificationAndCannotReplaceTelegram() {
        var user = User.CreateTelegram(123, "hash");
        IUserLookupRepository lookup = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        lookup.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var service = new UserAuthenticationIdentityService(lookup, writer, Substitute.For<IUserGoogleIdentityRepository>(), Substitute.For<IPasswordHasher>());
        DateTime now = DateTime.UtcNow;

        Result<UserEmailVerificationDeliveryModel?> email = await service.IssueEmailVerificationAsync(user.Id, "hash", now.AddHours(1), now, TimeSpan.FromMinutes(1), CancellationToken.None);
        Result<UserModel> telegram = await service.LinkTelegramAsync(user.Id, 456, CancellationToken.None);

        Assert.Equal("User.EmailRequired", email.Error.Code);
        Assert.Equal("User.TelegramIdentityDifferent", telegram.Error.Code);
        Assert.Empty(writer.ReceivedCalls());
        Assert.Equal(123, user.TelegramUserId);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("Invalid/Zone", false)]
    [InlineData("Asia/Tbilisi", true)]
    [InlineData(" UTC ", true)]
    public void TimeZoneInput_ValidatesOptionalIanaValue(string? zone, bool valid) => Assert.Equal(valid, UserTimeZoneInput.IsValid(zone));

    [Fact]
    public void TimeZoneInput_RejectsOversizedValue() => Assert.False(UserTimeZoneInput.IsValid(new string('x', 101)));
}

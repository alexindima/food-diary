using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Services;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Users;

[ExcludeFromCodeCoverage]
public sealed class UserNotificationProfileServiceTests {
    [Fact]
    public async Task UpdatePreferencesAsync_WhenUserIsMissing_ReturnsFailureWithoutWriting() {
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        var service = new UserNotificationProfileService(Substitute.For<IUserLookupRepository>(), writer);

        Result<UserNotificationProfileModel> result = await service.UpdatePreferencesAsync(
            UserId.New(),
            new UserPreferenceUpdate(),
            CancellationToken.None);

        ResultAssert.Failure(result, "Authentication.InvalidToken");
        await writer.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }
}

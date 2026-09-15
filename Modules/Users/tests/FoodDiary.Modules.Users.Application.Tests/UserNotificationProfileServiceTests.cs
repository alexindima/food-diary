using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Application.Services;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Tests;

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

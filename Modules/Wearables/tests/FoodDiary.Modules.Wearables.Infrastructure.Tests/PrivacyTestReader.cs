using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Wearables.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
internal sealed class PrivacyTestReader : IUserPrivacyStateReader {
    public static IUserPrivacyStateReader Accepted() => new PrivacyTestReader();
    public Task<UserPrivacyStateModel?> GetAsync(UserId userId, CancellationToken cancellationToken) =>
        Task.FromResult<UserPrivacyStateModel?>(new UserPrivacyStateModel("2026-11-02", DateTime.UtcNow, AiAcceptedAtUtc: null, MarketingAcceptedAtUtc: null));
}

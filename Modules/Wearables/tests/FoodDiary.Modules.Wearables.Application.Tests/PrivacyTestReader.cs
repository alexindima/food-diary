using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Wearables.Application.Tests;

[ExcludeFromCodeCoverage]
internal sealed class PrivacyTestReader(bool accepted = true) : IUserPrivacyStateReader {
    public static IUserPrivacyStateReader Accepted() => new PrivacyTestReader();
    public static IUserPrivacyStateReader Denied() => new PrivacyTestReader(accepted: false);
    public Task<UserPrivacyStateModel?> GetAsync(UserId userId, CancellationToken cancellationToken) =>
        Task.FromResult<UserPrivacyStateModel?>(accepted ? new UserPrivacyStateModel("2026-11-02", DateTime.UtcNow, AiAcceptedAtUtc: null, MarketingAcceptedAtUtc: null) : null);
}

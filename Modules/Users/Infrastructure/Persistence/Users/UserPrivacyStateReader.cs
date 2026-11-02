using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.Privacy;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

internal sealed class UserPrivacyStateReader(UsersDbContext context) : IUserPrivacyStateReader {
    public Task<UserPrivacyStateModel?> GetAsync(UserId userId, CancellationToken cancellationToken) =>
        context.Users.AsNoTracking().Where(user => user.Id == userId && user.IsActive && user.DeletedAt == null)
            .Select(user => new UserPrivacyStateModel(PrivacyNoticeVersions.Current,
                user.HealthConsentNoticeVersion == PrivacyNoticeVersions.Current ? user.HealthConsentAcceptedAtUtc : null,
                user.AiConsentNoticeVersion == PrivacyNoticeVersions.Current
                    || (PrivacyNoticeVersions.Current == PrivacyNoticeVersions.LegacyCompatibleAiNotice && (user.AiConsentNoticeVersion == null || user.AiConsentNoticeVersion == PrivacyNoticeVersions.LegacyUnversioned))
                    ? user.AiConsentAcceptedAt : null,
                user.MarketingConsentNoticeVersion == PrivacyNoticeVersions.Current ? user.MarketingConsentAcceptedAtUtc : null,
                user.HealthConsentDeclinedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
}

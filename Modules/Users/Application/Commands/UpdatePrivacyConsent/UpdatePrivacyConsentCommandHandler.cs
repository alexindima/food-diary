using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.Privacy;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Commands.UpdatePrivacyConsent;

public sealed class UpdatePrivacyConsentCommandHandler(IUserContextService users, TimeProvider clock)
    : ICommandHandler<UpdatePrivacyConsentCommand, Result> {
    public async Task<Result> Handle(UpdatePrivacyConsentCommand command, CancellationToken cancellationToken) {
        if (!Enum.IsDefined((PrivacyConsentPurpose)command.Purpose)
            || string.IsNullOrWhiteSpace(command.NoticeVersion) || command.NoticeVersion.Length > 64
            || (command.Granted && !string.Equals(command.NoticeVersion, PrivacyNoticeVersions.Current, StringComparison.Ordinal))) {
            return Result.Failure(new Error("Privacy.InvalidNotice", "Review the current privacy notice before saving your choice.", ErrorKind.Validation));
        }
        Result<UserId> id = await CurrentUserAccessResolver.ResolveAsync(command.UserId, users, cancellationToken);
        if (id.IsFailure) { return Result.Failure(id.Error); }
        Result<User> user = await users.GetAccessibleUserAsync(id.Value, cancellationToken);
        if (user.IsFailure) { return Result.Failure(user.Error); }
        user.Value.RecordPrivacyConsent((PrivacyConsentPurpose)command.Purpose, command.Granted, command.NoticeVersion, clock.GetUtcNow().UtcDateTime);
        await users.UpdateUserAsync(user.Value, cancellationToken);
        return Result.Success();
    }
}

using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Hydration.Application.Mappings;
using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Hydration.Application.Abstractions.Common;
using FoodDiary.Modules.Hydration.Application.Internal;

using FoodDiary.Modules.Hydration.Contracts.Models;
using FoodDiary.Modules.Hydration.Application.Validators;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Hydration.Application.Commands.CreateHydrationEntry;

public sealed class CreateHydrationEntryCommandHandler(
    IHydrationEntryWriteRepository repository,
    ICurrentUserAccessService currentUserAccessService) : ICommandHandler<CreateHydrationEntryCommand, Result<HydrationEntryModel>> {
    public async Task<Result<HydrationEntryModel>> Handle(
        CreateHydrationEntryCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<HydrationEntryModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result validation = HydrationValidators.ValidateAmount(command.AmountMl);
        if (validation.IsFailure) {
            return Result.Failure<HydrationEntryModel>(validation.Error);
        }

        DateTime timestampUtc = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(command.TimestampUtc);
        var entry = HydrationEntry.Create(userId, timestampUtc, command.AmountMl);
        await repository.AddAsync(entry, cancellationToken).ConfigureAwait(false);

        return Result.Success(entry.ToModel());
    }
}

using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Hydration.Mappings;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.Hydration.Validators;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;

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

using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Commands.StartFasting;

public class StartFastingCommandHandler(
    IFastingPlanWriteRepository fastingPlanRepository,
    IFastingOccurrenceWriteRepository fastingOccurrenceRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider dateTimeProvider)
    : ICommandHandler<StartFastingCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        StartFastingCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<FastingSessionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<FastingSessionModel>(accessError);
        }

        FastingPlan? currentPlan = await fastingPlanRepository.GetActiveAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (currentPlan is not null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.AlreadyActive);
        }

        DateTime startedAtUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        Result<(FastingPlan Plan, FastingOccurrence Occurrence)> creation = FastingStartFactory.Create(command, userId, startedAtUtc);
        if (creation.IsFailure) {
            return Result.Failure<FastingSessionModel>(creation.Error);
        }

        (FastingPlan plan, FastingOccurrence? occurrence) = creation.Value;

        await fastingPlanRepository.AddAsync(plan, cancellationToken).ConfigureAwait(false);
        await fastingOccurrenceRepository.AddAsync(occurrence, cancellationToken).ConfigureAwait(false);

        return Result.Success(occurrence.ToModel(plan));
    }
}

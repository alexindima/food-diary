using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Commands.PostponeCyclicDay;

public sealed class PostponeCyclicDayCommandHandler(
    IFastingPlanWriteRepository fastingPlanRepository,
    IFastingOccurrenceWriteRepository fastingOccurrenceRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider dateTimeProvider)
    : ICommandHandler<PostponeCyclicDayCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        PostponeCyclicDayCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<FastingSessionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<FastingSessionModel>(accessError);
        }

        FastingOccurrence? current = await fastingOccurrenceRepository.GetCurrentAsync(userId, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (current is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        FastingPlan? plan = current.Plan ?? await fastingPlanRepository.GetActiveAsync(userId, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (plan is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }
        if (!FastingCyclicTransitionPlanner.CanTransition(plan, current)) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.InvalidCyclicAction("Only an active cyclic period can be postponed."));
        }

        DateTime now = dateTimeProvider.GetUtcNow().UtcDateTime;
        try {
            var postponedUntil = DateTime.SpecifyKind(now.Date.AddDays(1), DateTimeKind.Utc);
            current.Postpone(now, postponedUntil);
            plan.ScheduleNextCyclicPhase(postponedUntil);
        } catch (ArgumentOutOfRangeException) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.InvalidCyclicAction("The cyclic period can only be postponed to a later date."));
        } catch (InvalidOperationException) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.InvalidCyclicAction("The current cyclic period cannot be postponed."));
        }

        FastingOccurrence nextOccurrence = FastingCyclicTransitionPlanner.CreateAfterPostpone(plan, current, userId, now);

        await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken).ConfigureAwait(false);
        await fastingPlanRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);
        await fastingOccurrenceRepository.AddAsync(nextOccurrence, cancellationToken).ConfigureAwait(false);

        return Result.Success(nextOccurrence.ToModel(plan));
    }
}

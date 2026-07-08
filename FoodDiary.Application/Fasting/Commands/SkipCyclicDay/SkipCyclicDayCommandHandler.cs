using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Commands.SkipCyclicDay;

public sealed class SkipCyclicDayCommandHandler(
    IFastingPlanWriteRepository fastingPlanRepository,
    IFastingOccurrenceWriteRepository fastingOccurrenceRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider dateTimeProvider)
    : ICommandHandler<SkipCyclicDayCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        SkipCyclicDayCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await FastingCurrentUserResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return FastingCurrentUserResolver.ToSessionFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        FastingOccurrence? current = await fastingOccurrenceRepository.GetCurrentAsync(userId, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (current is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        FastingPlan? plan = current.Plan ?? await fastingPlanRepository.GetActiveAsync(userId, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (plan is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }
        if (!FastingCyclicTransitionPlanner.CanTransition(plan, current)) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.InvalidCyclicAction("Only an active cyclic period can be skipped."));
        }

        DateTime now = dateTimeProvider.GetUtcNow().UtcDateTime;
        try {
            current.Skip(now);
            plan.ScheduleNextCyclicPhase(DateTime.SpecifyKind(now.Date.AddDays(1), DateTimeKind.Utc));
        } catch (InvalidOperationException) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.InvalidCyclicAction("The current cyclic period cannot be skipped."));
        }

        FastingOccurrence nextOccurrence = FastingCyclicTransitionPlanner.CreateAfterSkip(plan, current, userId, now);

        await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken).ConfigureAwait(false);
        await fastingPlanRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);
        await fastingOccurrenceRepository.AddAsync(nextOccurrence, cancellationToken).ConfigureAwait(false);

        return Result.Success(nextOccurrence.ToModel(plan));
    }
}

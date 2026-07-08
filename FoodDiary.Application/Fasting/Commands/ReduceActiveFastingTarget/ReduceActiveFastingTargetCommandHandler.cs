using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Commands.ReduceActiveFastingTarget;

public sealed class ReduceActiveFastingTargetCommandHandler(
    IFastingPlanWriteRepository fastingPlanRepository,
    IFastingOccurrenceWriteRepository fastingOccurrenceRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider dateTimeProvider)
    : ICommandHandler<ReduceActiveFastingTargetCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        ReduceActiveFastingTargetCommand command,
        CancellationToken cancellationToken) {
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
        if (plan.Type != FastingPlanType.Extended) {
            return Result.Failure<FastingSessionModel>(
                Errors.Validation.Invalid(nameof(command.ReducedHours), "Only extended fasting target can be reduced."));
        }

        try {
            current.Reduce(command.ReducedHours);
        } catch (ArgumentOutOfRangeException) {
            return Result.Failure<FastingSessionModel>(
                Errors.Validation.Invalid(nameof(command.ReducedHours), "Reduced fasting hours are invalid."));
        } catch (InvalidOperationException) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        DateTime now = dateTimeProvider.GetUtcNow().UtcDateTime;
        if (current.TargetHours.HasValue && now >= current.StartedAtUtc.AddHours(current.TargetHours.Value)) {
            current.Complete(now);
            plan.Stop(now);
            await fastingPlanRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);
        }

        await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken).ConfigureAwait(false);
        return Result.Success(current.ToModel(plan));
    }
}

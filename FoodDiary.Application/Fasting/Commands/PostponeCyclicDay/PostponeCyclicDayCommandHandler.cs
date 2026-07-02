using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Commands.PostponeCyclicDay;

public sealed class PostponeCyclicDayCommandHandler(
    IFastingPlanRepository fastingPlanRepository,
    IFastingOccurrenceRepository fastingOccurrenceRepository,
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
        if (plan.Type != FastingPlanType.Cyclic ||
            (current.Kind != FastingOccurrenceKind.FastDay && current.Kind != FastingOccurrenceKind.EatDay)) {
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

        FastingOccurrenceKind nextKind = ResolveNextKind(plan, current);
        int? nextTargetHours = nextKind == FastingOccurrenceKind.FastDay
            ? 24
            : plan.CyclicEatDayEatingWindowHours;
        int nextSequenceNumber = ResolveNextSequenceNumber(plan, current, nextKind);

        var nextOccurrence = FastingOccurrence.Create(
            plan.Id,
            userId,
            nextKind,
            now,
            nextSequenceNumber,
            targetHours: nextTargetHours,
            notes: current.Notes);

        await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken).ConfigureAwait(false);
        await fastingPlanRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);
        await fastingOccurrenceRepository.AddAsync(nextOccurrence, cancellationToken).ConfigureAwait(false);

        return Result.Success(nextOccurrence.ToModel(plan));
    }

    private static FastingOccurrenceKind ResolveNextKind(FastingPlan plan, FastingOccurrence current) {
        if (current.Kind == FastingOccurrenceKind.EatDay) {
            int phaseFastDays = Math.Max(1, plan.CyclicFastDays ?? 1);
            int phaseEatDays = Math.Max(1, plan.CyclicEatDays ?? 1);
            int phaseTotalCycleDays = phaseFastDays + phaseEatDays;
            int phaseOverallCycleDay = ((Math.Max(1, current.SequenceNumber) - 1) % phaseTotalCycleDays) + 1;
            int eatCycleDay = phaseOverallCycleDay <= phaseFastDays ? 1 : phaseOverallCycleDay - phaseFastDays;

            return eatCycleDay >= phaseEatDays
                ? FastingOccurrenceKind.FastDay
                : FastingOccurrenceKind.EatDay;
        }

        int fastDays = Math.Max(1, plan.CyclicFastDays ?? 1);
        int eatDays = Math.Max(1, plan.CyclicEatDays ?? 1);
        int totalCycleDays = fastDays + eatDays;
        int overallCycleDay = ((Math.Max(1, current.SequenceNumber) - 1) % totalCycleDays) + 1;
        int fastCycleDay = Math.Min(overallCycleDay, fastDays);

        return fastCycleDay >= fastDays
            ? FastingOccurrenceKind.EatDay
            : FastingOccurrenceKind.FastDay;
    }

    private static int ResolveNextSequenceNumber(FastingPlan plan, FastingOccurrence current, FastingOccurrenceKind nextKind) {
        int fastDays = Math.Max(1, plan.CyclicFastDays ?? 1);
        int eatDays = Math.Max(1, plan.CyclicEatDays ?? 1);
        int totalCycleDays = fastDays + eatDays;
        int overallCycleDay = ((Math.Max(1, current.SequenceNumber) - 1) % totalCycleDays) + 1;
        int cycleStartSequence = current.SequenceNumber - (overallCycleDay - 1);

        if (current.Kind == FastingOccurrenceKind.FastDay) {
            return nextKind == FastingOccurrenceKind.FastDay
                ? current.SequenceNumber + 1
                : cycleStartSequence + fastDays;
        }

        return nextKind == FastingOccurrenceKind.EatDay
            ? current.SequenceNumber + 1
            : cycleStartSequence + totalCycleDays;
    }
}

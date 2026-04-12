using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Persistence;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Commands.PostponeCyclicDay;

public sealed class PostponeCyclicDayCommandHandler(
    IFastingPlanRepository fastingPlanRepository,
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<PostponeCyclicDayCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        PostponeCyclicDayCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<FastingSessionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var current = await fastingOccurrenceRepository.GetCurrentAsync(userId, asTracking: true, cancellationToken);
        if (current is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        var plan = current.Plan ?? await fastingPlanRepository.GetActiveAsync(userId, asTracking: true, cancellationToken);
        if (plan is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        if (plan.Type != FastingPlanType.Cyclic ||
            (current.Kind != FastingOccurrenceKind.FastDay && current.Kind != FastingOccurrenceKind.EatDay)) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.InvalidCyclicAction("Only an active cyclic period can be postponed."));
        }

        var now = dateTimeProvider.UtcNow;
        var postponedUntil = DateTime.SpecifyKind(now.Date.AddDays(1), DateTimeKind.Utc);
        try {
            current.Postpone(now, postponedUntil);
            plan.ScheduleNextCyclicPhase(postponedUntil);
        } catch (ArgumentOutOfRangeException) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.InvalidCyclicAction("The cyclic period can only be postponed to a later date."));
        } catch (InvalidOperationException) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.InvalidCyclicAction("The current cyclic period cannot be postponed."));
        }

        var nextKind = ResolveNextKind(plan, current);
        var nextTargetHours = nextKind == FastingOccurrenceKind.FastDay
            ? 24
            : plan.CyclicEatDayEatingWindowHours;
        var nextSequenceNumber = ResolveNextSequenceNumber(plan, current, nextKind);

        var nextOccurrence = FastingOccurrence.Create(
            plan.Id,
            userId,
            nextKind,
            now,
            nextSequenceNumber,
            targetHours: nextTargetHours,
            notes: current.Notes);

        await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken);
        await fastingPlanRepository.UpdateAsync(plan, cancellationToken);
        await fastingOccurrenceRepository.AddAsync(nextOccurrence, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(nextOccurrence.ToModel(plan));
    }

    private static FastingOccurrenceKind ResolveNextKind(FastingPlan plan, FastingOccurrence current) {
        if (current.Kind == FastingOccurrenceKind.EatDay) {
            var phaseFastDays = Math.Max(1, plan.CyclicFastDays ?? 1);
            var phaseEatDays = Math.Max(1, plan.CyclicEatDays ?? 1);
            var phaseTotalCycleDays = phaseFastDays + phaseEatDays;
            var phaseOverallCycleDay = ((Math.Max(1, current.SequenceNumber) - 1) % phaseTotalCycleDays) + 1;
            var eatCycleDay = phaseOverallCycleDay <= phaseFastDays ? 1 : phaseOverallCycleDay - phaseFastDays;

            return eatCycleDay >= phaseEatDays
                ? FastingOccurrenceKind.FastDay
                : FastingOccurrenceKind.EatDay;
        }

        var fastDays = Math.Max(1, plan.CyclicFastDays ?? 1);
        var eatDays = Math.Max(1, plan.CyclicEatDays ?? 1);
        var totalCycleDays = fastDays + eatDays;
        var overallCycleDay = ((Math.Max(1, current.SequenceNumber) - 1) % totalCycleDays) + 1;
        var fastCycleDay = Math.Min(overallCycleDay, fastDays);

        return fastCycleDay >= fastDays
            ? FastingOccurrenceKind.EatDay
            : FastingOccurrenceKind.FastDay;
    }

    private static int ResolveNextSequenceNumber(FastingPlan plan, FastingOccurrence current, FastingOccurrenceKind nextKind) {
        var fastDays = Math.Max(1, plan.CyclicFastDays ?? 1);
        var eatDays = Math.Max(1, plan.CyclicEatDays ?? 1);
        var totalCycleDays = fastDays + eatDays;
        var overallCycleDay = ((Math.Max(1, current.SequenceNumber) - 1) % totalCycleDays) + 1;
        var cycleStartSequence = current.SequenceNumber - (overallCycleDay - 1);

        if (current.Kind == FastingOccurrenceKind.FastDay && nextKind == FastingOccurrenceKind.FastDay) {
            return current.SequenceNumber + 1;
        }

        if (current.Kind == FastingOccurrenceKind.FastDay && nextKind == FastingOccurrenceKind.EatDay) {
            return cycleStartSequence + fastDays;
        }

        if (current.Kind == FastingOccurrenceKind.EatDay && nextKind == FastingOccurrenceKind.EatDay) {
            return current.SequenceNumber + 1;
        }

        if (current.Kind == FastingOccurrenceKind.EatDay && nextKind == FastingOccurrenceKind.FastDay) {
            return cycleStartSequence + totalCycleDays;
        }

        return current.SequenceNumber + 1;
    }
}

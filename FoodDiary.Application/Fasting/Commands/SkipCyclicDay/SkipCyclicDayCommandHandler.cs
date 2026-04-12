using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Persistence;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Commands.SkipCyclicDay;

public sealed class SkipCyclicDayCommandHandler(
    IFastingPlanRepository fastingPlanRepository,
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SkipCyclicDayCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        SkipCyclicDayCommand command, CancellationToken cancellationToken) {
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
            return Result.Failure<FastingSessionModel>(Errors.Fasting.InvalidCyclicAction("Only an active cyclic period can be skipped."));
        }

        var now = dateTimeProvider.UtcNow;
        try {
            current.Skip(now);
            plan.ScheduleNextCyclicPhase(DateTime.SpecifyKind(now.Date.AddDays(1), DateTimeKind.Utc));
        } catch (InvalidOperationException) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.InvalidCyclicAction("The current cyclic period cannot be skipped."));
        }

        var nextKind = current.Kind == FastingOccurrenceKind.FastDay
            ? FastingOccurrenceKind.EatDay
            : FastingOccurrenceKind.FastDay;
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

    private static int ResolveNextSequenceNumber(FastingPlan plan, FastingOccurrence current, FastingOccurrenceKind nextKind) {
        var fastDays = Math.Max(1, plan.CyclicFastDays ?? 1);
        var eatDays = Math.Max(1, plan.CyclicEatDays ?? 1);
        var totalCycleDays = fastDays + eatDays;
        var overallCycleDay = ((Math.Max(1, current.SequenceNumber) - 1) % totalCycleDays) + 1;
        var cycleStartSequence = current.SequenceNumber - (overallCycleDay - 1);

        return nextKind switch {
            FastingOccurrenceKind.FastDay => cycleStartSequence + totalCycleDays,
            FastingOccurrenceKind.EatDay => cycleStartSequence + fastDays,
            _ => current.SequenceNumber + 1,
        };
    }
}

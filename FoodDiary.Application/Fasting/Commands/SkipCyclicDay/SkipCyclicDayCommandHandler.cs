using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Commands.SkipCyclicDay;

public sealed class SkipCyclicDayCommandHandler(
    IFastingPlanRepository fastingPlanRepository,
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IUserRepository userRepository,
    TimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SkipCyclicDayCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        SkipCyclicDayCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<FastingSessionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
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
            return Result.Failure<FastingSessionModel>(Errors.Fasting.InvalidCyclicAction("Only an active cyclic period can be skipped."));
        }

        DateTime now = dateTimeProvider.GetUtcNow().UtcDateTime;
        try {
            current.Skip(now);
            plan.ScheduleNextCyclicPhase(DateTime.SpecifyKind(now.Date.AddDays(1), DateTimeKind.Utc));
        } catch (InvalidOperationException) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.InvalidCyclicAction("The current cyclic period cannot be skipped."));
        }

        FastingOccurrenceKind nextKind = current.Kind == FastingOccurrenceKind.FastDay
            ? FastingOccurrenceKind.EatDay
            : FastingOccurrenceKind.FastDay;
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
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(nextOccurrence.ToModel(plan));
    }

    private static int ResolveNextSequenceNumber(FastingPlan plan, FastingOccurrence current, FastingOccurrenceKind nextKind) {
        int fastDays = Math.Max(1, plan.CyclicFastDays ?? 1);
        int eatDays = Math.Max(1, plan.CyclicEatDays ?? 1);
        int totalCycleDays = fastDays + eatDays;
        int overallCycleDay = ((Math.Max(1, current.SequenceNumber) - 1) % totalCycleDays) + 1;
        int cycleStartSequence = current.SequenceNumber - (overallCycleDay - 1);

        return nextKind == FastingOccurrenceKind.FastDay
            ? cycleStartSequence + totalCycleDays
            : cycleStartSequence + fastDays;
    }
}

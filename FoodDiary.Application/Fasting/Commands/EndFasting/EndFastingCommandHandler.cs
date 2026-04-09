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

namespace FoodDiary.Application.Fasting.Commands.EndFasting;

public class EndFastingCommandHandler(
    IFastingPlanRepository fastingPlanRepository,
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<EndFastingCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        EndFastingCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<FastingSessionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var current = await fastingOccurrenceRepository.GetCurrentAsync(userId, asTracking: true, cancellationToken);
        if (current is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        var plan = current.Plan ?? await fastingPlanRepository.GetActiveAsync(userId, asTracking: true, cancellationToken);
        if (plan is null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.NoActiveSession);
        }

        var now = dateTimeProvider.UtcNow;
        if (plan.Type == FastingPlanType.Cyclic) {
            current.Complete(now);
            var nextOccurrence = await CreateNextCyclicOccurrenceAsync(plan, current, userId, now, cancellationToken);
            await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken);
            await fastingOccurrenceRepository.AddAsync(nextOccurrence, cancellationToken);
            await fastingPlanRepository.UpdateAsync(plan, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(nextOccurrence.ToModel(plan));
        }

        if (plan.Type == FastingPlanType.Intermittent) {
            current.Complete(now);
        } else {
            var targetReachedAtUtc = current.StartedAtUtc.AddHours(current.TargetHours ?? 0);
            if (now >= targetReachedAtUtc) {
                current.Complete(now);
            } else {
                current.Interrupt(now);
            }
        }

        plan.Stop(now);
        await fastingOccurrenceRepository.UpdateAsync(current, cancellationToken);
        await fastingPlanRepository.UpdateAsync(plan, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(current.ToModel(plan));
    }

    private async Task<FastingOccurrence> CreateNextCyclicOccurrenceAsync(
        FastingPlan plan,
        FastingOccurrence current,
        UserId userId,
        DateTime now,
        CancellationToken cancellationToken) {
        var occurrences = await fastingOccurrenceRepository.GetByPlanAsync(plan.Id, includeCompleted: true, cancellationToken);
        var nextKind = ResolveNextCyclicKind(plan, current, occurrences);
        var nextTargetHours = nextKind == FastingOccurrenceKind.FastDay
            ? 24
            : plan.CyclicEatDayEatingWindowHours;

        plan.ScheduleNextCyclicPhase(DateTime.SpecifyKind(now.Date.AddDays(1), DateTimeKind.Utc));

        return FastingOccurrence.Create(
            plan.Id,
            userId,
            nextKind,
            now,
            current.SequenceNumber + 1,
            targetHours: nextTargetHours,
            notes: current.Notes);
    }

    private static FastingOccurrenceKind ResolveNextCyclicKind(
        FastingPlan plan,
        FastingOccurrence current,
        IReadOnlyList<FastingOccurrence> occurrences) {
        if (current.Kind == FastingOccurrenceKind.EatDay &&
            occurrences.Any(x => x.Kind == FastingOccurrenceKind.FastDay && x.Status == FastingOccurrenceStatus.Postponed)) {
            return FastingOccurrenceKind.FastDay;
        }

        var streak = occurrences
            .Where(x => x.SequenceNumber <= current.SequenceNumber && x.Status != FastingOccurrenceStatus.Postponed)
            .OrderByDescending(x => x.SequenceNumber)
            .TakeWhile(x => x.Kind == current.Kind)
            .Count();

        if (current.Kind == FastingOccurrenceKind.FastDay) {
            return streak < (plan.CyclicFastDays ?? 1)
                ? FastingOccurrenceKind.FastDay
                : FastingOccurrenceKind.EatDay;
        }

        return streak < (plan.CyclicEatDays ?? 1)
            ? FastingOccurrenceKind.EatDay
            : FastingOccurrenceKind.FastDay;
    }
}

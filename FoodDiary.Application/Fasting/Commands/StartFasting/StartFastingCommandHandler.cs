using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Commands.StartFasting;

public class StartFastingCommandHandler(
    IFastingPlanRepository fastingPlanRepository,
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IUserRepository userRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<StartFastingCommand, Result<FastingSessionModel>> {
    public async Task<Result<FastingSessionModel>> Handle(
        StartFastingCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<FastingSessionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<FastingSessionModel>(accessError);
        }

        FastingPlan? currentPlan = await fastingPlanRepository.GetActiveAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (currentPlan is not null) {
            return Result.Failure<FastingSessionModel>(Errors.Fasting.AlreadyActive);
        }

        Result<FastingPlanType> planTypeResult = ResolvePlanType(command);
        if (planTypeResult.IsFailure) {
            return Result.Failure<FastingSessionModel>(planTypeResult.Error);
        }

        DateTime startedAtUtc = dateTimeProvider.UtcNow;
        FastingPlanType planType = planTypeResult.Value;
        Result<(FastingPlan Plan, FastingOccurrence Occurrence)> creation = CreatePlanAndOccurrence(command, userId, planType, startedAtUtc, command.Notes);
        if (creation.IsFailure) {
            return Result.Failure<FastingSessionModel>(creation.Error);
        }

        (FastingPlan? plan, FastingOccurrence? occurrence) = creation.Value;

        await fastingPlanRepository.AddAsync(plan, cancellationToken).ConfigureAwait(false);
        await fastingOccurrenceRepository.AddAsync(occurrence, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(occurrence.ToModel(plan));
    }

    private static Result<FastingPlanType> ResolvePlanType(StartFastingCommand command) {
        if (!string.IsNullOrWhiteSpace(command.PlanType)) {
            return Enum.TryParse<FastingPlanType>(command.PlanType, ignoreCase: true, out FastingPlanType explicitPlanType)
                ? Result.Success(explicitPlanType)
                : Result.Failure<FastingPlanType>(Errors.Fasting.InvalidProtocol);
        }

        if (string.IsNullOrWhiteSpace(command.Protocol) ||
            !Enum.TryParse<FastingProtocol>(command.Protocol, ignoreCase: true, out FastingProtocol protocol)) {
            return Result.Success(FastingPlanType.Intermittent);
        }

        FastingPlanType planType = protocol switch {
            FastingProtocol.F16_8 or FastingProtocol.F18_6 or FastingProtocol.F20_4 or FastingProtocol.CustomIntermittent => FastingPlanType.Intermittent,
            _ => FastingPlanType.Extended,
        };

        return Result.Success(planType);
    }

    private static Result<(FastingPlan Plan, FastingOccurrence Occurrence)> CreatePlanAndOccurrence(
        StartFastingCommand command,
        UserId userId,
        FastingPlanType planType,
        DateTime startedAtUtc,
        string? notes) {
        try {
            return planType switch {
                FastingPlanType.Intermittent => CreateIntermittent(command, userId, startedAtUtc, notes),
                FastingPlanType.Extended => CreateExtended(command, userId, startedAtUtc, notes),
                FastingPlanType.Cyclic => CreateCyclic(command, userId, startedAtUtc, notes),
                _ => Result.Failure<(FastingPlan, FastingOccurrence)>(Errors.Fasting.InvalidProtocol),
            };
        } catch (ArgumentOutOfRangeException) {
            return Result.Failure<(FastingPlan, FastingOccurrence)>(Errors.Fasting.InvalidProtocol);
        }
    }

    private static Result<(FastingPlan Plan, FastingOccurrence Occurrence)> CreateIntermittent(
        StartFastingCommand command,
        UserId userId,
        DateTime startedAtUtc,
        string? notes) {
        if (string.IsNullOrWhiteSpace(command.Protocol) ||
            !Enum.TryParse<FastingProtocol>(command.Protocol, ignoreCase: true, out FastingProtocol protocol)) {
            return Result.Failure<(FastingPlan, FastingOccurrence)>(Errors.Fasting.InvalidProtocol);
        }

        int duration = command.PlannedDurationHours ?? FastingSession.GetDefaultDuration(protocol);
        if (protocol == FastingProtocol.CustomIntermittent && (duration < 1 || duration >= 24)) {
            return Result.Failure<(FastingPlan, FastingOccurrence)>(Errors.Fasting.InvalidProtocol);
        }

        var plan = FastingPlan.CreateIntermittent(userId, protocol, duration, 24 - duration, startedAtUtc);
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            userId,
            FastingOccurrenceKind.FastingWindow,
            startedAtUtc,
            sequenceNumber: 1,
            targetHours: duration,
            notes: notes);

        return Result.Success((plan, occurrence));
    }

    private static Result<(FastingPlan Plan, FastingOccurrence Occurrence)> CreateExtended(
        StartFastingCommand command,
        UserId userId,
        DateTime startedAtUtc,
        string? notes) {
        if (string.IsNullOrWhiteSpace(command.Protocol) ||
            !Enum.TryParse<FastingProtocol>(command.Protocol, ignoreCase: true, out FastingProtocol protocol)) {
            return Result.Failure<(FastingPlan, FastingOccurrence)>(Errors.Fasting.InvalidProtocol);
        }

        int duration = command.PlannedDurationHours ?? FastingSession.GetDefaultDuration(protocol);
        var plan = FastingPlan.CreateExtended(userId, protocol, duration, startedAtUtc);
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            userId,
            FastingOccurrenceKind.FastDay,
            startedAtUtc,
            sequenceNumber: 1,
            targetHours: duration,
            notes: notes);

        return Result.Success((plan, occurrence));
    }

    private static Result<(FastingPlan Plan, FastingOccurrence Occurrence)> CreateCyclic(
        StartFastingCommand command,
        UserId userId,
        DateTime startedAtUtc,
        string? notes) {
        int fastDays = command.CyclicFastDays ?? 1;
        int eatDays = command.CyclicEatDays ?? 1;
        int eatDayFastHours = command.CyclicEatDayFastHours ?? 16;
        int eatDayEatingWindowHours = command.CyclicEatDayEatingWindowHours ?? 8;

        var plan = FastingPlan.CreateCyclic(
            userId,
            fastDays,
            eatDays,
            eatDayFastHours,
            eatDayEatingWindowHours,
            startedAtUtc,
            startedAtUtc);
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            userId,
            FastingOccurrenceKind.FastDay,
            startedAtUtc,
            sequenceNumber: 1,
            targetHours: 24,
            notes: notes);

        return Result.Success((plan, occurrence));
    }
}

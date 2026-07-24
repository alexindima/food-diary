using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Queries.GetAttentionSignals;

public sealed record GetAttentionSignalsQuery(
    Guid? UserId,
    int InactivityDays,
    double CalorieDeviationPercent,
    int SustainedDays,
    double WeightChangePercent,
    int LookbackDays)
    : IQuery<Result<IReadOnlyList<AttentionSignalModel>>>, IUserRequest;

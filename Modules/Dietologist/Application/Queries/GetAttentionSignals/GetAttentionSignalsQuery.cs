using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetAttentionSignals;

public sealed record GetAttentionSignalsQuery(
    Guid? UserId,
    int InactivityDays,
    double CalorieDeviationPercent,
    int SustainedDays,
    double WeightChangePercent,
    int LookbackDays)
    : IQuery<Result<IReadOnlyList<AttentionSignalModel>>>, IUserRequest;

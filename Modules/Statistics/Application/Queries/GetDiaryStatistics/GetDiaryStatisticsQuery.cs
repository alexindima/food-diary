using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Statistics.Queries.GetDiaryStatistics;

public sealed record GetDiaryStatisticsQuery(Guid? UserId, int Days = 1) : IQuery<Result<DiaryStatisticsSummaryModel>>, IUserRequest;

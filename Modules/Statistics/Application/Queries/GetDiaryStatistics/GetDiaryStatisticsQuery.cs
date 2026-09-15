using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Statistics.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Statistics.Application.Queries.GetDiaryStatistics;

public sealed record GetDiaryStatisticsQuery(Guid? UserId, int Days = 1) : IQuery<Result<DiaryStatisticsSummaryModel>>, IUserRequest;

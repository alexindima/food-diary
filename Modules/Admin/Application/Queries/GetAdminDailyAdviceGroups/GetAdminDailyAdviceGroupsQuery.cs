using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminDailyAdviceGroups;

public sealed record GetAdminDailyAdviceGroupsQuery(int Page = 1, int Limit = 50) : IQuery<Result<IReadOnlyList<AdminDailyAdviceGroupModel>>>;

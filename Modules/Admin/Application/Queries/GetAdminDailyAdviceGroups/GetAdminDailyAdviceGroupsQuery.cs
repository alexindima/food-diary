using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminDailyAdviceGroups;

public sealed record GetAdminDailyAdviceGroupsQuery : IQuery<Result<IReadOnlyList<AdminDailyAdviceGroupModel>>>;

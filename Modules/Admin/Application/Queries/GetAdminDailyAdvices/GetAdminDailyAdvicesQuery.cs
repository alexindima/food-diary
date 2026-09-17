using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminDailyAdvices;

public sealed record GetAdminDailyAdvicesQuery : IQuery<Result<IReadOnlyList<AdminDailyAdviceModel>>>;

using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminRetention;

public sealed record GetAdminRetentionQuery(DateOnly? From, DateOnly? To, DateOnly? CohortFrom = null, DateOnly? CohortTo = null) : IQuery<Result<AdminRetentionReport>>;

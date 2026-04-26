using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Billing.Queries.GetBillingOverview;

public sealed record GetBillingOverviewQuery(Guid? UserId) : IQuery<Result<BillingOverviewModel>>, IUserRequest;

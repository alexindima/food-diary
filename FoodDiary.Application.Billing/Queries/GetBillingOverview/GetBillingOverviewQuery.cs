using FoodDiary.Application.Billing.Models;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Billing.Queries.GetBillingOverview;

public sealed record GetBillingOverviewQuery(Guid? UserId) : IRequest<Result<BillingOverviewModel>>;

using FoodDiary.Modules.Billing.Application.Models;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Queries.GetBillingOverview;

public sealed record GetBillingOverviewQuery(Guid? UserId) : IRequest<Result<BillingOverviewModel>>;

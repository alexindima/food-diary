using FoodDiary.Modules.Identity.Contracts.Authentication.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Identity.Contracts.Authentication.Queries.GetLoginDeviceSummary;

public sealed record GetLoginDeviceSummaryQuery(DateTime? FromUtc, DateTime? ToUtc) : IRequest<IReadOnlyList<UserLoginDeviceSummaryModel>>;

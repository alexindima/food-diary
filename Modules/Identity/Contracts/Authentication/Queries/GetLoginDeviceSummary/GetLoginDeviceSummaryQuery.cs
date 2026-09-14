using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Authentication.Queries.GetLoginDeviceSummary;

public sealed record GetLoginDeviceSummaryQuery(DateTime? FromUtc, DateTime? ToUtc) : IRequest<IReadOnlyList<UserLoginDeviceSummaryModel>>;

using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Authentication.Queries.GetLoginEvents;

public sealed record GetLoginEventsQuery(int Page, int Limit, Guid? UserId, string? Search, DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, string? Provider = null, string? Device = null) : IRequest<(IReadOnlyList<UserLoginEventReadModel> Items, int TotalItems)>;

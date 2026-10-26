using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Authentication.Queries.GetBrowserSessionProfile;

public sealed record GetBrowserSessionProfileQuery(UserId UserId) : IQuery<Result<UserAuthenticationPrincipalModel>>;

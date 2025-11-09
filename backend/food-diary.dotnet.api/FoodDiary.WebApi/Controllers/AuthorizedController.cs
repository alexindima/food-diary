using MediatR;
using Microsoft.AspNetCore.Authorization;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.WebApi.Extensions;

namespace FoodDiary.WebApi.Controllers;

[Authorize]
public abstract class AuthorizedController(ISender mediator) : BaseApiController(mediator) {
    protected UserId? CurrentUserId => User.GetUserId();
    protected Guid? CurrentUserGuid => CurrentUserId?.Value;
}

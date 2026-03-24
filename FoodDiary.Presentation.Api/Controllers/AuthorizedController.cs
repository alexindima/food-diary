using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace FoodDiary.Presentation.Api.Controllers;

[Authorize]
public abstract class AuthorizedController(ISender mediator) : BaseApiController(mediator) {
    protected UserId? CurrentUserId => User.GetUserId();
    protected Guid? CurrentUserGuid => CurrentUserId?.Value;

    protected bool TryGetCurrentUserId(out UserId userId) {
        if (CurrentUserId is null || CurrentUserId.Value == UserId.Empty) {
            userId = default;
            return false;
        }

        userId = CurrentUserId.Value;
        return true;
    }
}

using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;

namespace FoodDiary.Presentation.Api.Controllers;

[Authorize]
public abstract class AuthorizedController(ISender mediator) : BaseApiController(mediator);

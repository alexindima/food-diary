using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Controllers;

public abstract class BaseApiController(ISender mediator) : ControllerBase {
    protected ISender Mediator { get; } = mediator;
}

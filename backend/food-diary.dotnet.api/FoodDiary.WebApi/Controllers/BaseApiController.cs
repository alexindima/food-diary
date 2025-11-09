using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.WebApi.Controllers;

public abstract class BaseApiController(ISender mediator) : ControllerBase {
    protected ISender Mediator { get; } = mediator;
}

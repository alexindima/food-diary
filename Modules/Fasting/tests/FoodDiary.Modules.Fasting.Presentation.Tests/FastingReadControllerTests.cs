using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Modules.Fasting.Application.Queries.GetCurrentFasting;
using FoodDiary.Mediator;
using FoodDiary.Modules.Fasting.Presentation.Controllers;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Fasting.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class FastingReadControllerTests {
    [Fact]
    public async Task GetCurrent_WhenNoSession_ReturnsNoContentAndSendsUserQuery() {
        var userId = Guid.NewGuid();
        IRequest<Result<FastingSessionModel?>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(
            Result.Success<FastingSessionModel?>(value: null),
            request => sentRequest = request);
        var controller = new FastingReadController(sender) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        IActionResult result = await controller.GetCurrent(userId);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(userId, Assert.IsType<GetCurrentFastingQuery>(sentRequest).UserId);
    }
}

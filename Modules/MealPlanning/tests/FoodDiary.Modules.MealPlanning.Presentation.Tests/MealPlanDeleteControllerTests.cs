using FoodDiary.Mediator;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.DeleteMealPlan;
using FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Controllers;
using FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Mappings;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.MealPlanning.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class MealPlanDeleteControllerTests {
    [Fact]
    public async Task Delete_MapsRouteAndCurrentUserAndPropagatesRequestCancellation() {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IRequest<Result>>(), cancellation.Token).Returns(Result.Success());
        var controller = new MealPlansController(sender) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestAborted = cancellation.Token } },
        };

        IActionResult response = await controller.Delete(userId, planId);

        Assert.IsType<NoContentResult>(response);
        await sender.Received(1).Send(Arg.Is<IRequest<Result>>(new DeleteMealPlanCommand(userId, planId)), cancellation.Token);
    }

    [Fact]
    public void ToDeleteCommand_PreservesCurrentUserAndPlanId() {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var command = userId.ToDeleteMealPlanCommand(planId);
        Assert.Multiple(() => Assert.Equal(userId, command.UserId), () => Assert.Equal(planId, command.PlanId));
    }
}

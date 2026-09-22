using FoodDiary.Mediator;
using FoodDiary.Modules.Favorites.Application.FavoriteMeals.Commands.RestoreFavoriteMeal;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Modules.Favorites.Presentation.Contracts.Features.FavoriteMeals.Responses;
using FoodDiary.Modules.Favorites.Presentation.Features.FavoriteMeals.Controllers;
using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Favorites.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class FavoriteMealsRestoreControllerTests {
    [Fact]
    public async Task RestoreUsesCurrentOwnerAndOriginalFavoriteIdAsync() {
        var favorite = new FavoriteMealModel(Guid.NewGuid(), Guid.NewGuid(), "Lunch", DateTime.UtcNow, DateTime.UtcNow,
            "Lunch", 500, 20, 10, 30, 2);
        IRequest<Result<FavoriteMealModel>>? sent = null;
        ISender sender = SubstituteSender.Create(Result.Success(favorite), request => sent = request);
        var controller = new FavoriteMealsController(sender) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        var owner = Guid.NewGuid();
        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.Restore(favorite.Id, owner));
        FavoriteMealHttpResponse response = Assert.IsType<FavoriteMealHttpResponse>(result.Value);
        RestoreFavoriteMealCommand command = Assert.IsType<RestoreFavoriteMealCommand>(sent);
        Assert.Multiple(
            () => Assert.Equal(owner, command.UserId),
            () => Assert.Equal(favorite.Id, command.FavoriteMealId),
            () => Assert.Equal(favorite.Id, response.Id),
            () => Assert.Equal(favorite.CreatedAtUtc, response.CreatedAtUtc));
    }
}

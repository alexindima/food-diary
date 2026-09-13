using FoodDiary.Application.Hydration.Commands.CreateHydrationFromOperation;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Hydration;
using FoodDiary.Presentation.Api.Features.Hydration.Requests;
using FoodDiary.Presentation.Api.Features.Hydration.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class HydrationOperationControllerTests {
    [Fact]
    public async Task CreateFromOperation_PreservesOwnerOperationTimeAndSavedReceipt() {
        var owner = Guid.NewGuid();
        var model = new HydrationOperationModel(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, 250);
        IRequest<Result<HydrationOperationModel>>? sent = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sent = request);
        var controller = new HydrationEntriesController(sender, TimeProvider.System) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        var request = new CreateHydrationFromOperationHttpRequest(model.TimestampUtc, model.AmountMl);

        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.CreateFromOperation(model.OperationId, owner, request));

        CreateHydrationFromOperationCommand command = Assert.IsType<CreateHydrationFromOperationCommand>(sent);
        HydrationOperationHttpResponse response = Assert.IsType<HydrationOperationHttpResponse>(result.Value);
        Assert.Multiple(
            () => Assert.Equal(owner, command.UserId),
            () => Assert.Equal(model.OperationId, command.OperationId),
            () => Assert.Equal(request.TimestampUtc, command.TimestampUtc),
            () => Assert.Equal(request.AmountMl, command.AmountMl),
            () => Assert.Equal(model.OperationId, response.OperationId),
            () => Assert.Equal(model.EntryId, response.EntryId),
            () => Assert.Equal(model.TimestampUtc, response.TimestampUtc),
            () => Assert.Equal(model.AmountMl, response.AmountMl));
    }
}

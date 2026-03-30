using FoodDiary.Application.Authentication.Commands.Register;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Products.Commands.CreateProduct;
using FoodDiary.Application.Products.Queries.GetProducts;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class HandlerPipelineIntegrationTests(PostgresApiWebApplicationFactory factory)
    : IClassFixture<PostgresApiWebApplicationFactory> {

    [RequiresDockerFact]
    public async Task CreateProduct_ThroughMediatR_PersistsToPostgres() {
        using var scope = factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var registerResult = await mediator.Send(new RegisterCommand(
            $"handler-test-{Guid.NewGuid():N}@example.com",
            "Password123!",
            "en"));

        Assert.True(registerResult.IsSuccess);
        var userId = registerResult.Value.User!.Id;

        var createResult = await mediator.Send(new CreateProductCommand(
            UserId: userId,
            Barcode: null,
            Name: "Test Product",
            Brand: null,
            ProductType: "Food",
            Category: null,
            Description: null,
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            BaseUnit: "g",
            BaseAmount: 100,
            DefaultPortionAmount: 0,
            CaloriesPerBase: 250,
            ProteinsPerBase: 20,
            FatsPerBase: 10,
            CarbsPerBase: 30,
            FiberPerBase: 0,
            AlcoholPerBase: 0,
            Visibility: "Private"));

        Assert.True(createResult.IsSuccess);
        Assert.NotNull(createResult.Value);

        var getResult = await mediator.Send(new GetProductsQuery(
            UserId: userId,
            Page: 1,
            Limit: 10,
            Search: null,
            IncludePublic: false,
            ProductTypes: null));

        Assert.True(getResult.IsSuccess);
        Assert.Single(getResult.Value.Data);
        Assert.Equal("Test Product", getResult.Value.Data[0].Name);
    }

    [RequiresDockerFact]
    public async Task CreateProduct_WithInvalidData_ReturnsValidationFailure() {
        using var scope = factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new CreateProductCommand(
            UserId: null,
            Barcode: null,
            Name: "",
            Brand: null,
            ProductType: "Food",
            Category: null,
            Description: null,
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            BaseUnit: "g",
            BaseAmount: 100,
            DefaultPortionAmount: 0,
            CaloriesPerBase: 0,
            ProteinsPerBase: 0,
            FatsPerBase: 0,
            CarbsPerBase: 0,
            FiberPerBase: 0,
            AlcoholPerBase: 0,
            Visibility: "Private"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
    }

    [RequiresDockerFact]
    public async Task MultipleProducts_PersistedViaUnitOfWork() {
        using var scope = factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var registerResult = await mediator.Send(new RegisterCommand(
            $"uow-test-{Guid.NewGuid():N}@example.com",
            "Password123!",
            "en"));

        Assert.True(registerResult.IsSuccess);
        var userId = registerResult.Value.User!.Id;

        var product1 = await mediator.Send(new CreateProductCommand(
            UserId: userId, Barcode: null, Name: "Product A", Brand: null,
            ProductType: "Food", Category: null, Description: null, Comment: null,
            ImageUrl: null, ImageAssetId: null, BaseUnit: "g", BaseAmount: 100,
            DefaultPortionAmount: 0, CaloriesPerBase: 100, ProteinsPerBase: 10,
            FatsPerBase: 5, CarbsPerBase: 15, FiberPerBase: 0, AlcoholPerBase: 0,
            Visibility: "Private"));

        var product2 = await mediator.Send(new CreateProductCommand(
            UserId: userId, Barcode: null, Name: "Product B", Brand: null,
            ProductType: "Food", Category: null, Description: null, Comment: null,
            ImageUrl: null, ImageAssetId: null, BaseUnit: "g", BaseAmount: 100,
            DefaultPortionAmount: 0, CaloriesPerBase: 200, ProteinsPerBase: 20,
            FatsPerBase: 10, CarbsPerBase: 25, FiberPerBase: 0, AlcoholPerBase: 0,
            Visibility: "Private"));

        Assert.True(product1.IsSuccess);
        Assert.True(product2.IsSuccess);

        var getResult = await mediator.Send(new GetProductsQuery(
            UserId: userId, Page: 1, Limit: 10,
            Search: null, IncludePublic: false, ProductTypes: null));

        Assert.True(getResult.IsSuccess);
        Assert.Equal(2, getResult.Value.Data.Count);
    }
}

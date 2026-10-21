using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.SaveCatalogMealPlan;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetCatalogMealPlan;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetCatalogMealPlans;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.SearchCatalogRecipes;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.Tests.MealPlans;

[ExcludeFromCodeCoverage]
public sealed class CatalogMealPlanBoundaryTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CatalogPage_ForwardsPagingAndCancellationAndPreservesRepositoryOrder(bool empty) {
        IMealPlanCatalogRepository repository = Substitute.For<IMealPlanCatalogRepository>();
        using var cancellation = new CancellationTokenSource();
        MealPlanSummaryReadModel[] rows = empty ? [] : [
            new(Guid.NewGuid(), "Second page first", "Description", "Balanced", 2, 2100, IsCurated: true, 3),
            new(Guid.NewGuid(), "Second page second", Description: null, "Balanced", 1, TargetCaloriesPerDay: null, IsCurated: true, 1),
        ];
        repository.GetPageAsync(2, 7, cancellation.Token).Returns(rows);

        IReadOnlyList<MealPlanSummaryModel> result = ResultAssert.Success(await new GetCatalogMealPlansQueryHandler(repository)
            .Handle(new GetCatalogMealPlansQuery(2, 7), cancellation.Token));

        Assert.Equal(rows.Length, result.Count);
        if (!empty) {
            Assert.Equal(new MealPlanSummaryModel(rows[0].Id, "Second page first", "Description", "Balanced", 2, 2100, IsCurated: true, 3), result[0]);
            Assert.Equal(rows[1].Id, result[1].Id);
        }
        await repository.Received(1).GetPageAsync(2, 7, cancellation.Token);
        Assert.Single(repository.ReceivedCalls());
    }

    [Fact]
    public async Task CatalogRecipeSearch_ForwardsTheExactSearchAndReturnsTheReaderResult() {
        IMealPlanCatalogRecipeReader reader = Substitute.For<IMealPlanCatalogRecipeReader>();
        using var cancellation = new CancellationTokenSource();
        CatalogRecipeReadModel[] rows = [new(Guid.NewGuid(), "Soup", 2)];
        reader.SearchAsync("  soup  ", 11, cancellation.Token).Returns(rows);

        IReadOnlyList<CatalogRecipeReadModel> result = ResultAssert.Success(await new SearchCatalogRecipesQueryHandler(reader)
            .Handle(new SearchCatalogRecipesQuery("  soup  ", 11), cancellation.Token));

        Assert.Same(rows, result);
        await reader.Received(1).SearchAsync("  soup  ", 11, cancellation.Token);
        Assert.Single(reader.ReceivedCalls());
    }

    [Fact]
    public async Task CatalogDetail_RejectsAnEmptyIdBeforeReadingAnyPlan() {
        IMealPlanCompositionReader reader = Substitute.For<IMealPlanCompositionReader>();

        ResultAssert.Failure(await new GetCatalogMealPlanQueryHandler(reader).Handle(new GetCatalogMealPlanQuery(Guid.Empty), CancellationToken.None));

        Assert.Empty(reader.ReceivedCalls());
    }

    [Fact]
    public async Task CatalogDetail_DoesNotExposeAPrivateOwnersPlan() {
        IMealPlanCompositionReader reader = Substitute.For<IMealPlanCompositionReader>();
        var id = MealPlanId.New();
        reader.GetReadModelByIdAsync(id, viewerId: null, CancellationToken.None).Returns(
            new MealPlanReadModel(id.Value, Guid.NewGuid(), "Private", Description: null, "Balanced", 1, TargetCaloriesPerDay: null, IsCurated: false, []));

        ResultAssert.Failure(await new GetCatalogMealPlanQueryHandler(reader).Handle(new GetCatalogMealPlanQuery(id.Value), CancellationToken.None));

        await reader.Received(1).GetReadModelByIdAsync(id, viewerId: null, CancellationToken.None);
    }

    [Fact]
    public async Task Save_ReportsValidationErrorsBeforeAnyRepositoryOrRecipeCalls() {
        var dependencies = new SaveDependencies();
        var command = new SaveCatalogMealPlanCommand(Id: null, " ", new string('d', 2049), "Unknown", 0,
            TargetCaloriesPerDay: null, IsPublished: false, []);

        Error error = ResultAssert.Failure(await dependencies.Handler.Handle(command, CancellationToken.None), "MealPlan.InvalidCatalogPlan");

        Assert.Equal(ErrorKind.Validation, error.Kind);
        Assert.NotEmpty(error.Message);
        Assert.Empty(dependencies.Catalog.ReceivedCalls());
        Assert.Empty(dependencies.Writer.ReceivedCalls());
        Assert.Empty(dependencies.Recipes.ReceivedCalls());
        Assert.Empty(dependencies.Composition.ReceivedCalls());
    }

    [Fact]
    public async Task Save_WhenTheExistingPlanDisappeared_DoesNotCreateAReplacement() {
        var dependencies = new SaveDependencies();
        var id = MealPlanId.New();
        using var cancellation = new CancellationTokenSource();
        dependencies.Recipes.GetPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), cancellation.Token).Returns(new HashSet<Guid>());
        var command = new SaveCatalogMealPlanCommand(id.Value, "Missing", Description: null, "Balanced", 1,
            TargetCaloriesPerDay: null, IsPublished: false, []);

        ResultAssert.Failure(await dependencies.Handler.Handle(command, cancellation.Token));

        await dependencies.Catalog.Received(1).GetForUpdateAsync(id, cancellation.Token);
        Assert.Empty(dependencies.Writer.ReceivedCalls());
        Assert.Empty(dependencies.Composition.ReceivedCalls());
        await dependencies.Transaction.Received(1).ExecuteAsync(Arg.Any<Func<CancellationToken, Task<Result<MealPlanModel>>>>(), cancellation.Token);
    }

    [ExcludeFromCodeCoverage]
    private sealed class SaveDependencies {
        public IMealPlanCatalogRepository Catalog { get; } = Substitute.For<IMealPlanCatalogRepository>();
        public IMealPlanWriteRepository Writer { get; } = Substitute.For<IMealPlanWriteRepository>();
        public IMealPlanCatalogRecipeReader Recipes { get; } = Substitute.For<IMealPlanCatalogRecipeReader>();
        public IMealPlanCompositionReader Composition { get; } = Substitute.For<IMealPlanCompositionReader>();
        public IMealPlanCatalogTransactionRunner Transaction { get; } = Substitute.For<IMealPlanCatalogTransactionRunner>();
        public SaveCatalogMealPlanCommandHandler Handler => new(Catalog, Writer, Recipes, Composition, Transaction);

        public SaveDependencies() => Transaction.ExecuteAsync(Arg.Any<Func<CancellationToken, Task<Result<MealPlanModel>>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<Result<MealPlanModel>>>>()(call.Arg<CancellationToken>()));
    }
}

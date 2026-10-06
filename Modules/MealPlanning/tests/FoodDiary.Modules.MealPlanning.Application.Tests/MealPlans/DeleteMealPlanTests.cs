using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.DeleteMealPlan;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.Tests.MealPlans;

[ExcludeFromCodeCoverage]
public sealed class DeleteMealPlanTests {
    [Fact]
    public async Task Delete_ForwardsCurrentOwnerAndCancellationToOwnedRepositoryOperation() {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        IMealPlanWriteRepository repository = Substitute.For<IMealPlanWriteRepository>();
        repository.DeletePersonalAsync(new MealPlanId(planId), new UserId(userId), cancellation.Token).Returns(returnThis: true);
        var handler = new DeleteMealPlanCommandHandler(repository, Substitute.For<ICurrentUserAccessService>());

        Result result = await handler.Handle(new DeleteMealPlanCommand(userId, planId), cancellation.Token);

        ResultAssert.Success(result);
        await repository.Received(1).DeletePersonalAsync(new MealPlanId(planId), new UserId(userId), cancellation.Token);
    }

    [Fact]
    public async Task Delete_WhenNoOwnedPersonalPlanMatches_ReturnsNonDisclosingNotFound() {
        var handler = new DeleteMealPlanCommandHandler(Substitute.For<IMealPlanWriteRepository>(), Substitute.For<ICurrentUserAccessService>());
        Result result = await handler.Handle(new DeleteMealPlanCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("MealPlan.NotFound", result.Error.Code);
        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
    }

    [Fact]
    public async Task Delete_WithInvalidPlanId_DoesNotCallRepository() {
        IMealPlanWriteRepository repository = Substitute.For<IMealPlanWriteRepository>();
        var handler = new DeleteMealPlanCommandHandler(repository, Substitute.For<ICurrentUserAccessService>());
        Result result = await handler.Handle(new DeleteMealPlanCommand(Guid.NewGuid(), Guid.Empty), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("MealPlan.InvalidId", result.Error.Code);
        await repository.DidNotReceiveWithAnyArgs().DeletePersonalAsync(default, default, default);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Delete_WithoutCurrentUser_DoesNotCallRepository(string? userIdValue) {
        IMealPlanWriteRepository repository = Substitute.For<IMealPlanWriteRepository>();
        var handler = new DeleteMealPlanCommandHandler(repository, Substitute.For<ICurrentUserAccessService>());
        Guid? userId = userIdValue is null ? null : Guid.Parse(userIdValue);
        Result result = await handler.Handle(new DeleteMealPlanCommand(userId, Guid.NewGuid()), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        await repository.DidNotReceiveWithAnyArgs().DeletePersonalAsync(default, default, default);
    }

    [Fact]
    public async Task Delete_WhenAccessIsDenied_DoesNotMutate() {
        var denied = new Error("Authentication.AccountDeleted", "The account was deleted.");
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(denied);
        IMealPlanWriteRepository repository = Substitute.For<IMealPlanWriteRepository>();
        var handler = new DeleteMealPlanCommandHandler(repository, access);
        Result result = await handler.Handle(new DeleteMealPlanCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal(denied, result.Error);
        await repository.DidNotReceiveWithAnyArgs().DeletePersonalAsync(default, default, default);
    }

    [Fact]
    public async Task Delete_WhenCancelled_DoesNotReachPersistence() {
        IMealPlanWriteRepository repository = Substitute.For<IMealPlanWriteRepository>();
        var handler = new DeleteMealPlanCommandHandler(repository, Substitute.For<ICurrentUserAccessService>());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.Handle(
            new DeleteMealPlanCommand(Guid.NewGuid(), Guid.NewGuid()), cancellation.Token));
        await repository.DidNotReceiveWithAnyArgs().DeletePersonalAsync(default, default, default);
    }

    [Fact]
    public async Task Validator_RejectsEmptyIdentifiers() {
        FluentValidation.Results.ValidationResult result = await new DeleteMealPlanCommandValidator().ValidateAsync(new DeleteMealPlanCommand(Guid.Empty, Guid.Empty));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.ErrorCode, "Authentication.InvalidToken", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => string.Equals(error.ErrorCode, "MealPlan.InvalidId", StringComparison.Ordinal));
    }
}

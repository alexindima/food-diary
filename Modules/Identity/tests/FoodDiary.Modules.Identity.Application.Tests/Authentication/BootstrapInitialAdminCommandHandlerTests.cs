using FoodDiary.Application.Identity.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class BootstrapInitialAdminCommandHandlerTests {
    [Fact]
    public async Task Handle_DelegatesCommandValuesAndCancellationToken() {
        IInitialAdminBootstrapService service = Substitute.For<IInitialAdminBootstrapService>();
        var model = new BootstrapInitialAdminModel(BootstrapInitialAdminStatus.Created, "admin@example.com");
        service.BootstrapAsync("admin@example.com", "password", Arg.Any<CancellationToken>())
            .Returns(Result.Success(model));
        var handler = new BootstrapInitialAdminCommandHandler(service);
        var command = new BootstrapInitialAdminCommand("admin@example.com", "password");
        using var cancellationTokenSource = new CancellationTokenSource();

        Result<BootstrapInitialAdminModel> result =
            await handler.Handle(command, cancellationTokenSource.Token);

        BootstrapInitialAdminModel value = ResultAssert.Success(result);
        Assert.Same(model, value);
        await service.Received(1).BootstrapAsync(
            command.Email,
            command.Password,
            cancellationTokenSource.Token);
    }
}

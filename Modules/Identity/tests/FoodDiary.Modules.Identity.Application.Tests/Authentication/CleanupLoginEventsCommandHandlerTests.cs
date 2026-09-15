using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.CleanupLoginEvents;
using FoodDiary.Modules.Identity.Contracts.Authentication.Commands.CleanupLoginEvents;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class CleanupLoginEventsCommandHandlerTests {
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Handle_InvalidBatchSizeRejectsBeforeDatabaseAccess(int batchSize) {
        IUserLoginEventWriteRepository repository = Substitute.For<IUserLoginEventWriteRepository>();
        var handler = new CleanupLoginEventsCommandHandler(repository);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => handler.Handle(
            new CleanupLoginEventsCommand(DateTime.UtcNow, batchSize), CancellationToken.None));
        Assert.Empty(repository.ReceivedCalls());
    }
}

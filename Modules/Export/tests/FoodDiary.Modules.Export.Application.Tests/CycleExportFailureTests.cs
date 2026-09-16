using FoodDiary.Mediator;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Modules.Cycles.Contracts.Queries.GetCurrentCycle;
using FoodDiary.Modules.Export.Application.Queries.ExportCycle;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Export.Application.Models;

namespace FoodDiary.Modules.Export.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class CycleExportFailureTests {
    [Fact]
    public async Task SourceFailureIsPreservedWithoutExportAsync() {
        var userId = UserId.New();
        var error = new Error("Cycles.Unavailable", "Cycle source unavailable");
        ISender sender = Substitute.For<ISender>();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>()).Returns((Error?)null);
        sender.Send(Arg.Any<GetCurrentCycleQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<CycleModel?>(error));
        var handler = new ExportCycleQueryHandler(sender, access);
        var date = new DateOnly(2026, 9, 16);
        Result<FileExportResult> result = await handler.Handle(new ExportCycleQuery(userId.Value, date, date), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }
}

using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Models;
using FoodDiary.Modules.DailyAdvices.Application.Commands.ImportDailyAdvices;
using FoodDiary.Modules.DailyAdvices.Contracts.Commands.ImportDailyAdvices;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Modules.DailyAdvices.Domain.Entities.Content;
using FoodDiary.Results;

namespace FoodDiary.Modules.DailyAdvices.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ImportDailyAdvicesTests {
    [Fact]
    public async Task ImportNormalizesAndSkipsExistingAndWithinFileDuplicates() {
        IDailyAdviceReadModelRepository reader = Substitute.For<IDailyAdviceReadModelRepository>();
        reader.GetAllReadModelsAsync(Arg.Any<CancellationToken>()).Returns(new DailyAdviceReadModel[] {
            new(Guid.NewGuid(), "ru", "Drink water", "hydration", 1),
        });
        IDailyAdviceWriteRepository writer = Substitute.For<IDailyAdviceWriteRepository>();
        IReadOnlyList<DailyAdvice>? added = null;
        writer.AddRangeAsync(Arg.Any<IReadOnlyList<DailyAdvice>>(), Arg.Any<CancellationToken>())
            .Returns(call => { added = call.Arg<IReadOnlyList<DailyAdvice>>(); return Task.CompletedTask; });
        var handler = new ImportDailyAdvicesCommandHandler(reader, writer);
        using var cancellation = new CancellationTokenSource();
        Result<DailyAdviceImportModel> result = await handler.Handle(new ImportDailyAdvicesCommand([
            new(" Drink water ", " RU-ru ", 1, " hydration "),
            new("Walk", "en"), new("Walk", "en-US", 1, " "),
        ]), cancellation.Token);
        ResultAssert.Success(result);
        Assert.Multiple(() => Assert.Equal(1, result.Value.ImportedCount), () => Assert.Equal(2, result.Value.SkippedCount));
        DailyAdvice advice = Assert.Single(added!);
        Assert.Multiple(() => Assert.Equal("Walk", advice.Value), () => Assert.Equal("en", advice.Locale), () => Assert.Null(advice.Tag));
        await writer.Received(1).AddRangeAsync(Arg.Any<IReadOnlyList<DailyAdvice>>(), cancellation.Token);
    }

    [Theory]
    [InlineData("", "en", 1)]
    [InlineData("Advice", "de", 1)]
    [InlineData("Advice", "ru", 0)]
    public async Task InvalidItemRejectsEntireFileBeforeStagingWrites(string value, string locale, int weight) {
        IDailyAdviceReadModelRepository reader = Substitute.For<IDailyAdviceReadModelRepository>();
        IDailyAdviceWriteRepository writer = Substitute.For<IDailyAdviceWriteRepository>();
        var handler = new ImportDailyAdvicesCommandHandler(reader, writer);
        Result<DailyAdviceImportModel> result = await handler.Handle(new ImportDailyAdvicesCommand([
            new("Valid", "en"), new(value, locale, weight),
        ]), CancellationToken.None);
        ResultAssert.Failure(result);
        await writer.DidNotReceiveWithAnyArgs().AddRangeAsync(default!, default);
        await reader.DidNotReceiveWithAnyArgs().GetAllReadModelsAsync(default);
    }

    [Fact]
    public async Task ImportRejectsNullItemsAndOversizedTextAndBatches() {
        IDailyAdviceReadModelRepository reader = Substitute.For<IDailyAdviceReadModelRepository>();
        IDailyAdviceWriteRepository writer = Substitute.For<IDailyAdviceWriteRepository>();
        var handler = new ImportDailyAdvicesCommandHandler(reader, writer);
        IReadOnlyList<DailyAdviceImportItem>[] invalid = [null!, [], [null!], [new(new string('x', 513), "en")],
            [new("Advice", "en", 1, new string('x', 65))], Enumerable.Repeat(new DailyAdviceImportItem("Advice", "en"), 1001).ToArray()];
        foreach (IReadOnlyList<DailyAdviceImportItem> items in invalid) {
            ResultAssert.Failure(await handler.Handle(new ImportDailyAdvicesCommand(items), CancellationToken.None));
        }
        await writer.DidNotReceiveWithAnyArgs().AddRangeAsync(default!, default);
    }

    [Fact]
    public async Task ReimportDoesNotStageWrites() {
        IDailyAdviceReadModelRepository reader = Substitute.For<IDailyAdviceReadModelRepository>();
        reader.GetAllReadModelsAsync(Arg.Any<CancellationToken>()).Returns(new DailyAdviceReadModel[] { new(Guid.NewGuid(), "en", "Advice", null, 1) });
        IDailyAdviceWriteRepository writer = Substitute.For<IDailyAdviceWriteRepository>();
        Result<DailyAdviceImportModel> result = await new ImportDailyAdvicesCommandHandler(reader, writer)
            .Handle(new ImportDailyAdvicesCommand([new("Advice", "en")]), CancellationToken.None);
        ResultAssert.Success(result);
        Assert.Multiple(() => Assert.Equal(0, result.Value.ImportedCount), () => Assert.Equal(1, result.Value.SkippedCount));
        await writer.DidNotReceiveWithAnyArgs().AddRangeAsync(default!, default);
    }
}

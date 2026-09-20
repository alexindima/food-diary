using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Application.Commands.ImportDailyAdvicePairs;
using FoodDiary.Modules.DailyAdvices.Application.Commands.UpdateDailyAdviceGroup;
using FoodDiary.Modules.DailyAdvices.Contracts.Commands.ImportDailyAdvicePairs;
using FoodDiary.Modules.DailyAdvices.Contracts.Commands.UpdateDailyAdviceGroup;
using FoodDiary.Modules.DailyAdvices.Domain.Entities.Content;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.DailyAdvices.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DailyAdvicePairImportValidationTests {
    [Theory]
    [InlineData("null")]
    [InlineData("empty")]
    [InlineData("oversized")]
    [InlineData("null-item")]
    [InlineData("empty-id")]
    [InlineData("duplicate-id")]
    [InlineData("duplicate-text")]
    [InlineData("invalid-text")]
    public async Task InvalidFile_DoesNotAccessRepository(string scenario) {
        var id = Guid.NewGuid();
        DailyAdvicePairImportItem[] items = scenario switch {
            "null" => null!,
            "empty" => [],
            "oversized" => new DailyAdvicePairImportItem[501],
            "null-item" => [null!],
            "empty-id" => [new(Guid.Empty, "Russian", "English")],
            "duplicate-id" => [new(id, "Russian", "English"), new(id, "Other", "Another")],
            "duplicate-text" => [new(id, "Russian", "English"), new(Guid.NewGuid(), " Russian ", "Another")],
            _ => [new(id, "Russian", " ")],
        };
        IDailyAdviceWriteRepository repository = Substitute.For<IDailyAdviceWriteRepository>();
        Result<DailyAdviceImportModel> result = await new ImportDailyAdvicePairsCommandHandler(repository)
            .Handle(new ImportDailyAdvicePairsCommand(items), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal(FoodDiary.Results.ErrorKind.Validation, result.Error.Kind);
        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public async Task NewPair_AddsBothTranslationsWithSharedMetadata() {
        IDailyAdviceWriteRepository repository = Substitute.For<IDailyAdviceWriteRepository>();
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<DailyAdvice>());
        using var cancellation = new CancellationTokenSource();
        var group = Guid.NewGuid();
        IReadOnlyList<DailyAdvice>? added = null;
        await repository.AddRangeAsync(Arg.Do<IReadOnlyList<DailyAdvice>>(items => added = items), cancellation.Token);
        repository.ClearReceivedCalls();
        Result<DailyAdviceImportModel> result = await new ImportDailyAdvicePairsCommandHandler(repository)
            .Handle(new ImportDailyAdvicePairsCommand([new(group, " Russian ", " English ", 3, " tag ")]), cancellation.Token);
        ResultAssert.Success(result);
        Assert.Multiple(() => Assert.Equal(1, result.Value.ImportedCount), () => Assert.Equal(0, result.Value.SkippedCount));
        Assert.NotNull(added);
        Assert.Equal(2, added.Count);
        Assert.All(added, item => Assert.Multiple(() => Assert.Equal(group, item.GroupId),
            () => Assert.Equal(3, item.Weight), () => Assert.Equal("tag", item.Tag)));
        Assert.Equal("Russian", Assert.Single(added, item => string.Equals(item.Locale, "ru", StringComparison.Ordinal)).Value);
        Assert.Equal("English", Assert.Single(added, item => string.Equals(item.Locale, "en", StringComparison.Ordinal)).Value);
        await repository.Received(1).AddRangeAsync(Arg.Any<IReadOnlyList<DailyAdvice>>(), cancellation.Token);
    }

    [Fact]
    public async Task LegacyGroupAddressedLater_RejectsWithoutRelinking() {
        var legacy = DailyAdvice.Create("Russian", "ru");
        Guid original = legacy.GroupId;
        IDailyAdviceWriteRepository repository = Substitute.For<IDailyAdviceWriteRepository>();
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { legacy });
        Result<DailyAdviceImportModel> result = await new ImportDailyAdvicePairsCommandHandler(repository).Handle(new ImportDailyAdvicePairsCommand([
            new(Guid.NewGuid(), "Russian", "English"), new(original, "Other", "Another"),
        ]), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("DailyAdvice.ImportConflict", result.Error.Code);
        Assert.Equal(original, legacy.GroupId);
        await repository.DidNotReceiveWithAnyArgs().AddRangeAsync(default!, default);
    }

    [Fact]
    public async Task UpdateMissingGroup_ReturnsNotFoundWithoutAddingTranslations() {
        IDailyAdviceWriteRepository repository = Substitute.For<IDailyAdviceWriteRepository>();
        repository.GetGroupAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<DailyAdvice>());
        Result<DailyAdviceGroupModel> result = await new UpdateDailyAdviceGroupCommandHandler(repository)
            .Handle(new UpdateDailyAdviceGroupCommand(Guid.NewGuid(), "Russian", "English", 1, Tag: null), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal(FoodDiary.Results.ErrorKind.NotFound, result.Error.Kind);
        await repository.DidNotReceiveWithAnyArgs().AddRangeAsync(default!, default);
    }
}

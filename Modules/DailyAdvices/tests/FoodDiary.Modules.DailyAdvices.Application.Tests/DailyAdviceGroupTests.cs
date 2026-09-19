using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Application.Commands.ImportDailyAdvicePairs;
using FoodDiary.Modules.DailyAdvices.Application.Commands.UpdateDailyAdviceGroup;
using FoodDiary.Modules.DailyAdvices.Application.Commands.DeleteDailyAdviceGroup;
using FoodDiary.Modules.DailyAdvices.Contracts.Commands.ImportDailyAdvicePairs;
using FoodDiary.Modules.DailyAdvices.Domain.Entities.Content;
using FoodDiary.Results;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Modules.DailyAdvices.Contracts.Commands.UpdateDailyAdviceGroup;
using FoodDiary.Modules.DailyAdvices.Contracts.Commands.DeleteDailyAdviceGroup;

namespace FoodDiary.Modules.DailyAdvices.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DailyAdviceGroupTests {
    [Fact]
    public async Task ImportLinksLegacyTranslationsAndReimportSkipsPair() {
        var ru = DailyAdvice.Create("Russian", "ru");
        var en = DailyAdvice.Create("English", "en");
        Guid ruId = ru.Id.Value;
        Guid enId = en.Id.Value;
        var group = Guid.NewGuid();
        IDailyAdviceWriteRepository repository = Substitute.For<IDailyAdviceWriteRepository>();
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { ru, en });
        var handler = new ImportDailyAdvicePairsCommandHandler(repository);
        var request = new ImportDailyAdvicePairsCommand([new(group, " Russian ", "English")]);
        Result<DailyAdviceImportModel> result = await handler.Handle(request, CancellationToken.None);
        ResultAssert.Success(result);
        Assert.Multiple(() => Assert.Equal(1, result.Value.ImportedCount),
            () => Assert.Equal(group, ru.GroupId), () => Assert.Equal(group, en.GroupId),
            () => Assert.Equal(ruId, ru.Id.Value), () => Assert.Equal(enId, en.Id.Value));
        Result<DailyAdviceImportModel> retry = await handler.Handle(request, CancellationToken.None);
        ResultAssert.Success(retry);
        Assert.Multiple(() => Assert.Equal(0, retry.Value.ImportedCount), () => Assert.Equal(1, retry.Value.SkippedCount));
        await repository.DidNotReceiveWithAnyArgs().AddRangeAsync(default!, default);
    }

    [Fact]
    public async Task AmbiguousLaterPairDoesNotLinkEarlierLegacyRow() {
        var ru = DailyAdvice.Create("Russian", "ru");
        Guid original = ru.GroupId;
        IDailyAdviceWriteRepository repository = Substitute.For<IDailyAdviceWriteRepository>();
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] {
            ru, DailyAdvice.Create("Duplicate", "en"), DailyAdvice.Create("Duplicate", "en"),
        });
        Result<DailyAdviceImportModel> result = await new ImportDailyAdvicePairsCommandHandler(repository).Handle(new ImportDailyAdvicePairsCommand([
            new(Guid.NewGuid(), "Russian", "English"), new(Guid.NewGuid(), "Other", "Duplicate"),
        ]), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal(original, ru.GroupId);
        await repository.DidNotReceiveWithAnyArgs().AddRangeAsync(default!, default);
    }

    [Fact]
    public async Task ChangedReimportDoesNotOverwriteEditedPair() {
        var group = Guid.NewGuid();
        var ru = DailyAdvice.Create("Edited", "ru");
        var en = DailyAdvice.Create("English", "en");
        ru.AssignGroup(group);
        en.AssignGroup(group);
        IDailyAdviceWriteRepository repository = Substitute.For<IDailyAdviceWriteRepository>();
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { ru, en });
        Result<DailyAdviceImportModel> result = await new ImportDailyAdvicePairsCommandHandler(repository).Handle(new ImportDailyAdvicePairsCommand([new(group, "Original", "English")]), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("Edited", ru.Value);
    }

    [Fact]
    public async Task UpdateChangesBothLanguagesAndSharedMetadata() {
        var group = Guid.NewGuid();
        var ru = DailyAdvice.Create("Old Russian", "ru", tag: "old");
        var en = DailyAdvice.Create("Old English", "en", tag: "old");
        ru.AssignGroup(group);
        en.AssignGroup(group);
        IDailyAdviceWriteRepository repository = Substitute.For<IDailyAdviceWriteRepository>();
        repository.GetGroupAsync(group, Arg.Any<CancellationToken>()).Returns(new[] { ru, en });
        Result<DailyAdviceGroupModel> result = await new UpdateDailyAdviceGroupCommandHandler(repository).Handle(new UpdateDailyAdviceGroupCommand(group, "New Russian", "New English", 3, Tag: null), CancellationToken.None);
        ResultAssert.Success(result);
        Assert.Multiple(() => Assert.Equal("New Russian", ru.Value), () => Assert.Equal("New English", en.Value),
            () => Assert.Equal(3, ru.Weight), () => Assert.Equal(3, en.Weight), () => Assert.Null(ru.Tag), () => Assert.Null(en.Tag));
    }

    [Fact]
    public async Task InvalidSecondTranslationDoesNotChangeFirst() {
        var ru = DailyAdvice.Create("Unchanged", "ru");
        IDailyAdviceWriteRepository repository = Substitute.For<IDailyAdviceWriteRepository>();
        repository.GetGroupAsync(ru.GroupId, Arg.Any<CancellationToken>()).Returns(new[] { ru });
        Result<DailyAdviceGroupModel> result = await new UpdateDailyAdviceGroupCommandHandler(repository).Handle(new UpdateDailyAdviceGroupCommand(ru.GroupId, "Changed", " ", 1, Tag: null), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("Unchanged", ru.Value);
        await repository.DidNotReceiveWithAnyArgs().AddRangeAsync(default!, default);
    }

    [Fact]
    public async Task UpdateAddsMissingLegacyTranslationToSameGroup() {
        var ru = DailyAdvice.Create("Russian", "ru");
        IDailyAdviceWriteRepository repository = Substitute.For<IDailyAdviceWriteRepository>();
        repository.GetGroupAsync(ru.GroupId, Arg.Any<CancellationToken>()).Returns(new[] { ru });
        Result<DailyAdviceGroupModel> result = await new UpdateDailyAdviceGroupCommandHandler(repository).Handle(new UpdateDailyAdviceGroupCommand(ru.GroupId, "Russian", "English", 1, Tag: null), CancellationToken.None);
        ResultAssert.Success(result);
        await repository.Received(1).AddRangeAsync(Arg.Is<IReadOnlyList<DailyAdvice>>(items => items.Count == 1 && items[0].GroupId == ru.GroupId && items[0].Locale == "en"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRemovesEntireGroupAndMissingGroupReturnsNotFound() {
        var group = Guid.NewGuid();
        DailyAdvice[] items = [DailyAdvice.Create("Russian", "ru"), DailyAdvice.Create("English", "en")];
        IDailyAdviceWriteRepository repository = Substitute.For<IDailyAdviceWriteRepository>();
        repository.GetGroupAsync(group, Arg.Any<CancellationToken>()).Returns(items);
        var handler = new DeleteDailyAdviceGroupCommandHandler(repository);
        Assert.True((await handler.Handle(new DeleteDailyAdviceGroupCommand(group), CancellationToken.None)).IsSuccess);
        repository.Received(1).RemoveRange(items);
        var missing = Guid.NewGuid();
        repository.GetGroupAsync(missing, Arg.Any<CancellationToken>()).Returns(Array.Empty<DailyAdvice>());
        Assert.True((await handler.Handle(new DeleteDailyAdviceGroupCommand(missing), CancellationToken.None)).IsFailure);
    }
}

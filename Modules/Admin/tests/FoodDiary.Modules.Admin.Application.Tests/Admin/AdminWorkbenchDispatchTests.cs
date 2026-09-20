using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Application.Commands.ImportAdminDailyAdvicePairs;
using FoodDiary.Modules.Admin.Application.Commands.TestAdminAiPrompt;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Modules.Ai.Contracts.Commands.TestAiPrompt;
using FoodDiary.Modules.DailyAdvices.Contracts.Commands.ImportDailyAdvicePairs;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public sealed class AdminWorkbenchDispatchTests {
    [Fact]
    public async Task TestPrompt_PreservesCallerDraftCancellationAndResult() {
        ISender sender = Substitute.For<ISender>();
        using var cancellation = new CancellationTokenSource();
        var user = Guid.NewGuid();
        var draft = new AdminAiPromptDraft("text-parse", "ru", "Parse {{userText}}", "apple", ImageAssetId: null, FoodName: null, Amount: null, Unit: null);
        sender.Send(Arg.Any<TestAiPromptCommand>(), cancellation.Token).Returns(Result.Success("sample-json"));
        string result = ResultAssert.Success(await new TestAdminAiPromptCommandHandler(sender)
            .Handle(new TestAdminAiPromptCommand(user, "request", draft), cancellation.Token));
        Assert.Equal("sample-json", result);
        await sender.Received(1).Send(Arg.Is<TestAiPromptCommand>(command => command.UserId == user
            && command.RequestId == "request" && command.Draft.Key == draft.Key && command.Draft.Locale == draft.Locale
            && command.Draft.PromptText == draft.PromptText && command.Draft.Text == draft.Text), cancellation.Token);
    }

    [Fact]
    public async Task ImportUnsupportedVersion_RejectsWithoutDispatch() {
        ISender sender = Substitute.For<ISender>();
        ResultAssert.Failure(await new ImportAdminDailyAdvicePairsCommandHandler(sender)
            .Handle(new ImportAdminDailyAdvicePairsCommand(1, []), CancellationToken.None));
        Assert.Empty(sender.ReceivedCalls());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ImportOwnerFailure_PreservesInvalidItemsAndError(bool nullCollection) {
        ISender sender = Substitute.For<ISender>();
        using var cancellation = new CancellationTokenSource();
        var error = new Error("DailyAdvice.ImportConflict", "Conflict", Kind: ErrorKind.Conflict);
        sender.Send(Arg.Any<ImportDailyAdvicePairsCommand>(), cancellation.Token).Returns(Result.Failure<DailyAdviceImportModel>(error));
        Result<AdminDailyAdvicesImportModel> result = await new ImportAdminDailyAdvicePairsCommandHandler(sender)
            .Handle(new ImportAdminDailyAdvicePairsCommand(2, nullCollection ? null! : [null!]), cancellation.Token);
        ResultAssert.Failure(result);
        Assert.Equal(error, result.Error);
        await sender.Received(1).Send(Arg.Is<ImportDailyAdvicePairsCommand>(command => nullCollection
            ? command.Items == null : command.Items.Count == 1 && command.Items[0] == null), cancellation.Token);
    }
}

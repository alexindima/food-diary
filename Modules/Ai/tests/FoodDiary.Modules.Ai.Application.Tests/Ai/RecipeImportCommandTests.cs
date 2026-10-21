using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Application.Commands.ImportRecipe;
using FoodDiary.Modules.Ai.Application.Commands.ImportRecipeVideo;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
public sealed class RecipeImportCommandTests {
    private const string RequestId = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private readonly IOpenAiFoodService _service = Substitute.For<IOpenAiFoodService>();
    private readonly ICurrentUserAccessService _access = Substitute.For<ICurrentUserAccessService>();
    private readonly UserId _user = UserId.New();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handler_ForwardsTheAuthorizedUserInputAndCancellation(bool videoImport) {
        var draft = new RecipeImportDraftModel("Soup", Description: null, [new("Water", "250 ml")], ["Boil"], 1, 0, 5, AuthorNutrition: null, SourceUrl: null);
        using var cancellation = new CancellationTokenSource();
        await using var video = new MemoryStream([1, 2, 3]);
        const string url = "https://example.org/recipe";
        const string text = "Add salt";
        _service.ImportRecipeAsync(url, text, _user, RequestId, cancellation.Token).Returns(Result.Success(draft));
        _service.ImportRecipeVideoAsync(video, url, text, _user, RequestId, cancellation.Token).Returns(Result.Success(draft));

        Result<RecipeImportDraftModel> result = videoImport
            ? await new ImportRecipeVideoCommandHandler(_service, _access).Handle(new ImportRecipeVideoCommand(_user.Value, video, url, text, RequestId), cancellation.Token)
            : await new ImportRecipeCommandHandler(_service, _access).Handle(new ImportRecipeCommand(_user.Value, url, text, RequestId), cancellation.Token);

        Assert.Same(draft, ResultAssert.Success(result));
        await _access.Received(1).EnsureCanAccessAsync(_user, cancellation.Token);
        Assert.Single(_service.ReceivedCalls());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Handler_RejectsMissingOrUnauthorizedUsersBeforeCallingTheProvider(bool videoImport, bool missingUser) {
        var forbidden = new Error("Users.Forbidden", "Access denied.", ErrorKind.Forbidden);
        _access.EnsureCanAccessAsync(_user, Arg.Any<CancellationToken>()).Returns(forbidden);
        Guid? user = missingUser ? null : _user.Value;
        Result<RecipeImportDraftModel> result = videoImport
            ? await new ImportRecipeVideoCommandHandler(_service, _access).Handle(new ImportRecipeVideoCommand(user, Video: null, "https://example.org/video", Text: null, RequestId), CancellationToken.None)
            : await new ImportRecipeCommandHandler(_service, _access).Handle(new ImportRecipeCommand(user, SourceUrl: null, "Recipe text", RequestId), CancellationToken.None);

        ResultAssert.Failure(result);
        if (!missingUser) { Assert.Equal(forbidden, result.Error); }
        Assert.Empty(_service.ReceivedCalls());
        if (missingUser) { Assert.Empty(_access.ReceivedCalls()); }
    }

    [Fact]
    public void Validators_RequireTheAppropriateRecipeSource() {
        var text = new ImportRecipeCommandValidator();
        var video = new ImportRecipeVideoCommandValidator();
        using var stream = new MemoryStream();
        Assert.Multiple(
            () => Assert.True(text.Validate(new ImportRecipeCommand(_user.Value, SourceUrl: null, "Recipe text", RequestId)).IsValid),
            () => Assert.True(text.Validate(new ImportRecipeCommand(_user.Value, "https://example.org/recipe", Text: null, RequestId)).IsValid),
            () => Assert.False(text.Validate(new ImportRecipeCommand(_user.Value, " ", " ", RequestId)).IsValid),
            () => Assert.True(video.Validate(new ImportRecipeVideoCommand(_user.Value, stream, SourceUrl: null, Text: null, RequestId)).IsValid),
            () => Assert.True(video.Validate(new ImportRecipeVideoCommand(_user.Value, Video: null, "https://example.org/video", Text: null, RequestId)).IsValid),
            () => Assert.False(video.Validate(new ImportRecipeVideoCommand(_user.Value, Video: null, SourceUrl: null, "Text alone is not a video", RequestId)).IsValid));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validators_EnforceTextUrlAndRequestIdentityBounds(bool videoImport) {
        int textLimit = videoImport ? 8000 : 16000;
        bool Valid(string url, string text, string requestId) => videoImport
            ? new ImportRecipeVideoCommandValidator().Validate(new ImportRecipeVideoCommand(_user.Value, Video: null, url, text, requestId)).IsValid
            : new ImportRecipeCommandValidator().Validate(new ImportRecipeCommand(_user.Value, url, text, requestId)).IsValid;

        Assert.Multiple(
            () => Assert.True(Valid(new string('u', 1400), new string('t', textLimit), RequestId)),
            () => Assert.False(Valid(new string('u', 1401), "recipe", RequestId)),
            () => Assert.False(Valid("https://example.org", new string('t', textLimit + 1), RequestId)),
            () => Assert.False(Valid("https://example.org", "recipe", new string('A', 63))),
            () => Assert.False(Valid("https://example.org", "recipe", new string('a', 64))),
            () => Assert.False(Valid("https://example.org", "recipe", string.Empty)));
    }
}

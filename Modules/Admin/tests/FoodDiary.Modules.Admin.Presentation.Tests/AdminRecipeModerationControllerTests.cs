using FoodDiary.Modules.Admin.Presentation.Controllers;
using FoodDiary.Modules.Admin.Presentation.Mappings;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Recipes.Contracts.Moderation;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute.ExceptionExtensions;

namespace FoodDiary.Modules.Admin.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminRecipeModerationControllerTests : IDisposable {
    private readonly IRecipeModerationAdministration _administration = Substitute.For<IRecipeModerationAdministration>();
    private readonly CancellationTokenSource _request = new();
    private readonly Guid _revisionId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();

    [Fact]
    public async Task GetRecipes_UsesDefaultPagingWithoutAStatusFilterAsync() {
        var page = new RecipeModerationPage([], 0, 1, 20, 0, 0, 0);
        _administration.ListAsync(status: null, 1, 20, _request.Token).Returns(page);

        IActionResult result = await CreateController().GetRecipes(new GetAdminRecipeModerationHttpQuery());

        Assert.Same(page, Assert.IsType<OkObjectResult>(result).Value);
        await _administration.Received(1).ListAsync(status: null, 1, 20, _request.Token);
    }

    [Fact]
    public async Task GetRecipes_NormalizesTheStatusAndPreservesPagingAsync() {
        var page = new RecipeModerationPage([], 21, 3, 10, 7, 21, 4);
        _administration.ListAsync("NeedsReview", 3, 10, _request.Token).Returns(page);

        IActionResult result = await CreateController().GetRecipes(new GetAdminRecipeModerationHttpQuery("nEEDSrEVIEW", 3, 10));

        Assert.Same(page, Assert.IsType<OkObjectResult>(result).Value);
        await _administration.Received(1).ListAsync("NeedsReview", 3, 10, _request.Token);
    }

    [Fact]
    public async Task GetRecipe_ReturnsTheRequestedRevisionAsync() {
        var item = new RecipeModerationItem(
            _revisionId, Guid.NewGuid(), Guid.NewGuid(), "NeedsReview", "Plum jam", DateTimeOffset.UnixEpoch,
            UpdatedAt: null, Guid.NewGuid(), Reason: null, PrimaryResult: null, ShadowResult: null, Attempts: 1);
        _administration.GetAsync(_revisionId, _request.Token).Returns(item);

        IActionResult result = await CreateController().GetRecipe(_revisionId);

        Assert.Same(item, Assert.IsType<OkObjectResult>(result).Value);
        await _administration.Received(1).GetAsync(_revisionId, _request.Token);
    }

    [Fact]
    public async Task GetRecipe_ReturnsNotFoundWhenRevisionIsMissingAsync() {
        _administration.GetAsync(_revisionId, _request.Token).Returns((RecipeModerationItem?)null);

        IActionResult result = await CreateController().GetRecipe(_revisionId);

        AssertError(result, StatusCodes.Status404NotFound, "RecipeModeration.RevisionNotFound");
    }

    [Fact]
    public async Task GetImage_ReturnsNotFoundWhenPreviewIsUnavailableAsync() {
        var assetId = Guid.NewGuid();
        _administration.GetImageAsync(_revisionId, assetId, _request.Token).Returns((string?)null);

        IActionResult result = await CreateController().GetImage(_revisionId, assetId);

        AssertError(result, StatusCodes.Status404NotFound, "RecipeModeration.ImageNotFound");
        await _administration.Received(1).GetImageAsync(_revisionId, assetId, _request.Token);
    }

    [Fact]
    public async Task ResolveRecipe_ApprovesWithoutAReasonAsync() {
        _administration.ResolveAsync(_revisionId, _actorId, approve: true, reason: null, _request.Token)
            .Returns(RecipeModerationActionOutcome.Applied);

        IActionResult result = await CreateController().ResolveRecipe(
            _revisionId, _actorId, new AdminRecipeModerationResolveHttpRequest(Approve: true, Reason: null));

        Assert.IsType<NoContentResult>(result);
        await _administration.Received(1).ResolveAsync(_revisionId, _actorId, approve: true, reason: null, _request.Token);
    }

    [Fact]
    public async Task ResolveRecipe_TrimsTheReasonWhenRequestingChangesAsync() {
        const string Reason = "Replace the cover photo.";
        _administration.ResolveAsync(_revisionId, _actorId, approve: false, Reason, _request.Token)
            .Returns(RecipeModerationActionOutcome.Applied);

        IActionResult result = await CreateController().ResolveRecipe(
            _revisionId, _actorId, new AdminRecipeModerationResolveHttpRequest(Approve: false, $"  {Reason}  "));

        Assert.IsType<NoContentResult>(result);
        await _administration.Received(1).ResolveAsync(_revisionId, _actorId, approve: false, Reason, _request.Token);
    }

    [Fact]
    public async Task ResolveRecipe_RejectsAWhitespaceReasonBeforeDispatchingAsync() {
        IActionResult result = await CreateController().ResolveRecipe(
            _revisionId, _actorId, new AdminRecipeModerationResolveHttpRequest(Approve: false, "  "));

        AssertError(result, StatusCodes.Status400BadRequest, "RecipeModeration.ReasonRequired");
        Assert.Empty(_administration.ReceivedCalls());
    }

    [Theory]
    [InlineData(RecipeModerationActionOutcome.NotFound, StatusCodes.Status404NotFound, "RecipeModeration.RevisionNotFound")]
    [InlineData(RecipeModerationActionOutcome.Conflict, StatusCodes.Status409Conflict, "RecipeModeration.Conflict")]
    public async Task RetryRecipe_TranslatesOwnerFailuresAsync(RecipeModerationActionOutcome outcome, int statusCode, string code) {
        _administration.RetryAsync(_revisionId, _actorId, _request.Token).Returns(outcome);

        IActionResult result = await CreateController().RetryRecipe(_revisionId, _actorId);

        AssertError(result, statusCode, code);
        await _administration.Received(1).RetryAsync(_revisionId, _actorId, _request.Token);
    }

    [Fact]
    public async Task RetryRecipe_RejectsAnUnknownOutcomeInsteadOfReportingSuccessAsync() {
        _administration.RetryAsync(_revisionId, _actorId, _request.Token).Returns((RecipeModerationActionOutcome)100);

        ArgumentOutOfRangeException exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => CreateController().RetryRecipe(_revisionId, _actorId));

        Assert.Equal("outcome", exception.ParamName);
    }

    [Fact]
    public async Task GetRules_ReturnsTheActivePolicyAndHistoryAsync() {
        RecipeModerationPolicyView policy = CreatePolicy(new RecipeModerationPolicyInput(["violence"], ["violence"], 20));
        _administration.GetPolicyAsync(_request.Token).Returns(policy);

        IActionResult result = await CreateController().GetRules();

        Assert.Same(policy, Assert.IsType<OkObjectResult>(result).Value);
        await _administration.Received(1).GetPolicyAsync(_request.Token);
    }

    [Fact]
    public async Task PublishRules_PassesAllRulesAndTheActorToTheOwnerAsync() {
        string[] textCategories = ["violence"];
        string[] imageCategories = ["sexual"];
        var rules = new RecipeModerationPolicyInput(textCategories, imageCategories, 45);
        RecipeModerationPolicyView policy = CreatePolicy(rules);
        _administration.PublishPolicyAsync(_actorId, rules, _request.Token).Returns(policy);

        IActionResult result = await CreateController().PublishRules(
            _actorId, new AdminRecipeModerationRulesHttpRequest(textCategories, imageCategories, 45));

        Assert.Same(policy, Assert.IsType<OkObjectResult>(result).Value);
        await _administration.Received(1).PublishPolicyAsync(_actorId, rules, _request.Token);
    }

    [Fact]
    public async Task PublishRules_RejectsAnIncompletePolicyBeforeDispatchingAsync() {
        IActionResult result = await CreateController().PublishRules(
            _actorId, new AdminRecipeModerationRulesHttpRequest(TextReviewCategories: null, ["violence"], 20));

        AssertError(result, StatusCodes.Status400BadRequest, "RecipeModeration.InvalidRules");
        Assert.Empty(_administration.ReceivedCalls());
    }

    [Fact]
    public async Task PublishRules_TranslatesPolicyValidationFailureAsync() {
        _administration.PublishPolicyAsync(_actorId, Arg.Any<RecipeModerationPolicyInput>(), _request.Token)
            .ThrowsAsync<ArgumentException>();

        IActionResult result = await CreateController().PublishRules(
            _actorId, new AdminRecipeModerationRulesHttpRequest(["unknown-category"], ["violence"], 20));

        AssertError(result, StatusCodes.Status400BadRequest, "RecipeModeration.InvalidRules");
    }

    [Fact]
    public async Task RestoreRules_ReturnsTheRestoredPolicyAsync() {
        var previousVersionId = Guid.NewGuid();
        RecipeModerationPolicyView policy = CreatePolicy(new RecipeModerationPolicyInput(["violence"], ["violence"], 20));
        _administration.RestorePolicyAsync(previousVersionId, _actorId, _request.Token).Returns(policy);

        IActionResult result = await CreateController().RestoreRules(previousVersionId, _actorId);

        Assert.Same(policy, Assert.IsType<OkObjectResult>(result).Value);
        await _administration.Received(1).RestorePolicyAsync(previousVersionId, _actorId, _request.Token);
    }

    [Fact]
    public async Task RestoreRules_ReturnsNotFoundWhenVersionIsMissingAsync() {
        var previousVersionId = Guid.NewGuid();
        _administration.RestorePolicyAsync(previousVersionId, _actorId, _request.Token).Returns((RecipeModerationPolicyView?)null);

        IActionResult result = await CreateController().RestoreRules(previousVersionId, _actorId);

        AssertError(result, StatusCodes.Status404NotFound, "RecipeModeration.PolicyNotFound");
    }

    public void Dispose() => _request.Dispose();

    private AdminRecipeModerationController CreateController() =>
        new(new AdminRecipeModerationHttpProcessor(_administration)) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext { RequestAborted = _request.Token, TraceIdentifier = "moderation-request" },
            },
        };

    private RecipeModerationPolicyView CreatePolicy(RecipeModerationPolicyInput rules) {
        var active = new RecipeModerationPolicyVersion(Guid.NewGuid(), DateTimeOffset.UnixEpoch, _actorId, rules);
        return new RecipeModerationPolicyView(active, [active], MistralAvailable: true);
    }

    private static void AssertError(IActionResult result, int statusCode, string code) {
        ObjectResult response = Assert.IsType<ObjectResult>(result);
        ApiErrorHttpResponse error = Assert.IsType<ApiErrorHttpResponse>(response.Value);
        Assert.Multiple(
            () => Assert.Equal(statusCode, response.StatusCode),
            () => Assert.Equal(code, error.Error),
            () => Assert.Equal("moderation-request", error.TraceId));
    }
}

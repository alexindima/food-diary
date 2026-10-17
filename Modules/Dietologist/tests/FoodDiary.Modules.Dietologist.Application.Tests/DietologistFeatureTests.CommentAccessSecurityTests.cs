using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Dietologist.Application.Queries.GetRecommendationComments;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Application.Contracts.Common.Models;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Dietologist.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Tests;

public partial class DietologistFeatureTests {
    [Theory]
    [InlineData("accepted", false)]
    [InlineData("pending", false)]
    [InlineData("revoked", false)]
    [InlineData("declined", false)]
    [InlineData("missing", false)]
    [InlineData("other-provider", false)]
    [InlineData("accepted", true)]
    [InlineData("pending", true)]
    [InlineData("revoked", true)]
    [InlineData("declined", true)]
    [InlineData("missing", true)]
    [InlineData("other-provider", true)]
    public async Task GetRecommendationComments_RequiresCurrentProviderRelationshipButPreservesClientHistory(
        string state, bool asClient) {
        var client = User.Create("discussion-client@example.com", "hash");
        var dietologist = User.Create("discussion-provider@example.com", "hash");
        var recommendation = Recommendation.Create(dietologist.Id, client.Id, "Recommendation");
        var invitations = new InMemoryInvitationRepository();
        var invitation = DietologistInvitation.Create(client.Id, dietologist.Email!, "token-hash",
            DateTime.UtcNow.AddDays(1), DietologistPermissions.AllEnabled);
        if (state is "accepted" or "revoked" or "other-provider") {
            invitation.Accept(state is "other-provider" ? UserId.New() : dietologist.Id);
        }
        if (state is "revoked") {
            invitation.Revoke();
        } else if (state is "declined") {
            invitation.Decline();
        }
        if (state is not "missing") {
            invitations.Seed(invitation);
        }
        var users = new InMemoryUserRepository();
        users.Seed(client);
        users.Seed(dietologist);
        IRecommendationCommentReadModelRepository comments = Substitute.For<IRecommendationCommentReadModelRepository>();
        comments.GetParticipantsAsync(recommendation.Id, Arg.Any<CancellationToken>())
            .Returns(new RecommendationParticipantsReadModel(recommendation.ClientUserId, recommendation.DietologistUserId));
        comments.GetPageByRecommendationAsync(recommendation.Id, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new List<RecommendationCommentReadModel> {
                new(Guid.NewGuid(), recommendation.Id.Value, client.Id.Value, "Current client", null,
                    client.Email, "New private comment after relationship change", DateTime.UtcNow),
            }, 1));
        var handler = new GetRecommendationCommentsQueryHandler(comments, invitations, users);

        Result<PagedResponse<RecommendationCommentModel>> result = await handler.Handle(new GetRecommendationCommentsQuery(
            asClient ? client.Id.Value : dietologist.Id.Value, recommendation.Id.Value), CancellationToken.None);

        if (asClient || state is "accepted") {
            Assert.Equal("New private comment after relationship change", Assert.Single(ResultAssert.Success(result).Data).Text);
        } else {
            ResultAssert.Failure(result, "Dietologist.InvitationNotFound");
            await comments.DidNotReceiveWithAnyArgs().GetPageByRecommendationAsync(default, default, default, default);
        }
    }
}

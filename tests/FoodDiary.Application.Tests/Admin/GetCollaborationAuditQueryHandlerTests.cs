using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Audit.Models;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Queries.GetCollaborationAudit;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public sealed class GetCollaborationAuditQueryHandlerTests {
    [Theory]
    [InlineData(0, 1)]
    [InlineData(25, 25)]
    [InlineData(1000, 500)]
    public async Task Handle_ClampsLimitAndMapsAllFields(int requestedLimit, int expectedLimit) {
        IAuditEntryReadService readService = Substitute.For<IAuditEntryReadService>();
        var clientUserId = Guid.NewGuid();
        var entry = new AuditEntryReadModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            clientUserId,
            "action",
            "Target",
            "target-id",
            "{}",
            new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc));
        readService.GetRecentAsync(clientUserId, expectedLimit, Arg.Any<CancellationToken>())
            .Returns([entry]);
        var handler = new GetCollaborationAuditQueryHandler(readService);

        Result<IReadOnlyList<AdminAuditEntryModel>> result = await handler.Handle(
            new GetCollaborationAuditQuery(clientUserId, requestedLimit),
            CancellationToken.None);

        AdminAuditEntryModel model = Assert.Single(ResultAssert.Success(result));
        Assert.Multiple(
            () => Assert.Equal(entry.Id, model.Id),
            () => Assert.Equal(entry.ActorUserId, model.ActorUserId),
            () => Assert.Equal(entry.SubjectClientUserId, model.SubjectClientUserId),
            () => Assert.Equal(entry.Action, model.Action),
            () => Assert.Equal(entry.TargetType, model.TargetType),
            () => Assert.Equal(entry.TargetId, model.TargetId),
            () => Assert.Equal(entry.Metadata, model.Metadata),
            () => Assert.Equal(entry.CreatedAtUtc, model.CreatedAtUtc));
    }
}

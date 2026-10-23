using FoodDiary.Modules.Fasting.Application.Commands.EndFasting;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Fasting.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Fasting.Application.Tests;

public partial class FastingFeatureTests {
    [Theory]
    [InlineData(FastingProtocol.Fast24, 24, -1, "Interrupted")]
    [InlineData(FastingProtocol.Fast24, 24, 0, "Completed")]
    [InlineData(FastingProtocol.Fast24, 24, 1, "Completed")]
    [InlineData(FastingProtocol.Fast36, 36, -1, "Interrupted")]
    [InlineData(FastingProtocol.Fast36, 36, 0, "Completed")]
    [InlineData(FastingProtocol.Fast36, 36, 1, "Completed")]
    public async Task EndExtendedFasting_AroundExactTarget_UsesElapsedDurationAsync(
        FastingProtocol protocol, int targetHours, int tickOffset, string expectedStatus) {
        var userId = UserId.New();
        var plan = FastingPlan.CreateExtended(userId, protocol, targetHours, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, targetHours);
        DateTime endedAt = FixedNow.AddHours(targetHours).AddTicks(tickOffset);
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider(endedAt));

        Result<FastingSessionModel> result = await handler.Handle(new EndFastingCommand(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(expectedStatus, result.Value.Status),
            () => Assert.Equal(expectedStatus, occurrence.Status.ToString()),
            () => Assert.Equal(endedAt, result.Value.EndedAtUtc),
            () => Assert.Equal(FastingPlanStatus.Stopped, plan.Status));
    }
}

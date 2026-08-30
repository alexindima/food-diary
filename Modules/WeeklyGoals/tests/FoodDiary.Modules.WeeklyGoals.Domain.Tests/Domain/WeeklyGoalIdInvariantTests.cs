using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class WeeklyGoalIdInvariantTests {
    [Fact]
    public void ImplementsEntityIdContract() {
        Assert.IsAssignableFrom<IEntityId<Guid>>(WeeklyGoalId.New());
    }

    [Fact]
    public void Empty_ReturnsEmptyGuid() {
        Assert.Equal(Guid.Empty, WeeklyGoalId.Empty.Value);
    }

    [Fact]
    public void New_ReturnsNonEmptyGuid() {
        Assert.NotEqual(Guid.Empty, WeeklyGoalId.New().Value);
    }

    [Fact]
    public void Conversions_RoundTripGuid() {
        var guid = Guid.NewGuid();

        var id = (WeeklyGoalId)guid;

        Assert.Equal<Guid>(guid, id);
    }

    [Fact]
    public void ToString_IncludesGuid() {
        var guid = Guid.NewGuid();
        var id = (WeeklyGoalId)guid;

        Assert.Contains(guid.ToString(), id.ToString(), StringComparison.Ordinal);
    }
}

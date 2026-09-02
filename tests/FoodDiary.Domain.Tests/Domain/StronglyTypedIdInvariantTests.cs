using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class StronglyTypedIdInvariantTests {
    [Fact]
    public void ClientTaskId_Empty_ReturnsEmptyGuid() {
        Assert.Equal(Guid.Empty, ClientTaskId.Empty.Value);
    }
}

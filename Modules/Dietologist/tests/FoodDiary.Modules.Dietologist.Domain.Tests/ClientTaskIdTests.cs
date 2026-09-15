using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ClientTaskIdTests {
    [Fact]
    public void ClientTaskId_Empty_ReturnsEmptyGuid() {
        Assert.Equal(Guid.Empty, ClientTaskId.Empty.Value);
    }
}

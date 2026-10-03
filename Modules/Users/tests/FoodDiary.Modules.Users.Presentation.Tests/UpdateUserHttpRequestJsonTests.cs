using System.Text.Json;
using FoodDiary.Modules.Users.Application.Commands.UpdateUser;
using FoodDiary.Modules.Users.Presentation.Mappings;
using FoodDiary.Modules.Users.Presentation.Requests;

namespace FoodDiary.Modules.Users.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class UpdateUserHttpRequestJsonTests {
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("{}", false)]
    [InlineData("{\"language\":\"ru\"}", false)]
    [InlineData("{\"birthDate\":null}", true)]
    [InlineData("{\"birthDateSpecified\":true}", false)]
    public void Deserialize_DistinguishesClearFromOmittedDate(string json, bool specified) {
        UpdateUserHttpRequest request = Assert.IsType<UpdateUserHttpRequest>(JsonSerializer.Deserialize<UpdateUserHttpRequest>(json, Options));
        UpdateUserCommand command = request.ToCommand(Guid.NewGuid());

        Assert.Multiple(
            () => Assert.Null(request.BirthDate),
            () => Assert.Equal(specified, request.BirthDateSpecified),
            () => Assert.Null(command.BirthDate),
            () => Assert.Equal(specified, command.BirthDateSpecified));
    }

    [Fact]
    public void Deserialize_SelectedDateRetainsValueAndPresence() {
        UpdateUserHttpRequest request = Assert.IsType<UpdateUserHttpRequest>(JsonSerializer.Deserialize<UpdateUserHttpRequest>(
            "{\"birthDate\":\"2000-10-02T00:00:00Z\"}", Options));
        UpdateUserCommand command = request.ToCommand(Guid.NewGuid());

        Assert.Multiple(
            () => Assert.Equal(new DateTime(2000, 10, 2, 0, 0, 0, DateTimeKind.Utc), command.BirthDate),
            () => Assert.True(command.BirthDateSpecified));
    }

    [Fact]
    public void Serialize_DoesNotExposePresenceMetadata() {
        var request = new UpdateUserHttpRequest { BirthDate = null };
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(request, Options));

        Assert.Multiple(
            () => Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("birthDate").ValueKind),
            () => Assert.False(json.RootElement.TryGetProperty("birthDateSpecified", out _)));
    }
}

using FoodDiary.Presentation.Api.Features.Auth.Mappings;
using FoodDiary.Presentation.Api.Features.Auth.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class AuthHttpMappingsTests {
    [Fact]
    public void RegisterRequest_ToCommand_MapsAllFields() {
        var request = new RegisterHttpRequest(
            Email: "alex@example.com",
            Password: "P@ssw0rd!",
            Language: "ru");

        var command = request.ToCommand();

        Assert.Equal(request.Email, command.Email);
        Assert.Equal(request.Password, command.Password);
        Assert.Equal(request.Language, command.Language);
    }

    [Fact]
    public void TelegramAuthRequest_ToLinkCommand_MapsUserIdAndInitData() {
        var userId = Guid.NewGuid();
        var request = new TelegramAuthHttpRequest(InitData: "query_id=123&hash=abc");

        var command = request.ToLinkCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(request.InitData, command.InitData);
    }

    [Fact]
    public void TelegramLoginWidgetRequest_ToCommand_MapsAllFields() {
        var request = new TelegramLoginWidgetHttpRequest(
            Id: 42,
            AuthDate: 1_717_171_717,
            Hash: "hash-value",
            Username: "alex",
            FirstName: "Alex",
            LastName: "Doe",
            PhotoUrl: "https://cdn.example/avatar.png");

        var command = request.ToCommand();

        Assert.Equal(request.Id, command.Id);
        Assert.Equal(request.AuthDate, command.AuthDate);
        Assert.Equal(request.Hash, command.Hash);
        Assert.Equal(request.Username, command.Username);
        Assert.Equal(request.FirstName, command.FirstName);
        Assert.Equal(request.LastName, command.LastName);
        Assert.Equal(request.PhotoUrl, command.PhotoUrl);
    }

    [Fact]
    public void ConfirmPasswordResetRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var request = new ConfirmPasswordResetHttpRequest(
            UserId: userId,
            Token: "reset-token",
            NewPassword: "N3wP@ssword");

        var command = request.ToCommand();

        Assert.Equal(request.UserId, command.UserId);
        Assert.Equal(request.Token, command.Token);
        Assert.Equal(request.NewPassword, command.NewPassword);
    }
}

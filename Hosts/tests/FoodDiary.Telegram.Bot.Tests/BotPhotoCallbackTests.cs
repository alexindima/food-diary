using FoodDiary.Telegram.Bot.Operations;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotPhotoCallbackTests {
    [Fact]
    public void CopiedCallbackKeepsItsNewOperationAndTrialTransitionTogether() {
        var original = new BotPhotoCallback(new BotOperationId(Guid.NewGuid()), StartsTrial: false);
        var next = new BotOperationId(Guid.NewGuid());

        BotPhotoCallback trial = original with { OperationId = next, StartsTrial = true };

        Assert.Equal(next, trial.OperationId);
        Assert.Equal(BotPhotoTrial.OfferedCheckpoint, trial.ExpectedCheckpoint);
        Assert.Equal(BotPhotoTrial.AcceptedCheckpoint, trial.NextCheckpoint);
        Assert.False(original.StartsTrial);
        Assert.NotEqual(next, original.OperationId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong:11111111111111111111111111111111")]
    [InlineData("photo-trial:invalid")]
    [InlineData("photo-continue:00000000000000000000000000000000")]
    public void Read_RefusesMalformedOrUnboundCallbacks(string? data) =>
        Assert.False(BotPhotoCallback.TryRead(data, out _));

    [Theory]
    [InlineData("photo-trial:", true)]
    [InlineData("photo-continue:", false)]
    public void Read_PreservesTheOperationAndTheIntendedStateTransition(string prefix, bool startsTrial) {
        var operation = Guid.Parse("11111111-2222-3333-4444-555555555555");

        Assert.True(BotPhotoCallback.TryRead($"{prefix}{operation:N}", out BotPhotoCallback callback));
        Assert.Multiple(
            () => Assert.Equal(operation, callback.OperationId.Value),
            () => Assert.Equal(startsTrial, callback.StartsTrial),
            () => Assert.Equal(startsTrial ? BotPhotoTrial.OfferedCheckpoint : BotPhotoCallback.ConsentCheckpoint, callback.ExpectedCheckpoint),
            () => Assert.Equal(startsTrial ? BotPhotoTrial.AcceptedCheckpoint : """{"Stage":"received"}""", callback.NextCheckpoint));
    }
}

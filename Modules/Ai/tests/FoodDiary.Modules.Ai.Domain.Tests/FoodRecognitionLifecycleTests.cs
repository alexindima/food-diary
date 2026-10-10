using FoodDiary.Modules.Ai.Domain.Recognition;

namespace FoodDiary.Modules.Ai.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class FoodRecognitionLifecycleTests {
    [Theory]
    [InlineData(FoodRecognitionStatus.Queued, FoodRecognitionAction.Claim, FoodRecognitionStatus.Running)]
    [InlineData(FoodRecognitionStatus.Queued, FoodRecognitionAction.Interrupt, FoodRecognitionStatus.Failed)]
    [InlineData(FoodRecognitionStatus.Running, FoodRecognitionAction.CheckpointVision, FoodRecognitionStatus.Running)]
    [InlineData(FoodRecognitionStatus.Running, FoodRecognitionAction.Complete, FoodRecognitionStatus.Succeeded)]
    [InlineData(FoodRecognitionStatus.Running, FoodRecognitionAction.Fail, FoodRecognitionStatus.Failed)]
    [InlineData(FoodRecognitionStatus.Running, FoodRecognitionAction.Interrupt, FoodRecognitionStatus.Failed)]
    public void AllowedTransitions_MatchTheRecognitionProtocol(FoodRecognitionStatus current, FoodRecognitionAction action, FoodRecognitionStatus expected) =>
        Assert.Equal(expected, FoodRecognitionLifecycle.Next(current, action));

    [Theory]
    [InlineData(FoodRecognitionStatus.Succeeded)]
    [InlineData(FoodRecognitionStatus.Failed)]
    [InlineData(FoodRecognitionStatus.Unknown)]
    public void TerminalAndUnknownStates_RejectEveryWorkerAction(FoodRecognitionStatus status) {
        foreach (FoodRecognitionAction action in Enum.GetValues<FoodRecognitionAction>()) {
            Assert.Null(FoodRecognitionLifecycle.Next(status, action));
        }
        Assert.False(FoodRecognitionLifecycle.IsPending(status));
    }

    [Theory]
    [InlineData(FoodRecognitionStatus.Queued, FoodRecognitionAction.Complete)]
    [InlineData(FoodRecognitionStatus.Queued, FoodRecognitionAction.CheckpointVision)]
    [InlineData(FoodRecognitionStatus.Queued, FoodRecognitionAction.Fail)]
    [InlineData(FoodRecognitionStatus.Running, FoodRecognitionAction.Claim)]
    public void UnclaimedCompletionAndRunningReplay_AreForbidden(FoodRecognitionStatus current, FoodRecognitionAction action) =>
        Assert.Null(FoodRecognitionLifecycle.Next(current, action));

    [Theory]
    [InlineData("Queued", FoodRecognitionStatus.Queued)]
    [InlineData("Running", FoodRecognitionStatus.Running)]
    [InlineData("Succeeded", FoodRecognitionStatus.Succeeded)]
    [InlineData("Failed", FoodRecognitionStatus.Failed)]
    public void KnownStorageValues_RetainTheirExactEncoding(string stored, FoodRecognitionStatus expected) {
        Assert.Equal(expected, FoodRecognitionLifecycle.Decode(stored));
        Assert.Equal(stored, FoodRecognitionLifecycle.Encode(expected));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("succeeded")]
    [InlineData("FutureProviderStatus")]
    public void UnrecognizedStorageValues_CannotBecomeSuccessful(string? stored) =>
        Assert.Equal(FoodRecognitionStatus.Unknown, FoodRecognitionLifecycle.Decode(stored));
}

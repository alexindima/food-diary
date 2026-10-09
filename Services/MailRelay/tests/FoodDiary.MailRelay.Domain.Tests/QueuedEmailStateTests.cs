using FoodDiary.MailRelay.Domain.Emails;

namespace FoodDiary.MailRelay.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class QueuedEmailStateTests {
    [Theory]
    [InlineData("pending", QueuedEmailProcessingState.Pending)]
    [InlineData("retry", QueuedEmailProcessingState.Retry)]
    [InlineData("processing", QueuedEmailProcessingState.Processing)]
    [InlineData("sent", QueuedEmailProcessingState.Sent)]
    [InlineData("failed", QueuedEmailProcessingState.Failed)]
    [InlineData("suppressed", QueuedEmailProcessingState.Suppressed)]
    public void Codec_PreservesExactStorageCodes(string code, QueuedEmailProcessingState state) {
        Assert.Equal(state, QueuedEmailStateCodec.FromStorage(code));
        Assert.Equal(code, QueuedEmailStateCodec.ToStorage(state));
    }

    [Fact]
    public void UnknownState_IsPreservedAsUnrecognizedAndCannotBecomeAWrite() {
        Assert.Equal(QueuedEmailProcessingState.Unrecognized, QueuedEmailStateCodec.FromStorage("FutureState"));
        Assert.Throws<ArgumentOutOfRangeException>(() => QueuedEmailStateCodec.ToStorage(QueuedEmailProcessingState.Unrecognized));
    }

    [Fact]
    public void FailureDecision_DerivesTerminalFlagFromState() {
        var id = (QueuedEmailId)Guid.NewGuid();
        Assert.False(new QueuedEmailFailureDecision(id, 1, QueuedEmailProcessingState.Retry, "retry").IsTerminalFailure);
        Assert.True(new QueuedEmailFailureDecision(id, 3, QueuedEmailProcessingState.Failed, "failed").IsTerminalFailure);
        Assert.Throws<ArgumentOutOfRangeException>(() => new QueuedEmailFailureDecision(id, 1, QueuedEmailProcessingState.Sent, "invalid"));
    }
}

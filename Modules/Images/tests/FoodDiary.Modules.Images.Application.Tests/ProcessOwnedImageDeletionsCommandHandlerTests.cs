using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Application.Commands.ProcessOwnedImageDeletions;
using FoodDiary.Modules.Images.Service.Contracts.Commands.ProcessOwnedImageDeletions;

namespace FoodDiary.Modules.Images.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ProcessOwnedImageDeletionsCommandHandlerTests {
    private readonly IImageObjectDeletionOutboxProcessor _processor = Substitute.For<IImageObjectDeletionOutboxProcessor>();

    [Fact]
    public async Task Handle_ProcessesOnlyTheValidatedKeysAndPreservesCancellationAndResult() {
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();
        string[] keys = [$"users/{firstUser:D}/images/first.jpg", $"users/{secondUser:D}/images/second.jpg"];
        using var cancellation = new CancellationTokenSource();
        _processor.ProcessKeysAsync(keys, cancellation.Token).Returns(2);

        int result = await new ProcessOwnedImageDeletionsCommandHandler(_processor).Handle(
            new ProcessOwnedImageDeletionsCommand([firstUser, secondUser], keys), cancellation.Token);

        Assert.Equal(2, result);
        await _processor.Received(1).ProcessKeysAsync(keys, cancellation.Token);
        Assert.Single(_processor.ReceivedCalls());
    }

    [Theory]
    [InlineData(0, false, 0)]
    [InlineData(21, false, 0)]
    [InlineData(1, true, 0)]
    [InlineData(1, false, 1001)]
    public async Task Handle_RejectsUnboundedOrEmptyUserScopesBeforeProcessing(int userCount, bool emptyId, int keyCount) {
        Guid[] users = [.. Enumerable.Range(0, userCount).Select(_ => emptyId ? Guid.Empty : Guid.NewGuid())];
        string[] keys = [.. Enumerable.Repeat("unused", keyCount)];

        await Assert.ThrowsAsync<ArgumentException>(() => new ProcessOwnedImageDeletionsCommandHandler(_processor).Handle(
            new ProcessOwnedImageDeletionsCommand(users, keys), CancellationToken.None));
        Assert.Empty(_processor.ReceivedCalls());
    }

    [Theory]
    [InlineData("../outside.jpg")]
    [InlineData("./image.jpg")]
    [InlineData("folder/../../outside.jpg")]
    [InlineData("folder\\image.jpg")]
    public async Task Handle_RejectsTraversalEvenWhenTheKeyStartsWithTheOwnedPrefix(string suffix) {
        var user = Guid.NewGuid();
        string key = $"users/{user:D}/images/{suffix}";

        await Assert.ThrowsAsync<InvalidOperationException>(() => new ProcessOwnedImageDeletionsCommandHandler(_processor).Handle(
            new ProcessOwnedImageDeletionsCommand([user], [key]), CancellationToken.None));
        Assert.Empty(_processor.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_ValidatesTheEntireBatchBeforeDeletingAnyOwnedObject() {
        var user = Guid.NewGuid();
        string[] keys = [$"users/{user:D}/images/owned.jpg", $"users/{Guid.NewGuid():D}/images/foreign.jpg"];

        await Assert.ThrowsAsync<InvalidOperationException>(() => new ProcessOwnedImageDeletionsCommandHandler(_processor).Handle(
            new ProcessOwnedImageDeletionsCommand([user], keys), CancellationToken.None));
        Assert.Empty(_processor.ReceivedCalls());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Handle_RejectsBlankKeysBeforeProcessing(string key) {
        await Assert.ThrowsAsync<ArgumentException>(() => new ProcessOwnedImageDeletionsCommandHandler(_processor).Handle(
            new ProcessOwnedImageDeletionsCommand([Guid.NewGuid()], [key]), CancellationToken.None));
        Assert.Empty(_processor.ReceivedCalls());
    }
}

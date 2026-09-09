using FoodDiary.Application.Abstractions.Admin.Common;

namespace FoodDiary.Application.Tests.Admin;

public partial class AdminFeatureTests {
    [Fact]
    public async Task DefaultMailReader_RejectsUnsupportedPaging() {
        IAdminMailInboxReader reader = new RecordingAdminMailInboxReader();
        await Assert.ThrowsAsync<NotSupportedException>(() => reader.GetMessagePageAsync(1, 10, recipient: null, category: null, unread: null, CancellationToken.None));
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("recipient", null, null)]
    [InlineData(null, "category", null)]
    [InlineData(null, null, false)]
    public async Task DefaultMailReader_ForwardsOnlyUnfilteredQueries(string? recipient, string? category, bool? unread) {
        var reader = new RecordingAdminMailInboxReader();
        IAdminMailInboxReader contract = reader;
        if (recipient is not null || category is not null || unread is not null) {
            await Assert.ThrowsAsync<NotSupportedException>(() => contract.GetFilteredMessagesAsync(17, recipient, category, unread, CancellationToken.None));
            Assert.Equal(0, reader.LastLimit);
            return;
        }

        Assert.Same(reader.Messages, await contract.GetFilteredMessagesAsync(17, recipient: null, category: null, unread: null, CancellationToken.None));
        Assert.Equal(17, reader.LastLimit);
    }
}

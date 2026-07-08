using FoodDiary.MailRelay.Infrastructure.Services;

namespace FoodDiary.MailRelay.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class DnsClientMxResolverTests {
    [Fact]
    public async Task ResolveAsync_WhenDomainHasNoMxRecords_FallsBackToDomainItself() {
        var resolver = new DnsClientMxResolver();

        IReadOnlyList<MxRecord> records = await resolver.ResolveAsync("localhost", CancellationToken.None);

        MxRecord record = Assert.Single(records);
        Assert.Equal("localhost", record.Host);
        Assert.Equal(0, record.Preference);
    }

    [Fact]
    public async Task ResolveAsync_WhenDomainHasMxRecords_ReturnsHostsOrderedByPreference() {
        var resolver = new DnsClientMxResolver();

        IReadOnlyList<MxRecord> records = await resolver.ResolveAsync("gmail.com", CancellationToken.None);

        Assert.NotEmpty(records);
        Assert.DoesNotContain(records, static record => record.Host[^1] == '.');
        Assert.Equal(records.OrderBy(static record => record.Preference).Select(static record => record.Preference), records.Select(static record => record.Preference));
    }
}

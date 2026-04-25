using DnsClient;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class DnsClientMxResolver : IMxResolver {
    private readonly LookupClient _lookupClient = new();

    public async Task<IReadOnlyList<MxRecord>> ResolveAsync(string domain, CancellationToken cancellationToken) {
        var result = await _lookupClient.QueryAsync(domain, QueryType.MX, cancellationToken: cancellationToken);
        var records = result.Answers
            .MxRecords()
            .Select(static record => new MxRecord(record.Exchange.Value.TrimEnd('.'), record.Preference))
            .OrderBy(static record => record.Preference)
            .ToArray();

        return records.Length > 0 ? records : [new MxRecord(domain, 0)];
    }
}

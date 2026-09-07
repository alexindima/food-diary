using System.Net;
using System.Net.Http.Json;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.Infrastructure.Services;
using FoodDiary.MailInbox.IntegrationTests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace FoodDiary.MailInbox.IntegrationTests;

[Collection("mailinbox-postgres")]
public sealed class MailInboxExportIntegrationTests(MailInboxPostgresFixture fixture) {
    [RequiresDockerFact]
    public async Task Export_IsRecipientFiltered_PaginatesTies_AndPreservesBinaryContent() {
        string connectionString = await fixture.CreateIsolatedDatabaseAsync();
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        using var store = new NpgsqlInboundMailStore(dataSource, new DmarcReportParser(),
            Microsoft.Extensions.Options.Options.Create(new MailInboxStorageOptions()), TimeProvider.System);
        await store.EnsureSchemaAsync(CancellationToken.None);
        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        byte[] mime = [0, 255, 128, 13, 10, 10, 42];
        for (int i = 0; i < 3; i++) {
            await store.SaveAsync(InboundMailMessage.Receive(i.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "reporter@example.com", ["bugs@fooddiary.club"], "Bug", "Steps", htmlBody: null, [.. mime, (byte)i], now), CancellationToken.None);
        }
        InboundMailSaveResult other = await store.SaveAsync(InboundMailMessage.Receive("other", "sender@example.com",
            ["admin@fooddiary.club"], "Private admin mail", "Private", htmlBody: null, mime, now), CancellationToken.None);
        var exporter = new NpgsqlInboundMailExportStore(dataSource);
        IReadOnlyList<InboundMailExportEntry> first = await exporter.GetExportPageAsync("BUGS@fooddiary.club", beforeReceivedAtUtc: null, beforeId: null, 2, CancellationToken.None);
        Assert.Equal(2, first.Count);
        IReadOnlyList<InboundMailExportEntry> second = await exporter.GetExportPageAsync("bugs@fooddiary.club", first[^1].ReceivedAtUtc.ToOffset(TimeSpan.FromHours(3)), first[^1].Id, 2, CancellationToken.None);
        Assert.Single(second);
        Assert.Equal(3, first.Concat(second).Select(e => e.Id).Distinct().Count());
        Assert.Null(await exporter.GetRawMimeAsync(other.Id, "bugs@fooddiary.club", CancellationToken.None));

        await using var factory = new MailInboxWebApiFactory();
        await using Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<global::Program> configured = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services => {
            services.RemoveAll<IInboundMailExportStore>();
            services.AddSingleton<IInboundMailExportStore>(exporter);
        }));
        using HttpClient client = configured.CreateClient();
        using HttpResponseMessage unauthorized = await client.GetAsync("/api/mail-inbox/export?recipient=bugs%40fooddiary.club");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        client.DefaultRequestHeaders.Add("X-MailInbox-Api-Key", "fedcba9876543210fedcba987654321a");
        using HttpResponseMessage malformed = await client.GetAsync("/api/mail-inbox/export?recipient=bugs%40fooddiary.club&beforeId=" + first[0].Id);
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        InboundMailExportEntry[]? page = await client.GetFromJsonAsync<InboundMailExportEntry[]>("/api/mail-inbox/export?recipient=bugs%40fooddiary.club");
        Assert.Equal(3, page?.Length);
        string mimeUrl = $"/api/mail-inbox/export/{first[0].Id}/mime?recipient=bugs%40fooddiary.club";
        using HttpResponseMessage forbidden = await client.GetAsync(mimeUrl);
        Assert.Equal(HttpStatusCode.Unauthorized, forbidden.StatusCode);
        client.DefaultRequestHeaders.Remove("X-MailInbox-Api-Key");
        client.DefaultRequestHeaders.Add("X-MailInbox-Api-Key", "fedcba9876543210fedcba987654321b");
        byte[] downloaded = await client.GetByteArrayAsync(mimeUrl);
        Assert.Equal(mime, downloaded[..^1]);
        await store.PurgeExpiredAsync(now.AddDays(1), now.AddDays(-1), 100, CancellationToken.None);
        using HttpResponseMessage purged = await client.GetAsync(mimeUrl);
        Assert.Equal(HttpStatusCode.NotFound, purged.StatusCode);
    }
}

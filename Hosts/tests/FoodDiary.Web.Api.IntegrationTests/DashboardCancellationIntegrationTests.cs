using System.Data;
using System.Data.Common;
using System.Net;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class DashboardCancellationIntegrationTests(PostgresApiWebApplicationFactory factory)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    private const string DashboardUrl = "/api/v1/dashboard?date=2026-10-30T00:00:00.000Z&locale=en&trendDays=30&timeZoneOffsetMinutes=180&timeZoneId=Europe%2FMoscow";

    [RequiresDockerFact]
    public async Task Dashboard_SnapshotDeadlineAndRequestAbort_ReturnDifferentResponses() {
        var delay = new SlowSnapshotRead();
        using var requestAbort = new CancellationTokenSource();
        await using WebApplicationFactory<Program> isolated = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => {
            services.AddDbContext<SharedPersistenceDbContext>(options => options.AddInterceptors(delay));
            services.AddSingleton<IStartupFilter>(new AbortFilter(requestAbort.Token));
        }));
        using HttpClient client = isolated.CreateClient();
        using HttpResponseMessage registration = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"dashboard-cancellation-{Guid.NewGuid():N}@example.com", "Password123!", "en"), CancellationToken.None);
        registration.EnsureSuccessStatusCode();
        AuthenticationResponseCookies.ApplyBrowserSession(client, AuthenticationResponseCookies.ReadBrowserSession(registration));
        await client.GrantHealthTrackingConsentAsync(cancellationToken: CancellationToken.None);

        using HttpResponseMessage baseline = await client.GetAsync(DashboardUrl, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, baseline.StatusCode);

        var deadlineRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        delay.Entered = deadlineRead;
        using HttpResponseMessage expired = await client.GetAsync(DashboardUrl, CancellationToken.None);
        Assert.True(deadlineRead.Task.IsCompletedSuccessfully, "The snapshot did not reach the delayed database read.");
        Assert.Equal(HttpStatusCode.InternalServerError, expired.StatusCode);
        Assert.Contains("Server.Unexpected", await expired.Content.ReadAsStringAsync(CancellationToken.None), StringComparison.Ordinal);

        var abortedRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        delay.Entered = abortedRead;
        using var abortedRequest = new HttpRequestMessage(HttpMethod.Get, DashboardUrl);
        abortedRequest.Headers.Add(AbortFilter.Header, "true");
        Task<HttpResponseMessage> request = client.SendAsync(abortedRequest, CancellationToken.None);
        await abortedRead.Task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        await requestAbort.CancelAsync();
        using HttpResponseMessage aborted = await request.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        Assert.Equal(StatusCodes.Status499ClientClosedRequest, (int)aborted.StatusCode);

        delay.Entered = null;
        using HttpResponseMessage retry = await client.GetAsync(DashboardUrl, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
    }

    [ExcludeFromCodeCoverage]
    private sealed class SlowSnapshotRead : DbCommandInterceptor {
        public TaskCompletionSource? Entered { get; set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) {
            if (Entered is { } entered && command.Transaction?.IsolationLevel == IsolationLevel.RepeatableRead &&
                command.CommandText.StartsWith("SELECT EXISTS (", StringComparison.Ordinal) &&
                command.CommandText.Contains("FROM \"Users\"", StringComparison.Ordinal)) {
                command.CommandText = $"SELECT delayed.* FROM pg_sleep(30) CROSS JOIN LATERAL ({command.CommandText.Trim().TrimEnd(';')}) AS delayed";
                entered.TrySetResult();
            }
            return ValueTask.FromResult(result);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class AbortFilter(CancellationToken abort) : IStartupFilter {
        public const string Header = "X-Dashboard-Test-Abort";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app => {
            app.Use(async (context, pipeline) => {
                if (context.Request.Headers.ContainsKey(Header)) {
                    context.RequestAborted = abort;
                }
                await pipeline(context);
            });
            next(app);
        };
    }
}

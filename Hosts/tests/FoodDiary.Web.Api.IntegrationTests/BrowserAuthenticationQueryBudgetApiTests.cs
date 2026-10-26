using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class BrowserAuthenticationQueryBudgetApiTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task FreshSessionRequest_UsesTwoAuthenticationReadsAndDoesNotLoadCredentialOrProfileColumns() {
        var counter = new CommandCounter();
        await using WebApplicationFactory<Program> isolated = factory.WithWebHostBuilder(builder => builder.ConfigureServices(
            services => services.AddDbContext<SharedPersistenceDbContext>((_, options) => options.AddInterceptors(counter))));
        using HttpClient browser = isolated.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using HttpResponseMessage registered = await browser.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"query-budget-{Guid.NewGuid():N}@example.com", "Password123!", "en"));
        registered.EnsureSuccessStatusCode();
        counter.Enabled = true;
        using HttpResponseMessage listed = await browser.GetAsync("/api/v1/auth/sessions");
        counter.Enabled = false;
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        string[] commands = [.. counter.Commands];
        Assert.True(commands.Length == 4,
            $"A fresh session listing must need two authentication reads and its two guarded-listing queries. Observed SQL commands: {commands.Length}.");
        foreach (string command in commands) {
            Assert.DoesNotContain("\"Password\"", command, StringComparison.Ordinal);
            Assert.DoesNotContain("\"UserPreferences\"", command, StringComparison.Ordinal);
            Assert.DoesNotContain("\"UserNutritionProfiles\"", command, StringComparison.Ordinal);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class CommandCounter : DbCommandInterceptor {
        public bool Enabled { get; set; }
        public ConcurrentQueue<string> Commands { get; } = new();
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) {
            if (Enabled) { Commands.Enqueue(command.CommandText); }
            return ValueTask.FromResult(result);
        }
    }
}

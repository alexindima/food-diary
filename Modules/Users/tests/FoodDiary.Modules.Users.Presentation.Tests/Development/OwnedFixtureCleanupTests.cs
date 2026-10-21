using System.Globalization;
using System.Text.Json.Nodes;
using FoodDiary.Mediator;
using FoodDiary.Modules.Images.Service.Contracts.Commands.ProcessOwnedImageDeletions;
using FoodDiary.Modules.Users.Contracts.Commands.PurgeOwnedFixtureUsers;
using FoodDiary.Modules.Users.Presentation.Development;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Modules.Users.Presentation.Tests.Development;

[ExcludeFromCodeCoverage]
[Collection(nameof(OwnedCleanupEnvironment))]
public sealed class OwnedFixtureCleanupTests {
    [Fact]
    public async Task WithoutTheExplicitFlag_LeavesAllServicesUntouched() {
        using var fixture = new CleanupFixture();
        await using WebApplication app = fixture.CreateApp();

        Assert.False(await app.TryRunOwnedFixtureCleanupAsync([]));
        Assert.Empty(fixture.Sender.ReceivedCalls());
    }

    [Theory]
    [InlineData("production")]
    [InlineData("missing-config")]
    [InlineData("relative-config")]
    [InlineData("missing-manifest-argument")]
    public async Task Cleanup_RequiresAnExplicitOwnedDevelopmentConfiguration(string failure) {
        using var fixture = new CleanupFixture();
        if (string.Equals(failure, "missing-config", StringComparison.Ordinal)) { fixture.SetConfiguration(path: null); }
        if (string.Equals(failure, "relative-config", StringComparison.Ordinal)) { fixture.SetConfiguration("task.json"); }
        await using WebApplication app = fixture.CreateApp(environment: string.Equals(failure, "production", StringComparison.Ordinal) ? Environments.Production : Environments.Development);
        string[] args = string.Equals(failure, "missing-manifest-argument", StringComparison.Ordinal) ? ["--purge-owned-fixtures"] : fixture.Args;

        await Assert.ThrowsAsync<InvalidOperationException>(() => app.TryRunOwnedFixtureCleanupAsync(args));

        Assert.Empty(fixture.Sender.ReceivedCalls());
    }

    [Theory]
    [InlineData("runtime-id")]
    [InlineData("checkout")]
    [InlineData("manifest-path")]
    [InlineData("database-host")]
    [InlineData("database-name")]
    [InlineData("database-port")]
    public async Task Cleanup_RejectsUnmatchedManifestRuntimeOrDatabaseOwnershipBeforeDispatch(string failure) {
        using var fixture = new CleanupFixture();
        JsonObject manifest = fixture.Manifest();
        if (string.Equals(failure, "runtime-id", StringComparison.Ordinal)) { manifest["runtimeId"] = Guid.NewGuid(); }
        if (string.Equals(failure, "checkout", StringComparison.Ordinal)) { manifest["checkout"] = Path.GetDirectoryName(fixture.Checkout); }
        fixture.WriteManifest(manifest);
        string connection = failure switch {
            "database-host" => "Host=192.0.2.1;Database=fooddiary_task;Port=56999",
            "database-name" => "Host=127.0.0.1;Database=other;Port=56999",
            "database-port" => "Host=127.0.0.1;Database=fooddiary_task;Port=56998",
            _ => CleanupFixture.Connection,
        };
        string[] args = fixture.Args;
        if (string.Equals(failure, "manifest-path", StringComparison.Ordinal)) {
            string other = Path.Combine(fixture.Root, "other-manifest.json");
            File.Copy(fixture.ManifestPath, other);
            args = ["--purge-owned-fixtures", other];
        }
        await using WebApplication app = fixture.CreateApp(connection: connection);

        await Assert.ThrowsAsync<InvalidOperationException>(() => app.TryRunOwnedFixtureCleanupAsync(args));

        Assert.Empty(fixture.Sender.ReceivedCalls());
    }

    [Theory]
    [InlineData("foreign-user")]
    [InlineData("traversal")]
    [InlineData("backslash")]
    [InlineData("unbounded")]
    public async Task Cleanup_ValidatesTheCompleteObjectListBeforePurgingAnyAccount(string failure) {
        using var fixture = new CleanupFixture();
        string owned = $"users/{fixture.User:D}/images/owned.jpg";
        string invalid = failure switch {
            "foreign-user" => $"users/{Guid.NewGuid():D}/images/foreign.jpg",
            "traversal" => $"users/{fixture.User:D}/images/../outside.jpg",
            "backslash" => $"users/{fixture.User:D}/images/folder\\image.jpg",
            _ => owned,
        };
        string[] keys = string.Equals(failure, "unbounded", StringComparison.Ordinal)
            ? [.. Enumerable.Range(0, 1001).Select(index => string.Create(CultureInfo.InvariantCulture, $"users/{fixture.User:D}/images/{index}.jpg"))]
            : [owned, invalid];
        fixture.WriteManifest(fixture.Manifest(keys));
        await using WebApplication app = fixture.CreateApp();

        await Assert.ThrowsAsync<InvalidOperationException>(() => app.TryRunOwnedFixtureCleanupAsync(fixture.Args));

        Assert.Empty(fixture.Sender.ReceivedCalls());
    }

    [Fact]
    public async Task Cleanup_PurgesOnlyManifestUsersThenProcessesDistinctOwnedKeysWithTheSameCancellation() {
        using var fixture = new CleanupFixture();
        string key = $"users/{fixture.User:D}/images/owned.jpg";
        fixture.WriteManifest(fixture.Manifest([key, key]));
        using var cancellation = new CancellationTokenSource();
        List<string> order = [];
        fixture.Sender.Send<int>(Arg.Any<PurgeOwnedFixtureUsersCommand>(), cancellation.Token).Returns(_ => { order.Add("users"); return 1; });
        fixture.Sender.Send<int>(Arg.Any<ProcessOwnedImageDeletionsCommand>(), cancellation.Token).Returns(_ => { order.Add("images"); return 1; });
        await using WebApplication app = fixture.CreateApp();

        Assert.True(await app.TryRunOwnedFixtureCleanupAsync(fixture.Args, cancellation.Token));

        await fixture.Sender.Received(1).Send<int>(Arg.Is<PurgeOwnedFixtureUsersCommand>(request =>
            request.RunId == fixture.RunId && request.UserIds.SequenceEqual(new[] { fixture.User }) && request.OlderThanUtc == CleanupFixture.Now.UtcDateTime), cancellation.Token);
        await fixture.Sender.Received(1).Send<int>(Arg.Is<ProcessOwnedImageDeletionsCommand>(request =>
            request.UserIds.SequenceEqual(new[] { fixture.User }) && request.ObjectKeys.SequenceEqual(new[] { key }, StringComparer.Ordinal)), cancellation.Token);
        Assert.Equal(new[] { "users", "images" }, order, StringComparer.Ordinal);
        Assert.Equal(2, fixture.Sender.ReceivedCalls().Count());
        Assert.Empty(app.Urls);
    }

    [ExcludeFromCodeCoverage]
    private sealed class CleanupFixture : IDisposable {
        public const string Connection = "Host=127.0.0.1;Database=fooddiary_task;Port=56999";
        public static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
        private readonly string? _previousConfiguration = Environment.GetEnvironmentVariable("FOODDIARY_TASK_CONFIG");
        private readonly string _previousDirectory = Directory.GetCurrentDirectory();
        private readonly string _taskRuntimeBase;
        private readonly Guid _runtimeId = Guid.NewGuid();
        public ISender Sender { get; } = Substitute.For<ISender>();
        public Guid RunId { get; } = Guid.NewGuid();
        public Guid User { get; } = Guid.NewGuid();
        public string Checkout { get; }
        public string Root { get; }
        public string ManifestPath { get; }
        public string[] Args => ["--purge-owned-fixtures", ManifestPath];

        public CleanupFixture() {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FoodDiary.slnx"))) { directory = directory.Parent; }
            Checkout = directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
            _taskRuntimeBase = Path.GetFullPath(Path.Combine(Checkout, ".artifacts", "task-runtime"));
            Root = Path.Combine(_taskRuntimeBase, "coverage-401-" + Guid.NewGuid().ToString("N"));
            ManifestPath = Path.Combine(Root, "fixtures", RunId.ToString("D"), "manifest.json");
            Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath)!);
            File.WriteAllText(Path.Combine(Root, "runtime.json"), new JsonObject {
                ["checkout"] = Checkout,
                ["id"] = _runtimeId,
                ["ports"] = new JsonObject { ["postgres"] = 56999 },
            }.ToJsonString());
            Directory.SetCurrentDirectory(Checkout);
            SetConfiguration(Path.Combine(Root, "task.json"));
            WriteManifest(Manifest());
        }

        public void SetConfiguration(string? path) => Environment.SetEnvironmentVariable("FOODDIARY_TASK_CONFIG", path);

        public JsonObject Manifest(string[]? keys = null) => new() {
            ["runId"] = RunId,
            ["runtimeId"] = _runtimeId,
            ["checkout"] = Checkout,
            ["accounts"] = new JsonObject { ["owner"] = new JsonObject { ["id"] = User } },
            ["objects"] = new JsonArray((keys ?? []).Select(key => (JsonNode)new JsonObject { ["key"] = key }).ToArray()),
        };

        public void WriteManifest(JsonObject manifest) => File.WriteAllText(ManifestPath, manifest.ToJsonString());

        public WebApplication CreateApp(string environment = "Development", string connection = Connection) {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions {
                EnvironmentName = environment,
                ContentRootPath = Root,
            });
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) { ["ConnectionStrings:DefaultConnection"] = connection });
            builder.Logging.ClearProviders();
            builder.Services.AddSingleton(Sender);
            builder.Services.AddSingleton<TimeProvider>(new FixedClock());
            return builder.Build();
        }

        public void Dispose() {
            SetConfiguration(_previousConfiguration);
            Directory.SetCurrentDirectory(_previousDirectory);
            string target = Path.GetFullPath(Root);
            if (!target.StartsWith(_taskRuntimeBase + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(target).StartsWith("coverage-401-", StringComparison.Ordinal)) {
                throw new InvalidOperationException("Refusing cleanup outside the owned test fixture directory.");
            }
            Directory.Delete(target, recursive: true);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock : TimeProvider {
        public override DateTimeOffset GetUtcNow() => CleanupFixture.Now;
    }
}

using System.Text.Json;
using FoodDiary.Development.Mcp.Infrastructure;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
[Collection("PowerShell Wiki process")]
public sealed class DiagnosticFileCoverageTests : IDisposable {
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "fooddiary-mcp-diagnostic-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void RepositoryRootResolver_UsesConfiguredValidRepository() {
        string repository = CreateWikiRepository();
        string? original = Environment.GetEnvironmentVariable(RepositoryRootResolver.RepositoryRootEnvironmentVariable);
        try {
            Environment.SetEnvironmentVariable(RepositoryRootResolver.RepositoryRootEnvironmentVariable, repository);

            string resolved = RepositoryRootResolver.Resolve();

            Assert.Equal(Path.GetFullPath(repository), resolved);
        } finally {
            Environment.SetEnvironmentVariable(RepositoryRootResolver.RepositoryRootEnvironmentVariable, original);
        }
    }

    [Fact]
    public void RepositoryRootResolver_RejectsConfiguredDirectoryWithoutWiki() {
        Directory.CreateDirectory(_root);
        string? original = Environment.GetEnvironmentVariable(RepositoryRootResolver.RepositoryRootEnvironmentVariable);
        try {
            Environment.SetEnvironmentVariable(RepositoryRootResolver.RepositoryRootEnvironmentVariable, _root);

            DevelopmentMcpException exception = Assert.Throws<DevelopmentMcpException>(RepositoryRootResolver.Resolve);

            Assert.Equal(DevelopmentMcpErrorCodes.RepositoryNotFound, exception.ErrorCode);
        } finally {
            Environment.SetEnvironmentVariable(RepositoryRootResolver.RepositoryRootEnvironmentVariable, original);
        }
    }

    [Fact]
    public async Task WikiIndexManifest_NormalizesAndSortsValidPaths() {
        string repository = CreateWikiRepository();
        string manifest = Path.Combine(repository, ".llm-wiki", "policies", "query-indexes.json");
        Directory.CreateDirectory(Path.GetDirectoryName(manifest)!);
        await File.WriteAllTextAsync(manifest, """
            { "schemaVersion": 1, "paths": ["z\\index.json", "a/index.json", " ", "A/index.json"] }
            """);

        string[] paths = await WikiIndexManifest.ReadPathsAsync(repository, CancellationToken.None);

        Assert.Equal(["a/index.json", "z/index.json"], paths);
    }

    [Theory]
    [InlineData("{ not-json")]
    [InlineData("{ \"schemaVersion\": 2, \"paths\": [\"index.json\"] }")]
    [InlineData("{ \"schemaVersion\": 1, \"paths\": [\"../outside.json\"] }")]
    public async Task WikiIndexManifest_RejectsUnavailableOrInvalidManifest(string content) {
        string repository = CreateWikiRepository();
        string manifest = Path.Combine(repository, ".llm-wiki", "policies", "query-indexes.json");
        Directory.CreateDirectory(Path.GetDirectoryName(manifest)!);
        await File.WriteAllTextAsync(manifest, content);

        DevelopmentMcpException exception = await Assert.ThrowsAsync<DevelopmentMcpException>(
            () => WikiIndexManifest.ReadPathsAsync(repository, CancellationToken.None));

        Assert.Equal(DevelopmentMcpErrorCodes.WikiUnavailable, exception.ErrorCode);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public async Task WikiVerificationReceipt_AcceptsOnlyCurrentSchema(int schemaVersion, bool expectedReceipt) {
        string repository = CreateWikiRepository();
        string gitDirectory = Path.Combine(repository, ".git");
        string receiptPath = Path.Combine(gitDirectory, "llm-wiki", "index-verification.json");
        Directory.CreateDirectory(Path.GetDirectoryName(receiptPath)!);
        await File.WriteAllTextAsync(receiptPath, JsonSerializer.Serialize(new {
            schemaVersion,
            gitHead = "head",
            sourceFingerprint = "source",
            indexFingerprint = "index",
            verifiedAtUtc = DateTimeOffset.UtcNow,
        }));

        WikiVerificationReceipt? receipt = await WikiVerificationReceipt.ReadAsync(repository, CancellationToken.None);

        Assert.Equal(expectedReceipt, receipt is not null);
    }

    [Fact]
    public async Task WikiVerificationReceipt_ReturnsNullForMalformedJson() {
        string repository = CreateWikiRepository();
        string receiptPath = Path.Combine(repository, ".git", "llm-wiki", "index-verification.json");
        Directory.CreateDirectory(Path.GetDirectoryName(receiptPath)!);
        await File.WriteAllTextAsync(receiptPath, "not-json");

        WikiVerificationReceipt? receipt = await WikiVerificationReceipt.ReadAsync(repository, CancellationToken.None);

        Assert.Null(receipt);
    }

    public void Dispose() {
        if (Directory.Exists(_root)) {
            Directory.Delete(_root, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    private string CreateWikiRepository() {
        string repository = Path.Combine(_root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repository, ".git"));
        string wikiDirectory = Path.Combine(repository, ".llm-wiki");
        Directory.CreateDirectory(wikiDirectory);
        File.WriteAllText(Path.Combine(wikiDirectory, "wiki.ps1"), string.Empty);
        return repository;
    }
}

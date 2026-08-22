using System.Text.Json;
using FoodDiary.Development.Mcp.Infrastructure;
using NSubstitute;

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

    [Fact]
    public async Task ReadGitHead_ReturnsDetachedHead() {
        string repository = CreateWikiRepository();
        await File.WriteAllTextAsync(Path.Combine(repository, ".git", "HEAD"), "detached-head\n");

        string head = await ServerStatusService.ReadGitHeadAsync(repository, CancellationToken.None);

        Assert.Equal("detached-head", head);
    }

    [Fact]
    public async Task ReadGitHead_ResolvesLooseReference() {
        string repository = CreateWikiRepository();
        string reference = Path.Combine(repository, ".git", "refs", "heads", "main");
        Directory.CreateDirectory(Path.GetDirectoryName(reference)!);
        await File.WriteAllTextAsync(Path.Combine(repository, ".git", "HEAD"), "ref: refs/heads/main\n");
        await File.WriteAllTextAsync(reference, "loose-head\n");

        string head = await ServerStatusService.ReadGitHeadAsync(repository, CancellationToken.None);

        Assert.Equal("loose-head", head);
    }

    [Fact]
    public async Task ReadGitHead_ResolvesGitFileCommonDirectoryAndPackedReference() {
        string repository = CreateWikiRepository();
        Directory.Delete(Path.Combine(repository, ".git"), recursive: true);
        string worktreeGit = Path.Combine(_root, "worktree-git");
        string commonGit = Path.Combine(_root, "common-git");
        Directory.CreateDirectory(worktreeGit);
        Directory.CreateDirectory(commonGit);
        await File.WriteAllTextAsync(Path.Combine(repository, ".git"), $"gitdir: {worktreeGit}\n");
        await File.WriteAllTextAsync(Path.Combine(worktreeGit, "commondir"), commonGit);
        await File.WriteAllTextAsync(Path.Combine(worktreeGit, "HEAD"), "ref: refs/heads/main\n");
        await File.WriteAllTextAsync(
            Path.Combine(commonGit, "packed-refs"),
            "ignored refs/heads/other\npacked-head refs/heads/main\n");

        string head = await ServerStatusService.ReadGitHeadAsync(repository, CancellationToken.None);

        Assert.Equal("packed-head", head);
    }

    [Fact]
    public async Task ReadGitHead_RejectsInvalidGitDirectoryMarkerAndMissingReference() {
        string invalidMarkerRepository = CreateWikiRepository();
        Directory.Delete(Path.Combine(invalidMarkerRepository, ".git"), recursive: true);
        await File.WriteAllTextAsync(Path.Combine(invalidMarkerRepository, ".git"), "invalid");
        await Assert.ThrowsAsync<DevelopmentMcpException>(() =>
            ServerStatusService.ReadGitHeadAsync(invalidMarkerRepository, CancellationToken.None));

        string missingReferenceRepository = CreateWikiRepository();
        await File.WriteAllTextAsync(
            Path.Combine(missingReferenceRepository, ".git", "HEAD"),
            "ref: refs/heads/missing\n");
        await File.WriteAllTextAsync(Path.Combine(missingReferenceRepository, ".git", "packed-refs"), string.Empty);
        await Assert.ThrowsAsync<DevelopmentMcpException>(() =>
            ServerStatusService.ReadGitHeadAsync(missingReferenceRepository, CancellationToken.None));
    }

    [Fact]
    public void ServerRuntimeIdentity_CaptureReadsCurrentAssembly() {
        var identity = ServerRuntimeIdentity.Capture("repository-head");

        Assert.Multiple(
            () => Assert.Equal(Environment.ProcessId, identity.ProcessId),
            () => Assert.Equal("repository-head", identity.RepositoryHeadAtStartup),
            () => Assert.NotEmpty(identity.AssemblySha256),
            () => Assert.NotEmpty(identity.ModuleVersionId));
    }

    [Fact]
    public async Task ServerStatusService_ReportsAndClassifiesCurrentRepositoryState() {
        string repository = RepositoryRootResolver.Resolve();
        string head = await ServerStatusService.ReadGitHeadAsync(repository, CancellationToken.None);
        IChangeSetSnapshotService snapshots = Substitute.For<IChangeSetSnapshotService>();
        snapshots.GetAsync(Arg.Any<CancellationToken>()).Returns(new ChangeSetSnapshot(
            head,
            "snapshot-fingerprint",
            [
                "FoodDiary.Domain/Entity.cs",
                ".llm-wiki/generated/index.json",
                ".llm-wiki/reviews/source-impact-reviews.json",
            ],
            DateTimeOffset.UtcNow));
        var identity = ServerRuntimeIdentity.Capture(head);
        var service = new ServerStatusService(TimeProvider.System, identity, snapshots);

        ServerStatus status = await service.GetStatusAsync(CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(repository, status.RepositoryRoot),
            () => Assert.Equal(head, status.GitHead),
            () => Assert.True(status.WorktreeDirty),
            () => Assert.Contains("FoodDiary.Domain/Entity.cs", status.SourceChangedPaths, StringComparer.Ordinal),
            () => Assert.Contains(".llm-wiki/generated/index.json", status.DerivedWikiPaths, StringComparer.Ordinal),
            () => Assert.Contains(".llm-wiki/reviews/source-impact-reviews.json", status.ReviewMetadataPaths, StringComparer.Ordinal),
            () => Assert.NotEmpty(status.Indexes));
    }

    [Fact]
    public async Task RepositorySourceFingerprint_RejectsDirectoryOutsideGitRepository() {
        string directory = Path.Combine(_root, "not-a-git-repository");
        Directory.CreateDirectory(directory);

        DevelopmentMcpException exception = await Assert.ThrowsAsync<DevelopmentMcpException>(() =>
            RepositorySourceFingerprint.ComputeAsync(directory, CancellationToken.None));

        Assert.Equal(DevelopmentMcpErrorCodes.RepositoryNotFound, exception.ErrorCode);
    }

    [Fact]
    public async Task RepositorySourceFingerprint_PropagatesCallerCancellation() {
        string repository = RepositoryRootResolver.Resolve();
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            RepositorySourceFingerprint.ComputeAsync(repository, cancellation.Token));
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

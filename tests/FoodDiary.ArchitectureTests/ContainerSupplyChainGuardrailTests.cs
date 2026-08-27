using System.Text.RegularExpressions;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ContainerSupplyChainGuardrailTests {
    private static readonly string[] HardenedRuntimeServices = ["api", "mail-relay", "telegram-bot", "job-manager", "client", "nginx"];
    private static readonly string[] ExpectedProductionProjects = [
        "FoodDiary.Web.Api/FoodDiary.Web.Api.csproj",
        "FoodDiary.JobManager/FoodDiary.JobManager.csproj",
        "FoodDiary.Initializer/FoodDiary.Initializer.csproj",
        "FoodDiary.Telegram.Bot/FoodDiary.Telegram.Bot.csproj",
        "MailRelay/FoodDiary.MailRelay.WebApi/FoodDiary.MailRelay.WebApi.csproj",
        "MailRelay/FoodDiary.MailRelay.Initializer/FoodDiary.MailRelay.Initializer.csproj",
        "MailInbox/FoodDiary.MailInbox.WebApi/FoodDiary.MailInbox.WebApi.csproj",
        "MailInbox/FoodDiary.MailInbox.Initializer/FoodDiary.MailInbox.Initializer.csproj",
    ];

    private static readonly string[] ExpectedDockerfiles = [
        "FoodDiary.Initializer/Dockerfile",
        "FoodDiary.JobManager/Dockerfile",
        "FoodDiary.Telegram.Bot/Dockerfile",
        "FoodDiary.Web.Api/Dockerfile",
        "FoodDiary.Web.Client/Dockerfile",
        "MailInbox/FoodDiary.MailInbox.Initializer/Dockerfile",
        "MailInbox/FoodDiary.MailInbox.WebApi/Dockerfile",
        "MailRelay/FoodDiary.MailRelay.Initializer/Dockerfile",
        "MailRelay/FoodDiary.MailRelay.WebApi/Dockerfile",
    ];

    private static readonly string[] ExpectedBuildIds = [
        "build_api",
        "build_telegram_bot",
        "build_initializer",
        "build_job_manager",
        "build_mail_relay",
        "build_mailrelay_initializer",
        "build_mail_inbox",
        "build_mailinbox_postgres_tls_init",
        "build_mailinbox_initializer",
        "build_client",
        "build_tech_radar",
    ];

    [Fact]
    public void DeployWorkflow_PublishesIndexedImagesWithSupplyChainMetadata() {
        string workflow = ReadDeployWorkflow();
        string[] buildSteps = [
            .. workflow
            .Split("      - name: Build and push ", StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(section => section.Split(
                "      - name: Record failure step",
                2,
                StringSplitOptions.None)[0]),
        ];

        Assert.Equal(ExpectedBuildIds.Length, buildSteps.Length);
        Assert.DoesNotContain("provenance: false", workflow, StringComparison.Ordinal);

        foreach (string step in buildSteps) {
            Assert.Contains("provenance: mode=max", step, StringComparison.Ordinal);
            Assert.Contains("sbom: true", step, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DeployWorkflow_SignsAndVerifiesEveryPublishedDigestBeforeSshSetup() {
        string workflow = ReadDeployWorkflow();

        Assert.Contains("id-token: write", workflow, StringComparison.Ordinal);
        Assert.Contains("uses: sigstore/cosign-installer@398d4b0eeef1380460a10c8013a76f728fb906ac # v3", workflow, StringComparison.Ordinal);

        int signingStep = workflow.IndexOf("- name: Sign and verify production images", StringComparison.Ordinal);
        int sshStep = workflow.IndexOf("- name: Setup SSH", StringComparison.Ordinal);

        Assert.True(signingStep >= 0, "The image signing step is missing.");
        Assert.True(sshStep > signingStep, "Images must be signed and verified before SSH deployment starts.");

        foreach (string buildId in ExpectedBuildIds) {
            Assert.Contains($"steps.{buildId}.outputs.digest", workflow, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DeployWorkflow_SerializesDeploysWithoutCancellingTheActiveRun() {
        string workflow = ReadDeployWorkflow();

        Assert.Contains("group: fooddiary-production-deploy", workflow, StringComparison.Ordinal);
        Assert.Contains("cancel-in-progress: false", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("cancel-in-progress: true", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void Workflows_PinEveryExternalActionToACommitSha() {
        string workflowsRoot = ArchitectureTestPaths.FromRoot(".github", "workflows");
        string[] violations = [.. Directory
            .EnumerateFiles(workflowsRoot, "*.*", SearchOption.AllDirectories)
            .Where(static path => Path.GetExtension(path).Equals(".yml", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(path).Equals(".yaml", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select(static line => {
                    string normalized = line.TrimStart();
                    return normalized.StartsWith("- ", StringComparison.Ordinal) ? normalized[2..].TrimStart() : normalized;
                })
                .Where(static line => line.StartsWith("uses:", StringComparison.Ordinal))
                .Select(static line => line["uses:".Length..].Split('#', 2)[0].Trim())
                .Where(static use => !use.StartsWith("./", StringComparison.Ordinal))
                .Select(use => {
                    int separator = use.LastIndexOf('@');
                    return new {
                    Path = Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path),
                    Action = separator > 0 ? use[..separator] : use,
                    Reference = separator > 0 ? use[(separator + 1)..] : string.Empty,
                };
                }))
            .Where(static item => item.Reference.Length != 40 ||
                item.Reference.Any(static character => !Uri.IsHexDigit(character) || char.IsUpper(character)))
            .Select(item => $"{item.Path}: {item.Action}@{item.Reference}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void DeployWorkflow_AuthenticatesSshHostAndRequiresProductionCredentials() {
        string workflow = ReadDeployWorkflow();
        string compose = File.ReadAllText(ArchitectureTestPaths.FromRoot("docker-compose.yml"));

        Assert.Multiple(
            () => Assert.Contains("secrets.SSH_KNOWN_HOSTS", workflow, StringComparison.Ordinal),
            () => Assert.Contains("ssh-keygen -F", workflow, StringComparison.Ordinal),
            () => Assert.Contains("StrictHostKeyChecking=yes", workflow, StringComparison.Ordinal),
            () => Assert.DoesNotContain("StrictHostKeyChecking=no", workflow, StringComparison.Ordinal),
            () => Assert.DoesNotContain("StrictHostKeyChecking=accept-new", workflow, StringComparison.Ordinal),
            () => Assert.DoesNotContain("UserKnownHostsFile=/dev/null", workflow, StringComparison.Ordinal),
            () => Assert.DoesNotContain("GlobalKnownHostsFile=/dev/null", workflow, StringComparison.Ordinal),
            () => Assert.Contains("MailRelayBroker__Password MailRelay__RequireMailgunWebhookSignature MailRelay__MailgunWebhookSigningKey", workflow, StringComparison.Ordinal),
            () => Assert.Contains("MAIL_INBOX_POSTGRES_PASSWORD MAIL_INBOX_POSTGRES_RUNTIME_PASSWORD", workflow, StringComparison.Ordinal),
            () => Assert.Contains("case \"${value,,}\"", workflow, StringComparison.Ordinal),
            () => Assert.Contains("Production requires MailRelay__RequireMailgunWebhookSignature=true", workflow, StringComparison.Ordinal),
            () => Assert.Contains("${POSTGRES_PASSWORD:?Set POSTGRES_PASSWORD}", compose, StringComparison.Ordinal),
            () => Assert.Contains("${MAIL_RELAY_POSTGRES_PASSWORD:?Set MAIL_RELAY_POSTGRES_PASSWORD}", compose, StringComparison.Ordinal),
            () => Assert.Contains("${MailRelayBroker__Password:?Set MailRelayBroker__Password}", compose, StringComparison.Ordinal),
            () => Assert.DoesNotContain("Password=${POSTGRES_PASSWORD:-", compose, StringComparison.Ordinal),
            () => Assert.DoesNotContain("Password=${MAIL_RELAY_POSTGRES_PASSWORD:-", compose, StringComparison.Ordinal),
            () => Assert.DoesNotContain("RABBITMQ_DEFAULT_PASS: ${MailRelayBroker__Password:-", compose, StringComparison.Ordinal));
    }

    [Fact]
    public void SigningScript_RejectsPlainManifestsAndVerifiesKeylessSignature() {
        string scriptPath = ArchitectureTestPaths.FromRoot(
            ".github",
            "scripts",
            "sign-and-verify-images.sh");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("application/vnd.oci.image.index.v1+json", script, StringComparison.Ordinal);
        Assert.Contains("application/vnd.docker.distribution.manifest.list.v2+json", script, StringComparison.Ordinal);
        Assert.Contains("must resolve to an image index", script, StringComparison.Ordinal);
        Assert.Contains("cosign sign --yes", script, StringComparison.Ordinal);
        Assert.Contains("cosign verify", script, StringComparison.Ordinal);
        Assert.Contains("--certificate-identity", script, StringComparison.Ordinal);
        Assert.Contains("--certificate-oidc-issuer", script, StringComparison.Ordinal);
    }

    [Fact]
    public void NuGetRestore_UsesRepositorySourceAllowlistAndCommittedLockFiles() {
        string configPath = ArchitectureTestPaths.FromRoot("NuGet.config");
        var config = System.Xml.Linq.XDocument.Load(configPath);
        System.Xml.Linq.XElement root = Assert.IsType<System.Xml.Linq.XElement>(config.Root);
        System.Xml.Linq.XElement packageSources = Assert.Single(root.Elements("packageSources"));
        System.Xml.Linq.XElement auditSources = Assert.Single(root.Elements("auditSources"));
        System.Xml.Linq.XElement sourceMapping = Assert.Single(root.Elements("packageSourceMapping"));

        Assert.Single(packageSources.Elements("clear"));
        System.Xml.Linq.XElement packageSource = Assert.Single(packageSources.Elements("add"));
        Assert.Equal("nuget.org", packageSource.Attribute("key")?.Value);
        Assert.Equal("https://api.nuget.org/v3/index.json", packageSource.Attribute("value")?.Value);

        Assert.Single(auditSources.Elements("clear"));
        System.Xml.Linq.XElement auditSource = Assert.Single(auditSources.Elements("add"));
        Assert.Equal("nuget.org", auditSource.Attribute("key")?.Value);
        Assert.Equal("https://api.nuget.org/v3/index.json", auditSource.Attribute("value")?.Value);

        Assert.Single(sourceMapping.Elements("clear"));
        System.Xml.Linq.XElement mapping = Assert.Single(sourceMapping.Elements("packageSource"));
        Assert.Equal("nuget.org", mapping.Attribute("key")?.Value);
        Assert.Equal("*", Assert.Single(mapping.Elements("package")).Attribute("pattern")?.Value);

        string buildProps = File.ReadAllText(ArchitectureTestPaths.FromRoot("Directory.Build.props"));
        Assert.Contains("<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>", buildProps, StringComparison.Ordinal);
        Assert.Contains(".nuget\\lockfiles\\$(MSBuildProjectName).packages.lock.json", buildProps, StringComparison.Ordinal);

        string lockDirectory = ArchitectureTestPaths.FromRoot(".nuget", "lockfiles");
        var solution = System.Xml.Linq.XDocument.Load(ArchitectureTestPaths.FromRoot("FoodDiary.slnx"));
        string[] projectNames = [
            .. solution
                .Descendants("Project")
                .Select(project => project.Attribute("Path")?.Value)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => Path.GetFileNameWithoutExtension(path!)!)
                .Order(StringComparer.Ordinal),
        ];
        string[] lockNames = [
            .. Directory
                .EnumerateFiles(lockDirectory, "*.packages.lock.json", SearchOption.TopDirectoryOnly)
                .Select(path => Path.GetFileName(path)[..^".packages.lock.json".Length])
                .Order(StringComparer.Ordinal),
        ];
        string[] expectedLockNames = [.. projectNames
            .Concat([
                "LlmWiki.ContractReferenceExtractor",
                "LlmWiki.RoslynExtractor",
                "LlmWiki.SqliteReader",
            ])
            .Order(StringComparer.Ordinal)];

        Assert.Equal(expectedLockNames, lockNames);
    }

    [Fact]
    public void Ci_RestoresLockedGraphsAndAuditsEveryProductionHost() {
        string workflow = File.ReadAllText(ArchitectureTestPaths.FromRoot(".github", "workflows", "ci-tests.yml"));
        string[] restoreCommands = [
            .. workflow
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("dotnet restore FoodDiary.slnx", StringComparison.Ordinal)),
        ];

        Assert.Equal(3, restoreCommands.Length);
        Assert.All(restoreCommands, command => Assert.Contains("--locked-mode", command, StringComparison.Ordinal));

        foreach (string project in ExpectedProductionProjects) {
            Assert.Contains($"\"{project}\"", workflow, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ContainerDefinitions_PinExternalImagesAndLockedRestoreInputs() {
        string[] dockerfiles = [
            .. ExpectedDockerfiles.Select(path => ArchitectureTestPaths.FromRoot(path.Split('/'))),
        ];

        Assert.Equal(9, dockerfiles.Length);
        foreach (string dockerfile in dockerfiles) {
            string contents = File.ReadAllText(dockerfile);
            string[] fromLines = [
                .. contents
                    .Split('\n')
                    .Select(line => line.Trim())
                    .Where(line => line.StartsWith("FROM ", StringComparison.Ordinal)),
            ];

            Assert.NotEmpty(fromLines);
            Assert.All(fromLines, line => Assert.Matches(@"^FROM [^\s]+@sha256:[0-9a-f]{64}(?: AS \w+)?\r?$", line));

            if (dockerfile.EndsWith("FoodDiary.Web.Client\\Dockerfile", StringComparison.OrdinalIgnoreCase) ||
                dockerfile.EndsWith("FoodDiary.Web.Client/Dockerfile", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            Assert.Contains("NuGet.config", contents, StringComparison.Ordinal);
            Assert.Contains("COPY .nuget/lockfiles/ .nuget/lockfiles/", contents, StringComparison.Ordinal);
            Assert.Contains("COPY FoodDiary.Analyzers/*.csproj FoodDiary.Analyzers/", contents, StringComparison.Ordinal);
            Assert.Contains("COPY FoodDiary.Analyzers/ FoodDiary.Analyzers/", contents, StringComparison.Ordinal);
            Assert.Contains("dotnet restore", contents, StringComparison.Ordinal);
            Assert.Contains("--locked-mode", contents, StringComparison.Ordinal);
        }

        string compose = File.ReadAllText(ArchitectureTestPaths.FromRoot("docker-compose.yml"));
        Assert.DoesNotContain(":-latest", compose, StringComparison.Ordinal);
        Assert.Contains("postgres:17-alpine@sha256:", compose, StringComparison.Ordinal);
        Assert.Contains("rabbitmq:4-management@sha256:", compose, StringComparison.Ordinal);
        Assert.Contains("redis:7-alpine@sha256:", compose, StringComparison.Ordinal);
        Assert.Contains("nginx:alpine@sha256:", compose, StringComparison.Ordinal);
    }

    [Fact]
    public void InternetFacingAndApplicationContainers_UseRuntimeHardeningBaseline() {
        string compose = File.ReadAllText(ArchitectureTestPaths.FromRoot("docker-compose.yml"));
        foreach (string serviceName in HardenedRuntimeServices) {
            Match service = Regex.Match(
                compose,
                $@"(?ms)^  {Regex.Escape(serviceName)}:\r?\n.*?(?=^  [a-zA-Z0-9_-]+:\r?$|\z)",
                RegexOptions.None,
                TimeSpan.FromSeconds(1));
            Assert.True(service.Success, $"Compose service '{serviceName}' is missing.");
            string block = service.Value;

            Assert.Multiple(
                () => Assert.Contains("read_only: true", block, StringComparison.Ordinal),
                () => Assert.Contains("cap_drop:", block, StringComparison.Ordinal),
                () => Assert.Contains("- ALL", block, StringComparison.Ordinal),
                () => Assert.Contains("no-new-privileges:true", block, StringComparison.Ordinal),
                () => Assert.Contains("tmpfs:", block, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void DeployWorkflow_DeploysTheSignedBuildOutputDigests() {
        string workflow = ReadDeployWorkflow();

        Assert.Contains("SOURCE_COMMIT_SHA: ${{ github.event.workflow_run.head_sha || github.sha }}", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain(":sha-${{ github.sha }}", workflow, StringComparison.Ordinal);

        foreach (string buildId in ExpectedBuildIds) {
            Assert.Contains($"@${{{{ steps.{buildId}.outputs.digest }}}}", workflow, StringComparison.Ordinal);
        }

        Assert.Contains("docker create \"$CLIENT_IMAGE_REF\"", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("docker create ghcr.io/alexindima/food-diary/client:${DEPLOY_IMAGE_TAG}", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void DeployWorkflow_ProvisionsMailInboxPostgresTlsBeforeDatabaseInitialization() {
        string workflow = ReadDeployWorkflow();
        string tlsInitializerDockerfile = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "MailInbox",
            "FoodDiary.MailInbox.WebApi",
            "Dockerfile.postgres-tls-init"));

        Assert.Matches(@"(?m)^FROM postgres:18-alpine@sha256:[0-9a-f]{64}\r?$", tlsInitializerDockerfile);
        Assert.Contains("MAIL_INBOX_POSTGRES_TLS_INIT_IMAGE_REF=\"${{ env.IMAGE_PREFIX }}/mailinbox-postgres-tls-init@${{ steps.build_mailinbox_postgres_tls_init.outputs.digest }}\"", workflow, StringComparison.Ordinal);
        Assert.Contains("$SCP_CMD MailInbox/FoodDiary.MailInbox.WebApi/mailinbox-pg_hba.conf", workflow, StringComparison.Ordinal);

        int postgresStart = workflow.IndexOf("docker compose --profile mail-inbox up -d mailinbox-postgres", StringComparison.Ordinal);
        int databaseInitialization = workflow.IndexOf("docker compose --profile mail-inbox run -T --rm mailinbox-db-init update", StringComparison.Ordinal);
        Assert.True(postgresStart >= 0, "MailInbox PostgreSQL TLS startup is missing from deployment.");
        Assert.True(databaseInitialization > postgresStart, "MailInbox PostgreSQL must be running with TLS before database initialization.");
    }

    private static string ReadDeployWorkflow() {
        string workflowPath = ArchitectureTestPaths.FromRoot(".github", "workflows", "deploy.yml");
        return File.ReadAllText(workflowPath);
    }
}

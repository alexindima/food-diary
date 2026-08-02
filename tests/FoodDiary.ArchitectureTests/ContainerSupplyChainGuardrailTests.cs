namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ContainerSupplyChainGuardrailTests {
    private static readonly string[] ExpectedBuildIds = [
        "build_api",
        "build_telegram_bot",
        "build_initializer",
        "build_job_manager",
        "build_mail_relay",
        "build_mailrelay_initializer",
        "build_mail_inbox",
        "build_mailinbox_initializer",
        "build_client",
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
        Assert.Contains("uses: sigstore/cosign-installer@v3", workflow, StringComparison.Ordinal);

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

    private static string ReadDeployWorkflow() {
        string workflowPath = ArchitectureTestPaths.FromRoot(".github", "workflows", "deploy.yml");
        return File.ReadAllText(workflowPath);
    }
}

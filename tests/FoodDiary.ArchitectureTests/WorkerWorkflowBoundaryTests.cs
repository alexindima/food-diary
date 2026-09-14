using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class WorkerWorkflowBoundaryTests {
    [Theory]
    [InlineData("Billing", "RenewDueSubscriptions", "BillingRenewalRunResult")]
    [InlineData("Billing", "ProcessBillingWebhookInbox", "BillingWebhookInboxRunResult")]
    [InlineData("Billing", "ReplayFailedPaddleNotifications", "PaddleNotificationRecoveryResult")]
    [InlineData("Ai", "ProcessNextFoodRecognition", "bool")]
    [InlineData("Admin", "SendBugAcknowledgements", "Unit")]
    public void WorkflowsWithIndependentCommits_DoNotEnableAutomaticUnitOfWorkSave(string module, string command, string response) {
        string path = ArchitectureTestPaths.FromRoot("Modules", module, "Contracts", "Commands", command, command + "Command.cs");
        RecordDeclarationSyntax declaration = Assert.Single(CSharpSyntaxTree.ParseText(File.ReadAllText(path))
            .GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>());

        Assert.NotNull(declaration.BaseList);
        Assert.Equal($"IRequest<{response}>", Assert.Single(declaration.BaseList.Types).Type.ToString());
    }

    [Theory]
    [InlineData("ProcessQueuedBillingWebhook", "Result")]
    [InlineData("CreateCheckoutSession", "Result<BillingCheckoutSessionModel>")]
    public void BillingWorkflow_WithExplicitCommit_DoesNotEnableAutomaticUnitOfWorkSave(string command, string response) {
        string path = ArchitectureTestPaths.FromRoot("Modules", "Billing", "Application", "Commands",
            command, command + "Command.cs");
        RecordDeclarationSyntax declaration = Assert.Single(CSharpSyntaxTree.ParseText(File.ReadAllText(path))
            .GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>());

        Assert.NotNull(declaration.BaseList);
        Assert.Equal($"IRequest<{response}>", Assert.Single(declaration.BaseList.Types).Type.ToString());
    }

    [Theory]
    [InlineData("BillingWebhookInboxJob", "ProcessBillingWebhookInboxCommand")]
    [InlineData("PaddleNotificationRecoveryJob", "ReplayFailedPaddleNotificationsCommand")]
    [InlineData("BillingRenewalJob", "RenewDueSubscriptionsCommand")]
    public void BillingSchedulerAdapters_DispatchConsumerCommands(string job, string command) {
        string path = ArchitectureTestPaths.FromRoot("FoodDiary.JobManager", "Services", job + ".cs");
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetCompilationUnitRoot();

        Assert.DoesNotContain(root.Usings, directive => directive.Name?.ToString().StartsWith(
            "FoodDiary.Modules.Billing.Application", StringComparison.Ordinal) is true);
        Assert.DoesNotContain(root.Usings, directive => directive.Name?.ToString().StartsWith(
            "FoodDiary.Modules.Billing.Infrastructure", StringComparison.Ordinal) is true);
        Assert.Contains(root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>(), creation =>
            string.Equals(creation.Type.ToString(), command, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Billing", "Application", "Services/BillingRenewalService.cs")]
    [InlineData("Billing", "Application", "Common/IBillingRenewalService.cs")]
    [InlineData("Ai", "Application", "Services/FoodRecognitionProcessor.cs")]
    [InlineData("Ai", "Application.Abstractions", "Common/IFoodRecognitionProcessor.cs")]
    [InlineData("Admin", "Application", "Services/BugAcknowledgementService.cs")]
    public void RetiredWorkerServices_DoNotReturn(string module, string project, string relativePath) {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("Modules", module, project, relativePath)));
    }
}

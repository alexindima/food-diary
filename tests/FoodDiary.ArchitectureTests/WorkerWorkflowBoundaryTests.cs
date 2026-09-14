using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class WorkerWorkflowBoundaryTests {
    [Theory]
    [InlineData("Billing", "RenewDueSubscriptions", "BillingRenewalRunResult")]
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
    [InlineData("Billing", "Application", "Services/BillingRenewalService.cs")]
    [InlineData("Billing", "Application", "Common/IBillingRenewalService.cs")]
    [InlineData("Ai", "Application", "Services/FoodRecognitionProcessor.cs")]
    [InlineData("Ai", "Application.Abstractions", "Common/IFoodRecognitionProcessor.cs")]
    [InlineData("Admin", "Application", "Services/BugAcknowledgementService.cs")]
    public void RetiredWorkerServices_DoNotReturn(string module, string project, string relativePath) {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("Modules", module, project, relativePath)));
    }
}

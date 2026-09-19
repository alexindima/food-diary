using System.Reflection;
using System.Xml.Linq;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Contracts.Commands.ExchangeAdminImpersonation;
using FoodDiary.Modules.Dietologist.Contracts.Commands.SendClientTaskReminders;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ConsumerTransactionBoundaryTests {
    [Fact]
    public void EveryPublicOwnerRequest_PreservesCallerTransaction() {
        string[] projects = [.. Directory.GetDirectories(ArchitectureTestPaths.FromRoot("Modules"))
            .SelectMany(module => new[] { "Contracts", "Service.Contracts" }
                .Select(layer => Path.Combine(module, layer)))
            .Where(Directory.Exists)
            .SelectMany(root => Directory.GetFiles(root, "*.csproj"))];
        Assert.NotEmpty(projects);
        Type[] requests = [.. projects.Select(project => {
            string assemblyName = XDocument.Load(project).Descendants("AssemblyName").SingleOrDefault()?.Value
                ?? Path.GetFileNameWithoutExtension(project);
            return Assembly.Load(assemblyName);
        }).SelectMany(assembly => assembly.GetExportedTypes()).Where(IsRequest)];
        Assert.NotEmpty(requests);
        // These public contracts are outer HTTP/job entrypoints, not nested owner requests.
        Type[] entrypoints = [typeof(ExchangeAdminImpersonationCommand), typeof(SendClientTaskRemindersCommand)];
        Assert.Equal(entrypoints.OrderBy(type => type.FullName, StringComparer.Ordinal),
            FindTransactionOwners(requests).OrderBy(type => type.FullName, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("Admin", typeof(ExchangeAdminImpersonationCommand))]
    [InlineData("Dietologist", typeof(SendClientTaskRemindersCommand))]
    public void TransactionOwningEntrypoints_AreNotConsumedByForeignApplications(string owner, Type request) {
        string[] violations = [.. ModuleSourceCatalog.ApplicationRoots
            .Where(module => !string.Equals(module.Key, owner, StringComparison.Ordinal))
            .SelectMany(module => SourceScanner.SourceFiles(module.Value))
            .Where(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot()
                .DescendantNodes().OfType<IdentifierNameSyntax>()
                .Any(identifier => string.Equals(identifier.Identifier.ValueText, request.Name, StringComparison.Ordinal)))];
        Assert.Empty(violations);
    }

    [Fact]
    public void Guard_DetectsNestedCommitIncludingInheritedMarkers() {
        Assert.Equal([typeof(InvalidAtomicRequest)], FindTransactionOwners([typeof(ValidOwnerRequest), typeof(InvalidAtomicRequest)]));
    }

    private static bool IsRequest(Type type) => type.GetInterfaces().Any(contract =>
        contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>));

    private static Type[] FindTransactionOwners(IEnumerable<Type> requests) =>
        [.. requests.Where(request => typeof(ITransactionalCommand).IsAssignableFrom(request))];

    [ExcludeFromCodeCoverage]
    private sealed record ValidOwnerRequest : IRequest<Unit>;
    [ExcludeFromCodeCoverage]
    private sealed record InvalidAtomicRequest : IRequest<Unit>, IAtomicCommand;
}

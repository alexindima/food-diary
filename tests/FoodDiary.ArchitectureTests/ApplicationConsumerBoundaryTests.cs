using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Xml.Linq;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ApplicationConsumerBoundaryTests {
    [Theory]
    [InlineData("Application")]
    [InlineData("Presentation")]
    public void Modules_NeverReferenceAnotherModulesApplicationImplementation(string layer) {
        string modules = ArchitectureTestPaths.FromRoot("Modules");
        string[] applicationProjects = [.. Directory.GetDirectories(modules)
            .SelectMany(module => Directory.GetFiles(Path.Combine(module, "Application"), "*.csproj"))];
        Dictionary<string, string> owners = applicationProjects.ToDictionary(Path.GetFullPath,
            project => Directory.GetParent(project)!.Parent!.Name, StringComparer.OrdinalIgnoreCase);
        var violations = new List<string>();
        string[] consumers = [.. Directory.GetDirectories(modules)
            .Select(module => Path.Combine(module, layer)).Where(Directory.Exists)
            .SelectMany(folder => Directory.GetFiles(folder, "*.csproj"))];
        foreach (string project in consumers) {
            string owner = Directory.GetParent(project)!.Parent!.Name;
            foreach (System.Xml.Linq.XElement reference in XDocument.Load(project).Descendants("ProjectReference")) {
                string target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project)!, reference.Attribute("Include")!.Value));
                if (owners.TryGetValue(target, out string? targetOwner) && !string.Equals(owner, targetOwner, StringComparison.Ordinal)) {
                    violations.Add($"{owner} -> {targetOwner}");
                }
            }
        }
        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationImplementations_DoNotAcquireForeignRepositoriesOrStores() {
        string modules = ArchitectureTestPaths.FromRoot("Modules");
        Dictionary<string, string> repositoryOwners = [];
        foreach (string module in Directory.GetDirectories(modules)) {
            string abstractions = Path.Combine(module, "Application", "Abstractions");
            if (!Directory.Exists(abstractions)) {
                continue;
            }
            foreach (string file in SourceScanner.SourceFiles(abstractions)) {
                foreach (InterfaceDeclarationSyntax declaration in CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot()
                             .DescendantNodes().OfType<InterfaceDeclarationSyntax>()) {
                    string name = declaration.Identifier.ValueText;
                    if (name.EndsWith("Repository", StringComparison.Ordinal) || name.EndsWith("Store", StringComparison.Ordinal)) {
                        repositoryOwners.Add(name, Path.GetFileName(module));
                    }
                }
            }
        }
        var violations = new List<string>();
        foreach (string module in Directory.GetDirectories(modules)) {
            string application = Path.Combine(module, "Application");
            string owner = Path.GetFileName(module);
            foreach (string file in SourceScanner.SourceFiles(application)
                         .Where(file => !Path.GetRelativePath(application, file).StartsWith($"Abstractions{Path.DirectorySeparatorChar}", StringComparison.Ordinal))) {
                foreach (IdentifierNameSyntax identifier in CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot()
                             .DescendantNodes().OfType<IdentifierNameSyntax>()) {
                    if (repositoryOwners.TryGetValue(identifier.Identifier.ValueText, out string? targetOwner) &&
                        !string.Equals(owner, targetOwner, StringComparison.Ordinal)) {
                        violations.Add($"{Path.GetRelativePath(modules, file)} acquires {targetOwner}'s {identifier.Identifier.ValueText}");
                    }
                }
            }
        }
        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("Meals")]
    [InlineData("Favorites")]
    public void ScalarContracts_DoNotExposeAggregateAssemblies(string module) {
        string path = $"Modules/{module}/Domain.Contracts/FoodDiary.Modules.{module}.Domain.Contracts.csproj";
        Assert.Equal(["FoodDiary.Domain.Primitives"], ProjectReferenceReader.ReadProjectReferences(path));
        Assert.DoesNotContain($"FoodDiary.Modules.{module}.Domain", ProjectReferenceReader.ReadProjectReferences(
            $"Modules/{module}/Contracts/FoodDiary.Modules.{module}.Contracts.csproj"), StringComparer.Ordinal);
        string root = ArchitectureTestPaths.FromRoot($"Modules/{module}/Domain.Contracts");
        BaseTypeDeclarationSyntax[] types = [.. SourceScanner.SourceFiles(root)
            .SelectMany(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())];
        Assert.Equal(string.Equals(module, "Meals", StringComparison.Ordinal) ? 9 : 3, types.Length);
        Assert.All(types, type => Assert.True(type is EnumDeclarationSyntax ||
            (type is RecordDeclarationSyntax record && record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword))));
    }

    [Fact]
    public void EveryOutboxStream_FencesEfWritesByClaimOwner() {
        using var context = new FoodDiaryDbContext(new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql("Host=localhost;Database=architecture;Username=test;Password=test").Options);
        Microsoft.EntityFrameworkCore.Metadata.IEntityType[] streams = [.. context.Model.GetEntityTypes()
            .Where(entity => typeof(IOutboxMessage).IsAssignableFrom(entity.ClrType))];
        Assert.Equal(4, streams.Length);
        Assert.All(streams, entity => Assert.True(entity.FindProperty(nameof(IOutboxMessage.LockedBy))!.IsConcurrencyToken));
        Assert.True(context.Model.FindEntityType(typeof(FoodDiary.Infrastructure.Persistence.Achievements.AchievementEvaluationOutboxMessage))!
            .FindProperty("Revision")!.IsConcurrencyToken);
        Assert.False(context.Database.HasPendingModelChanges());
    }
}

using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class PresentationContractBoundaryTests {
    [Fact]
    public void ModulePresentations_DoNotReferenceForeignControllerAssemblies() {
        foreach (string module in Directory.GetDirectories(ArchitectureTestPaths.FromRoot("Modules"))) {
            string presentation = Path.Combine(module, "Presentation");
            if (!Directory.Exists(presentation)) {
                continue;
            }

            foreach (string project in Directory.GetFiles(presentation, "*.csproj")) {
                string owner = new DirectoryInfo(module).Name;
                string[] references = ProjectReferenceReader.ReadProjectReferences(
                    Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, project));
                Assert.DoesNotContain(references, reference =>
                    reference.StartsWith("FoodDiary.Modules.", StringComparison.Ordinal)
                    && reference.EndsWith(".Presentation", StringComparison.Ordinal)
                    && !string.Equals(reference, $"FoodDiary.Modules.{owner}.Presentation", StringComparison.Ordinal));
            }
        }
    }

    [Theory]
    [InlineData("Presentation.Contracts")]
    [InlineData("Presentation.Mappings")]
    public void ReusablePresentationLayers_HaveNoRuntimeOrImplementationDependencies(string layer) {
        var entityNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (string module in Directory.GetDirectories(ArchitectureTestPaths.FromRoot("Modules"))) {
            string entities = Path.Combine(module, "Domain", "Entities");
            if (!Directory.Exists(entities)) {
                continue;
            }

            foreach (string path in SourceScanner.SourceFiles(entities)) {
                entityNames.UnionWith(CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot()
                    .DescendantNodes().OfType<TypeDeclarationSyntax>()
                    .Select(type => type.Identifier.ValueText));
            }
        }

        string[] directories = [.. Directory.GetDirectories(ArchitectureTestPaths.FromRoot("Modules"))
            .Select(module => Path.Combine(module, layer)).Where(Directory.Exists)];
        Assert.NotEmpty(directories);
        foreach (string directory in directories) {
            string project = Assert.Single(Directory.GetFiles(directory, "*.csproj"));
            var document = XDocument.Load(project);
            Assert.Empty(document.Descendants("PackageReference"));
            Assert.Empty(document.Descendants("FrameworkReference"));
            string[] references = ProjectReferenceReader.ReadProjectReferences(
                Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, project));
            Assert.All(references, reference => {
                bool allowed = reference.EndsWith(".Presentation.Contracts", StringComparison.Ordinal)
                    || (string.Equals(layer, "Presentation.Mappings", StringComparison.Ordinal)
                        && (reference.EndsWith(".Contracts", StringComparison.Ordinal)
                            || reference.EndsWith(".Application.Abstractions", StringComparison.Ordinal)
                            || reference.EndsWith(".Presentation.Mappings", StringComparison.Ordinal)));
                Assert.True(allowed, $"{project} references implementation/runtime assembly {reference}");
            });

            foreach (string path in SourceScanner.SourceFiles(directory)) {
                SyntaxNode syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
                Assert.DoesNotContain(syntax.DescendantNodes().OfType<SimpleNameSyntax>()
                    .Where(name => name.Parent is not MemberAccessExpressionSyntax access || access.Name != name)
                    .Where(name => name.Parent is not MemberBindingExpressionSyntax),
                    name => entityNames.Contains(name.Identifier.ValueText));
                if (string.Equals(layer, "Presentation.Contracts", StringComparison.Ordinal)) {
                    Assert.Empty(syntax.DescendantNodes().OfType<MethodDeclarationSyntax>());
                    Assert.Empty(syntax.DescendantNodes().OfType<ExtensionBlockDeclarationSyntax>());
                } else {
                    Assert.All(syntax.DescendantNodes().OfType<ClassDeclarationSyntax>(), declaration =>
                        Assert.Contains(declaration.Modifiers, modifier => modifier.IsKind(SyntaxKind.StaticKeyword)));
                    Assert.Empty(syntax.DescendantNodes().OfType<AwaitExpressionSyntax>());
                }
            }
        }
    }
}

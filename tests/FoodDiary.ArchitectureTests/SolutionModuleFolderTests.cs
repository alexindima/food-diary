using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class SolutionModuleFolderTests {
    [Fact]
    public void ModuleProjects_AreNestedUnderTheirOwningModuleSolutionFolder() {
        var solution = XDocument.Load(ArchitectureTestPaths.FromRoot("FoodDiary.slnx"));
        var misplaced = new List<string>();

        foreach (XElement folder in solution.Root!.Elements("Folder")) {
            string folderName = (string?)folder.Attribute("Name") ?? string.Empty;
            foreach (XElement project in folder.Elements("Project")) {
                string path = (string?)project.Attribute("Path") ?? string.Empty;
                string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length < 2 || !string.Equals(segments[0], "Modules", StringComparison.Ordinal)) {
                    continue;
                }

                string expectedPrefix = $"/Modules/{segments[1]}/";
                if (!folderName.StartsWith(expectedPrefix, StringComparison.Ordinal)) {
                    misplaced.Add($"{path} is nested under {folderName}; expected {expectedPrefix}");
                }
            }
        }

        Assert.Empty(misplaced);
    }
}

using System.Reflection;
using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
internal static class ModuleDomainAssemblyCatalog {
    public static string[] ReadProjectPaths() =>
        [.. Directory.GetDirectories(ArchitectureTestPaths.FromRoot("Modules"))
            .Select(module => Path.Combine(module, "Domain"))
            .Where(Directory.Exists)
            .SelectMany(domain => Directory.GetFiles(domain, "*.csproj"))
            .Order(StringComparer.Ordinal)];

    public static Assembly[] LoadAssemblies(Func<AssemblyName, Assembly>? loadAssembly = null) {
        string[] projects = ReadProjectPaths();
        if (projects.Length == 0) {
            throw new InvalidOperationException("No module Domain projects were found.");
        }

        loadAssembly ??= Assembly.Load;
        return [.. projects.Select(project => {
            var document = XDocument.Load(project);
            string name = document.Descendants("AssemblyName").SingleOrDefault()?.Value
                          ?? Path.GetFileNameWithoutExtension(project);
            return loadAssembly(new AssemblyName(name));
        })];
    }
}

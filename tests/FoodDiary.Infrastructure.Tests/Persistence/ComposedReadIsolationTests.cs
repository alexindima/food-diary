using System.Reflection;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.ReadModel.Composition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class ComposedReadIsolationTests {
    [Fact]
    public void CrossModuleForeignKeys_DoNotExposeObjectNavigations() {
        using FoodDiaryDbContext context = CreateContext();
        IForeignKey[] relationships = [.. context.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys())
            .Where(key => !string.Equals(GetOwner(key.DeclaringEntityType.ClrType),
                GetOwner(key.PrincipalEntityType.ClrType), StringComparison.Ordinal))];
        Assert.NotEmpty(relationships);
        Assert.All(relationships, key => {
            Assert.Null(key.DependentToPrincipal);
            Assert.Null(key.PrincipalToDependent);
        });
    }

    [Fact]
    public void ComposedReaderResults_DoNotExposeMappedEntitiesEvenInsideDtos() {
        using FoodDiaryDbContext context = CreateContext();
        HashSet<Type> mappedTypes = [.. context.Model.GetEntityTypes().Select(entity => entity.ClrType)];
        MethodInfo[] methods = [.. typeof(ReadModelCompositionRegistration).Assembly.GetExportedTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => !method.IsSpecialName)];
        Assert.NotEmpty(methods);
        foreach (MethodInfo method in methods) {
            AssertResultContainsNoEntity(method.ReturnType, mappedTypes, [], $"{method.DeclaringType!.Name}.{method.Name}");
        }
    }

    private static void AssertResultContainsNoEntity(Type type, HashSet<Type> mappedTypes, HashSet<Type> visited, string path) {
        if (!visited.Add(type)) { return; }
        Assert.False(mappedTypes.Contains(type), $"{path} exposes mapped entity {type.FullName}.");
        if (type.HasElementType) {
            AssertResultContainsNoEntity(type.GetElementType()!, mappedTypes, visited, path);
        }
        foreach (Type argument in type.GetGenericArguments()) {
            AssertResultContainsNoEntity(argument, mappedTypes, visited, path);
        }
        if (type.Assembly.GetName().Name?.StartsWith("FoodDiary.", StringComparison.Ordinal) != true) { return; }
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            AssertResultContainsNoEntity(property.PropertyType, mappedTypes, visited, $"{path}.{property.Name}");
        }
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) {
            AssertResultContainsNoEntity(field.FieldType, mappedTypes, visited, $"{path}.{field.Name}");
        }
    }

    private static FoodDiaryDbContext CreateContext() => new(new DbContextOptionsBuilder<FoodDiaryDbContext>()
        .UseNpgsql("Host=localhost;Database=composition_model;Username=test")
        .Options);

    private static string GetOwner(Type type) {
        string name = type.Assembly.GetName().Name!;
        return name.StartsWith("FoodDiary.Modules.", StringComparison.Ordinal) ? name.Split('.')[2] : name;
    }
}

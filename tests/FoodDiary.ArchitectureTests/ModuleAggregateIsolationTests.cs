using System.Reflection;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ModuleAggregateIsolationTests {
    [Fact]
    public void DomainState_DoesNotContainForeignEntitiesEvenInCollections() {
        string[] violations = [.. ModuleDomainAssemblyCatalog.LoadAssemblies().SelectMany(assembly => assembly.GetTypes())
            .SelectMany(type => type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .SelectMany(field => TypeClosure(field.FieldType)
                    .Where(target => IsEntity(target) && target.Assembly != type.Assembly)
                    .Select(target => $"{type.FullName}.{field.Name} -> {target.FullName}")))
            .Order(StringComparer.Ordinal)];
        Assert.Empty(violations);
    }

    [Fact]
    public void EfNavigations_DoNotTraverseModuleOwners() {
        using FoodDiaryDbContext context = CreateContext();
        string[] violations = [.. context.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetNavigations())
            .Where(navigation => !string.Equals(ModuleOwner(navigation.DeclaringEntityType.ClrType), ModuleOwner(navigation.TargetEntityType.ClrType), StringComparison.Ordinal))
            .Select(navigation => $"{navigation.DeclaringEntityType.ClrType.Name}.{navigation.Name} -> {navigation.TargetEntityType.ClrType.Name}")
            .Order(StringComparer.Ordinal)];
        Assert.Empty(violations);
    }

    [Fact]
    public void ScalarBoundaryMappings_RequireNoRelationalMigration() {
        using FoodDiaryDbContext context = CreateContext();
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public void ImageForeignKeys_NeverNullForeignReferencesDuringDeletion() {
        using FoodDiaryDbContext context = CreateContext();
        Microsoft.EntityFrameworkCore.Metadata.IForeignKey[] imageLinks = [.. context.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys())
            .Where(key => string.Equals(key.PrincipalEntityType.ClrType.Name, "ImageAsset", StringComparison.Ordinal))];
        Assert.Equal(6, imageLinks.Length);
        Assert.All(imageLinks, key => Assert.Equal(DeleteBehavior.ClientNoAction, key.DeleteBehavior));
    }

    [Fact]
    public void LegacyImagesContracts_ContainsOnlyTheDomainIdentifier() =>
        Assert.Equal([typeof(ImageAssetId)], typeof(ImageAssetId).Assembly.GetExportedTypes());

    [Fact]
    public void ImagesServiceContracts_ExposeOnlyImmutableReads() {
        Assembly assembly = typeof(FoodDiary.Application.Abstractions.Images.Models.ImageAssetReadModel).Assembly;
        Assert.Equal(
            new[] {
                typeof(FoodDiary.Application.Abstractions.Images.Common.IImageAssetAccessService),
                typeof(FoodDiary.Application.Abstractions.Images.Models.ImageAssetReadModel),
                typeof(FoodDiary.Application.Images.Common.ImageAssetIdParser),
                typeof(FoodDiary.Application.Images.Common.ImageAssetResolution),
                typeof(FoodDiary.Application.Images.Common.ImageAssetResolver),
            }.OrderBy(type => type.FullName, StringComparer.Ordinal),
            assembly.GetExportedTypes().OrderBy(type => type.FullName, StringComparer.Ordinal));
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference =>
            reference.Name!.EndsWith(".Domain", StringComparison.Ordinal) ||
            reference.Name.EndsWith(".Application.Abstractions", StringComparison.Ordinal));
        Assert.All(assembly.GetExportedTypes().SelectMany(type => type.GetMethods()), method => {
            Assert.DoesNotContain(TypeClosure(method.ReturnType), IsEntity);
            Assert.All(method.GetParameters(), parameter => Assert.DoesNotContain(TypeClosure(parameter.ParameterType), IsEntity));
        });
    }

    private static string ModuleOwner(Type type) {
        string assembly = type.Assembly.GetName().Name!;
        const string prefix = "FoodDiary.Modules.";
        return assembly.StartsWith(prefix, StringComparison.Ordinal) ? assembly[prefix.Length..].Split('.')[0] : assembly;
    }

    private static FoodDiaryDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql("Host=localhost;Database=food_diary_architecture;Username=test;Password=test").Options);

    private static IEnumerable<Type> TypeClosure(Type type) {
        yield return type;
        if (type.HasElementType) {
            foreach (Type nested in TypeClosure(type.GetElementType()!)) { yield return nested; }
        }
        foreach (Type argument in type.GetGenericArguments()) {
            foreach (Type nested in TypeClosure(argument)) { yield return nested; }
        }
    }

    private static bool IsEntity(Type type) {
        for (Type? candidate = type; candidate is not null; candidate = candidate.BaseType) {
            if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(Entity<>)) { return true; }
        }
        return false;
    }
}

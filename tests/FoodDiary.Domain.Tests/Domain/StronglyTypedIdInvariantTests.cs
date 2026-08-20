using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;
using System.Reflection;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class StronglyTypedIdInvariantTests {
    [Fact]
    public void ClientTaskId_Empty_ReturnsEmptyGuid() {
        Assert.Equal(Guid.Empty, ClientTaskId.Empty.Value);
    }

    public static IEnumerable<object[]> StronglyTypedGuidIdTypes() {
        return typeof(UserId).Assembly
            .GetTypes()
            .Where(static type =>
                type is { IsValueType: true, IsAbstract: false } &&
                string.Equals(type.Namespace, "FoodDiary.Domain.ValueObjects.Ids", StringComparison.Ordinal) &&
                type.Name.EndsWith("Id", StringComparison.Ordinal) &&
                type.GetProperty("Value")?.PropertyType == typeof(Guid))
            .OrderBy(static type => type.Name, StringComparer.Ordinal)
            .Select(static type => new object[] { type });
    }

    [Theory]
    [MemberData(nameof(StronglyTypedGuidIdTypes))]
    public void StronglyTypedGuidId_ImplementsEntityIdContract(Type idType) {
        Assert.True(
            typeof(IEntityId<Guid>).IsAssignableFrom(idType),
            $"{idType.FullName} must implement {typeof(IEntityId<Guid>).FullName}.");
    }

    [Theory]
    [MemberData(nameof(StronglyTypedGuidIdTypes))]
    public void StronglyTypedGuidId_Empty_ReturnsEmptyGuid(Type idType) {
        object empty = idType.GetProperty("Empty", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        var value = (Guid)idType.GetProperty("Value")!.GetValue(empty)!;

        Assert.Equal(Guid.Empty, value);
    }

    [Theory]
    [MemberData(nameof(StronglyTypedGuidIdTypes))]
    public void StronglyTypedGuidId_New_ReturnsNonEmptyGuid(Type idType) {
        object id = idType.GetMethod("New", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [])!;
        var value = (Guid)idType.GetProperty("Value")!.GetValue(id)!;

        Assert.NotEqual(Guid.Empty, value);
    }

    [Theory]
    [MemberData(nameof(StronglyTypedGuidIdTypes))]
    public void StronglyTypedGuidId_Conversions_RoundTripGuid(Type idType) {
        var guid = Guid.NewGuid();
        object id = idType.GetMethod("op_Explicit", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [guid])!;
        var converted = (Guid)idType.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [id])!;

        Assert.Equal(guid, converted);
    }

    [Theory]
    [MemberData(nameof(StronglyTypedGuidIdTypes))]
    public void StronglyTypedGuidId_ToString_IncludesGuid(Type idType) {
        var guid = Guid.NewGuid();
        object id = idType.GetMethod("op_Explicit", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [guid])!;

        Assert.Contains(guid.ToString(), id.ToString(), StringComparison.Ordinal);
    }
}

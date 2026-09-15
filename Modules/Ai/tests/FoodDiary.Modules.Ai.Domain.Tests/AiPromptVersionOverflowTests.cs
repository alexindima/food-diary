using FoodDiary.Modules.Ai.Domain.Entities;
using System.Reflection;

namespace FoodDiary.Modules.Ai.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class AiPromptVersionOverflowTests {

    private static void SetProperty<T>(T instance, string propertyName, object value) {
        PropertyInfo property = typeof(T).GetProperty(propertyName) ?? throw new InvalidOperationException($"Property {propertyName} was not found.");
        property.SetValue(instance, value);
    }

    [Fact]
    public void VersionedEntities_RejectOverflowBeforeMutation() {
        var template = AiPromptTemplate.Create("key", "en", "old text");
        SetProperty(template, nameof(AiPromptTemplate.Version), int.MaxValue);
        Assert.Throws<InvalidOperationException>(() => template.Update("new text"));
        Assert.Equal("old text", template.PromptText);
        Assert.Equal(int.MaxValue, template.Version);
        Assert.Null(template.ModifiedOnUtc);
    }
}

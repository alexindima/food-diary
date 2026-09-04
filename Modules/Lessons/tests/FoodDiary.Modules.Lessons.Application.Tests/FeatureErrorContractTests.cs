using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void LessonErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Lessons.Application.Abstractions", typeof(LessonErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.Lessons.Common", typeof(LessonErrors).Namespace));
    }

    [Fact]
    public void LessonErrors_PreservesEveryPublicErrorContract() {
        AssertError(LessonErrors.NotFound(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "Lesson.NotFound", "Lesson with ID 12345678-1234-1234-1234-123456789abc was not found.", ErrorKind.NotFound);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}

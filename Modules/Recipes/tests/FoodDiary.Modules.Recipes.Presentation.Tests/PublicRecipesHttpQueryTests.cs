using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FoodDiary.Modules.Recipes.Presentation.Requests;

namespace FoodDiary.Modules.Recipes.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class PublicRecipesHttpQueryTests {
    [Theory]
    [InlineData("newest", true)]
    [InlineData("oldest", true)]
    [InlineData("fastest", true)]
    [InlineData("slowest", true)]
    [InlineData("name", true)]
    [InlineData("name_desc", true)]
    [InlineData("unknown", false)]
    public void SortBy_HttpValidationAcceptsSupportedDirections(string sortBy, bool expected) {
        ParameterInfo parameter = typeof(PublicRecipesHttpQuery).GetConstructors()
            .Single(constructor => constructor.IsPublic).GetParameters()
            .Single(parameter => string.Equals(parameter.Name, nameof(PublicRecipesHttpQuery.SortBy), StringComparison.Ordinal));
        RegularExpressionAttribute validation = Assert.IsType<RegularExpressionAttribute>(
            parameter.GetCustomAttribute<RegularExpressionAttribute>());

        Assert.Equal(expected, validation.IsValid(sortBy));
    }
}

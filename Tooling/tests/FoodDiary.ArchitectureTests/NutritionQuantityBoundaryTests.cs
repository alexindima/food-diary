using System.Reflection;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.Nutrition;
using FoodDiary.Modules.Users.Domain.ValueObjects;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class NutritionQuantityBoundaryTests {
    [Theory]
    [InlineData(typeof(Meal), "AddProduct", typeof(ProductUnitQuantity))]
    [InlineData(typeof(Meal), "AddRecipe", typeof(RecipeServingQuantity))]
    [InlineData(typeof(RecipeStep), "AddProductIngredient", typeof(ProductUnitQuantity))]
    [InlineData(typeof(RecipeStep), "AddNestedRecipeIngredient", typeof(RecipeServingQuantity))]
    [InlineData(typeof(MealItem), "UpdateProductQuantity", typeof(ProductUnitQuantity))]
    [InlineData(typeof(MealItem), "UpdateRecipeServings", typeof(RecipeServingQuantity))]
    [InlineData(typeof(RecipeIngredient), "UpdateProductQuantity", typeof(ProductUnitQuantity))]
    [InlineData(typeof(RecipeIngredient), "UpdateRecipeServings", typeof(RecipeServingQuantity))]
    public void QuantityMutation_HasOneTypedEntryPointWithoutAScalarOverload(Type owner, string methodName, Type quantityType) {
        MethodInfo method = Assert.Single(owner.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            candidate => string.Equals(candidate.Name, methodName, StringComparison.Ordinal));

        Assert.Equal(quantityType, method.GetParameters()[^1].ParameterType);
    }

    [Theory]
    [InlineData(typeof(MealItem))]
    [InlineData(typeof(RecipeIngredient))]
    public void ItemMutation_DoesNotExposeAnUntypedAmountWriter(Type owner) {
        MethodInfo[] methods = owner.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.NotEmpty(methods);
        Assert.DoesNotContain(methods, method => method.GetParameters().Any(parameter =>
            parameter.ParameterType == typeof(double) && parameter.Name is "amount" or "servings" or "quantity"));
        Assert.Null(owner.GetMethod("UpdateAmount", BindingFlags.Public | BindingFlags.Instance));
    }

    [Theory]
    [InlineData(typeof(ProductUnitQuantity))]
    [InlineData(typeof(RecipeServingQuantity))]
    public void PositiveQuantity_CannotBeDefaultConstructedOrImplicitlyConverted(Type quantityType) {
        Assert.Multiple(() => {
            Assert.False(quantityType.IsValueType);
            Assert.True(quantityType.IsSealed);
            Assert.Empty(quantityType.GetConstructors());
            Assert.DoesNotContain(quantityType.GetMethods(BindingFlags.Public | BindingFlags.Static),
                method => string.Equals(method.Name, "op_Implicit", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void NutritionInput_RequiresExplicitSourceSelectionBeforeCalculation() {
        Assert.Empty(typeof(RecipeNutritionIngredient).GetConstructors());
        Assert.DoesNotContain(typeof(RecipeNutritionIngredient).GetProperties(), property =>
            property.SetMethod?.IsPublic == true);
    }

    [Fact]
    public void PersonalInfoPatch_KeepsKilogramsAndCentimetersAsDistinctValidatedValues() {
        Assert.Multiple(() => {
            Assert.Equal(typeof(ProfileWeightKg), typeof(UserPersonalInfoUpdate).GetProperty(nameof(UserPersonalInfoUpdate.WeightKg))!.PropertyType);
            Assert.Equal(typeof(ProfileHeightCm), typeof(UserPersonalInfoUpdate).GetProperty(nameof(UserPersonalInfoUpdate.HeightCm))!.PropertyType);
            Assert.False(typeof(ProfileWeightKg).IsValueType);
            Assert.False(typeof(ProfileHeightCm).IsValueType);
            Assert.Empty(typeof(ProfileWeightKg).GetConstructors());
            Assert.Empty(typeof(ProfileHeightCm).GetConstructors());
        });
    }
}

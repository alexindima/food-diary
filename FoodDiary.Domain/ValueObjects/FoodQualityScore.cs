using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct FoodQualityScore(int Score, FoodQualityGrade Grade) {
    public static FoodQualityScore Calculate(
        double caloriesPerBase,
        double proteinsPerBase,
        double fatsPerBase,
        double carbsPerBase,
        double fiberPerBase,
        double alcoholPerBase,
        ProductType productType = ProductType.Unknown) {
        if (caloriesPerBase <= 0) {
            return new FoodQualityScore(50, FoodQualityGrade.Yellow);
        }

        double score = 0.0;

        // Protein density: g protein per 100 kcal (max 25 points)
        double proteinPer100Kcal = proteinsPerBase / caloriesPerBase * 100.0;
        score += Math.Min(proteinPer100Kcal * 2.5, 25.0);

        // Fiber density: g fiber per 100 kcal (max 20 points)
        double fiberPer100Kcal = fiberPerBase / caloriesPerBase * 100.0;
        score += Math.Min(fiberPer100Kcal * 5.0, 20.0);

        // Calorie density penalty: kcal per 100g base (max 25 points)
        // < 100 kcal/100g = 25 pts, 100-250 = 15-25, 250-400 = 5-15, > 400 = 0-5
        double calDensityScore = caloriesPerBase switch {
            <= 100 => 25.0,
            <= 250 => 25.0 - ((caloriesPerBase - 100.0) / 150.0 * 10.0),
            <= 400 => 15.0 - ((caloriesPerBase - 250.0) / 150.0 * 10.0),
            _ => Math.Max(0, 5.0 - ((caloriesPerBase - 400.0) / 200.0 * 5.0)),
        };
        score += calDensityScore;

        // Macro balance: protein ratio of total macros (max 15 points)
        double totalMacroGrams = proteinsPerBase + fatsPerBase + carbsPerBase;
        if (totalMacroGrams > 0) {
            double proteinRatio = proteinsPerBase / totalMacroGrams;
            // Ideal protein ratio ~0.3, good range 0.2-0.5
            score += proteinRatio switch {
                >= 0.2 and <= 0.5 => 15.0,
                >= 0.1 => 15.0 * (proteinRatio / 0.2),
                _ => 0,
            };
        }

        // Alcohol penalty (max -10 points)
        if (alcoholPerBase > 0 && caloriesPerBase > 0) {
            double alcoholCalories = alcoholPerBase * 7.0;
            double alcoholRatio = alcoholCalories / caloriesPerBase;
            score -= Math.Min(alcoholRatio * 15.0, 10.0);
        }

        // Product type modifier (max +/- 15 points)
        score += GetProductTypeModifier(productType);

        int finalScore = (int)Math.Round(Math.Clamp(score, 0, 100), MidpointRounding.ToEven);
        FoodQualityGrade grade = finalScore switch {
            >= 67 => FoodQualityGrade.Green,
            >= 34 => FoodQualityGrade.Yellow,
            _ => FoodQualityGrade.Red,
        };

        return new FoodQualityScore(finalScore, grade);
    }

    private static double GetProductTypeModifier(ProductType productType) {
        return productType switch {
            ProductType.Vegetable => 15.0,
            ProductType.Fruit => 10.0,
            ProductType.Seafood => 8.0,
            ProductType.Meat => 5.0,
            ProductType.Dairy => 3.0,
            ProductType.Grain => 2.0,
            ProductType.Cheese => -2.0,
            ProductType.Beverage => -5.0,
            ProductType.Dessert => -10.0,
            _ => 0,
        };
    }
}

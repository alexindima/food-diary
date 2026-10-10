import {
    scaleNutrientInput,
    calculateCaloriesFromMacros,
} from "@candidate/app/shared/lib/nutrition-form.utils";

const total: number = scaleNutrientInput(333.33, 1 / 2);
scaleNutrientInput(total, 2);
calculateCaloriesFromMacros(10, 5, 20, null);
// @ts-expect-error Text is not a numeric nutrient observation.
scaleNutrientInput("333.33", 2);

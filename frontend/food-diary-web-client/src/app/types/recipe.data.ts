import { NutrientChartData } from "./charts.data";
import { Product } from "./product.data";

export interface Recipe {
    id?: number; // ID рецепта (опционально, если рецепт еще не сохранен)
    name: string; // Название рецепта
    description: string | null; // Описание рецепта
    prepTime: number | null; // Время подготовки (в минутах)
    cookTime: number | null; // Время готовки (в минутах)
    servings: number; // Количество порций
    steps: RecipeStep[]; // Массив шагов рецепта
    totalCalories?: number; // Общая калорийность (опционально, для отображения)
    nutrientChartData?: NutrientChartData; // Данные для графика нутриентов (опционально)
}

export interface RecipeStep {
    description: string; // Описание шага
    ingredients: RecipeIngredient[]; // Массив ингредиентов для шага
}

export interface RecipeIngredient {
    food: Product | null; // Продукт, выбранный из списка
    amount: number | null; // Количество продукта
}

export interface RecipeDto {
    name: string; // Название рецепта
    description?: string; // Описание рецепта (опционально)
    prepTime: number; // Время на подготовку в минутах
    cookTime: number; // Время на готовку в минутах
    servings: number; // Количество порций
    steps: RecipeStepDto[]; // Массив шагов рецепта
}

export interface RecipeStepDto {
    description: string; // Описание шага
    ingredients: RecipeIngredientDto[]; // Ингредиенты для шага
}

export interface RecipeIngredientDto {
    foodId: string; // ID продукта (Guid)
    amount: number; // Количество продукта
}

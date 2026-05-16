import type { NutrientBar } from '../../../../components/shared/dashboard-summary-card/dashboard-summary-card.types';
import type { MealPreviewEntry } from '../../../../components/shared/meals-preview/meals-preview.types';
import type { QuickMealItem } from '../../../meals/lib/quick/quick-meal.service';
import type { Meal } from '../../../meals/models/meal.data';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../products/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../recipes/models/recipe.data';

export type LandingPreviewContent = {
    heroSummaryCard: {
        dailyGoal: number;
        dailyConsumed: number;
        weeklyConsumed: number;
        weeklyGoal: number;
        nutrientBars: NutrientBar[];
    };
    guestMealEntries: MealPreviewEntry[];
    previewProducts: Product[];
    previewRecipes: Recipe[];
    previewQuickItems: QuickMealItem[];
};

type TranslateFn = (key: string) => string;

export function buildLandingPreviewContent(translate: TranslateFn, now: Date = new Date()): LandingPreviewContent {
    const previewProducts = buildPreviewProducts(translate, now);
    const previewRecipes = buildPreviewRecipes(translate, now);

    return {
        heroSummaryCard: buildHeroSummaryCard(),
        guestMealEntries: buildGuestMeals(translate, now),
        previewProducts,
        previewRecipes,
        previewQuickItems: buildPreviewQuickItems(previewProducts, previewRecipes),
    };
}

function buildHeroSummaryCard(): LandingPreviewContent['heroSummaryCard'] {
    return {
        dailyGoal: 2000,
        dailyConsumed: 1450,
        weeklyConsumed: 8200,
        weeklyGoal: 14000,
        nutrientBars: [
            {
                id: 'protein',
                label: 'Protein',
                labelKey: 'GENERAL.NUTRIENTS.PROTEIN',
                current: 110,
                target: 140,
                unit: 'g',
                unitKey: 'GENERAL.UNITS.G',
                colorStart: 'var(--fd-gradient-brand-start)',
                colorEnd: 'var(--fd-color-primary-600)',
            },
            {
                id: 'carbs',
                label: 'Carbs',
                labelKey: 'GENERAL.NUTRIENTS.CARB',
                current: 180,
                target: 250,
                unit: 'g',
                unitKey: 'GENERAL.UNITS.G',
                colorStart: 'var(--fd-color-teal-500)',
                colorEnd: 'var(--fd-color-sky-500)',
            },
            {
                id: 'fats',
                label: 'Fats',
                labelKey: 'GENERAL.NUTRIENTS.FAT',
                current: 45,
                target: 70,
                unit: 'g',
                unitKey: 'GENERAL.UNITS.G',
                colorStart: 'var(--fd-color-yellow-300)',
                colorEnd: 'var(--fd-color-orange-500)',
            },
            {
                id: 'fiber',
                label: 'Fiber',
                labelKey: 'SHARED.NUTRIENTS_SUMMARY.FIBER',
                current: 18,
                target: 30,
                unit: 'g',
                unitKey: 'GENERAL.UNITS.G',
                colorStart: 'var(--fd-color-rose-500)',
                colorEnd: 'var(--fd-color-rose-500)',
            },
        ],
    };
}

function buildGuestMeals(translate: TranslateFn, now: Date): MealPreviewEntry[] {
    const comment = translate('LANDING_PREVIEW_TOUR.PREVIEW_DATA.MEAL_COMMENT');
    const lunch: Meal = {
        id: 'guest-lunch',
        date: now.toISOString(),
        mealType: 'LUNCH',
        totalCalories: 430,
        totalProteins: 24,
        totalFats: 12,
        totalCarbs: 52,
        totalFiber: 7,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        manualCalories: null,
        manualProteins: null,
        manualFats: null,
        manualCarbs: null,
        manualFiber: null,
        manualAlcohol: null,
        preMealSatietyLevel: null,
        postMealSatietyLevel: null,
        comment,
        imageUrl: 'assets/images/stubs/meals/lunch-soup-photo.webp',
        imageAssetId: null,
        items: [],
    };

    return [
        { slot: 'BREAKFAST', icon: 'wb_sunny', labelKey: 'MEAL_CARD.MEAL_TYPES.BREAKFAST', meal: null },
        { slot: 'LUNCH', icon: 'lunch_dining', labelKey: 'MEAL_CARD.MEAL_TYPES.LUNCH', meal: lunch },
        { slot: 'DINNER', icon: 'nights_stay', labelKey: 'MEAL_CARD.MEAL_TYPES.DINNER', meal: null },
    ];
}

function buildPreviewProducts(translate: TranslateFn, now: Date): Product[] {
    const yogurtName = translate('LANDING_PREVIEW_TOUR.PREVIEW_DATA.PRODUCTS.YOGURT.NAME');
    const yogurtCategory = translate('LANDING_PREVIEW_TOUR.PREVIEW_DATA.PRODUCTS.YOGURT.CATEGORY');
    const yogurtDescription = translate('LANDING_PREVIEW_TOUR.PREVIEW_DATA.PRODUCTS.YOGURT.DESCRIPTION');
    const granolaName = translate('LANDING_PREVIEW_TOUR.PREVIEW_DATA.PRODUCTS.GRANOLA.NAME');
    const granolaCategory = translate('LANDING_PREVIEW_TOUR.PREVIEW_DATA.PRODUCTS.GRANOLA.CATEGORY');
    const granolaDescription = translate('LANDING_PREVIEW_TOUR.PREVIEW_DATA.PRODUCTS.GRANOLA.DESCRIPTION');

    return [
        {
            id: 'preview-yogurt',
            name: yogurtName,
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 150,
            caloriesPerBase: 60,
            proteinsPerBase: 10,
            fatsPerBase: 2,
            carbsPerBase: 4,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            productType: ProductType.Dairy,
            brand: 'FarmFresh',
            barcode: null,
            category: yogurtCategory,
            description: yogurtDescription,
            comment: null,
            imageUrl: 'assets/images/stubs/products/greek-yogurt-photo.webp',
            imageAssetId: null,
            visibility: ProductVisibility.Public,
            usageCount: 0,
            createdAt: now,
            isOwnedByCurrentUser: true,
            qualityScore: 72,
            qualityGrade: 'green',
        },
        {
            id: 'preview-granola',
            name: granolaName,
            baseUnit: MeasurementUnit.G,
            baseAmount: 50,
            defaultPortionAmount: 60,
            caloriesPerBase: 210,
            proteinsPerBase: 6,
            fatsPerBase: 9,
            carbsPerBase: 28,
            fiberPerBase: 4,
            alcoholPerBase: 0,
            productType: ProductType.Grain,
            brand: 'Crunchy',
            barcode: null,
            category: granolaCategory,
            description: granolaDescription,
            comment: null,
            imageUrl: 'assets/images/stubs/products/granola-photo.webp',
            imageAssetId: null,
            visibility: ProductVisibility.Public,
            usageCount: 0,
            createdAt: now,
            isOwnedByCurrentUser: true,
            qualityScore: 45,
            qualityGrade: 'yellow',
        },
    ];
}

function buildPreviewRecipes(translate: TranslateFn, now: Date): Recipe[] {
    return [buildPreviewBowlRecipe(translate, now), buildPreviewSaladRecipe(translate, now)];
}

function buildPreviewBowlRecipe(translate: TranslateFn, now: Date): Recipe {
    return {
        id: 'preview-bowl',
        name: translate('LANDING_PREVIEW_TOUR.PREVIEW_DATA.RECIPES.BOWL.NAME'),
        description: translate('LANDING_PREVIEW_TOUR.PREVIEW_DATA.RECIPES.BOWL.DESCRIPTION'),
        category: translate('LANDING_PREVIEW_TOUR.PREVIEW_DATA.RECIPES.BOWL.CATEGORY'),
        imageUrl: 'assets/images/stubs/products/salmon-bowl-photo.webp',
        imageAssetId: null,
        prepTime: 10,
        cookTime: 10,
        servings: 2,
        visibility: RecipeVisibility.Public,
        usageCount: 0,
        createdAt: now.toISOString(),
        isOwnedByCurrentUser: true,
        qualityScore: 81,
        qualityGrade: 'green',
        totalCalories: 520,
        totalProteins: 32,
        totalFats: 18,
        totalCarbs: 55,
        totalFiber: 7,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        manualCalories: null,
        manualProteins: null,
        manualFats: null,
        manualCarbs: null,
        manualFiber: null,
        manualAlcohol: null,
        steps: [
            {
                id: 'step-1',
                stepNumber: 1,
                instruction: translate('LANDING_PREVIEW_TOUR.PREVIEW_DATA.RECIPES.BOWL.STEP'),
                ingredients: [],
            },
        ],
    };
}

function buildPreviewSaladRecipe(translate: TranslateFn, now: Date): Recipe {
    return {
        id: 'preview-salad',
        name: translate('LANDING_PREVIEW_TOUR.PREVIEW_DATA.RECIPES.SALAD.NAME'),
        description: translate('LANDING_PREVIEW_TOUR.PREVIEW_DATA.RECIPES.SALAD.DESCRIPTION'),
        category: translate('LANDING_PREVIEW_TOUR.PREVIEW_DATA.RECIPES.SALAD.CATEGORY'),
        imageUrl: 'assets/images/stubs/products/chicken-avocado-salad-photo.webp',
        imageAssetId: null,
        prepTime: 12,
        cookTime: 0,
        servings: 1,
        visibility: RecipeVisibility.Public,
        usageCount: 0,
        createdAt: now.toISOString(),
        isOwnedByCurrentUser: true,
        qualityScore: 76,
        qualityGrade: 'green',
        totalCalories: 340,
        totalProteins: 28,
        totalFats: 18,
        totalCarbs: 12,
        totalFiber: 6,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        manualCalories: null,
        manualProteins: null,
        manualFats: null,
        manualCarbs: null,
        manualFiber: null,
        manualAlcohol: null,
        steps: [
            {
                id: 'step-1',
                stepNumber: 1,
                instruction: translate('LANDING_PREVIEW_TOUR.PREVIEW_DATA.RECIPES.SALAD.STEP'),
                ingredients: [],
            },
        ],
    };
}

function buildPreviewQuickItems(products: Product[], recipes: Recipe[]): QuickMealItem[] {
    return [
        {
            key: `product-${products[0].id}`,
            type: 'product',
            product: products[0],
            amount: 150,
        },
        {
            key: `product-${products[1].id}`,
            type: 'product',
            product: products[1],
            amount: 60,
        },
        {
            key: `recipe-${recipes[0].id}`,
            type: 'recipe',
            recipe: recipes[0],
            amount: 1,
        },
    ];
}

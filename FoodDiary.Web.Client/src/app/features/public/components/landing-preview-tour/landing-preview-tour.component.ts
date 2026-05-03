import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

import {
    DashboardSummaryCardComponent,
    type NutrientBar,
} from '../../../../components/shared/dashboard-summary-card/dashboard-summary-card.component';
import { type MealPreviewEntry, MealsPreviewComponent } from '../../../../components/shared/meals-preview/meals-preview.component';
import { ProductCardComponent } from '../../../../components/shared/product-card/product-card.component';
import { RecipeCardComponent } from '../../../../components/shared/recipe-card/recipe-card.component';
import { AuthService } from '../../../../services/auth.service';
import { QuickConsumptionDrawerComponent } from '../../../meals/components/quick-consumption-drawer/quick-consumption-drawer.component';
import { type QuickMealItem, QuickMealService } from '../../../meals/lib/quick-meal.service';
import { type Meal } from '../../../meals/models/meal.data';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../products/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../recipes/models/recipe.data';

@Component({
    selector: 'fd-landing-preview-tour',
    standalone: true,
    imports: [
        TranslateModule,
        DashboardSummaryCardComponent,
        MealsPreviewComponent,
        ProductCardComponent,
        RecipeCardComponent,
        QuickConsumptionDrawerComponent,
    ],
    templateUrl: './landing-preview-tour.component.html',
    styleUrls: ['./landing-preview-tour.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingPreviewTourComponent {
    private readonly authService = inject(AuthService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly quickConsumptionService = inject(QuickMealService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    public isAuthenticated = this.authService.isAuthenticated;
    public readonly heroSummaryCard = {
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
        ] satisfies NutrientBar[],
    };
    public guestMealEntries: MealPreviewEntry[] = [];
    public previewProducts: Product[] = [];
    public previewRecipes: Recipe[] = [];
    public previewQuickItems: QuickMealItem[] = [];

    private readonly clearPreviewOnAuth = effect(() => {
        if (this.isAuthenticated()) {
            this.quickConsumptionService.exitPreview();
        }
    });

    public constructor() {
        this.refreshPreviewContent();
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.refreshPreviewContent();
        });
    }

    public addPreviewProduct(product: Product): void {
        this.quickConsumptionService.addProduct(product);
    }

    public addPreviewRecipe(recipe: Recipe): void {
        this.quickConsumptionService.addRecipe(recipe);
    }

    public async openAuthDialog(mode: 'login' | 'register'): Promise<void> {
        const { AuthDialogComponent } = await import('../../../auth/dialogs/auth-dialog/auth-dialog.component');

        this.fdDialogService.open(AuthDialogComponent, {
            preset: 'form',
            data: { mode },
        });
    }

    private buildGuestMeals(): MealPreviewEntry[] {
        const comment = this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.MEAL_COMMENT');
        const lunch: Meal = {
            id: 'guest-lunch',
            date: new Date().toISOString(),
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

    private buildPreviewProducts(): Product[] {
        const yogurtName = this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.PRODUCTS.YOGURT.NAME');
        const yogurtCategory = this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.PRODUCTS.YOGURT.CATEGORY');
        const yogurtDescription = this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.PRODUCTS.YOGURT.DESCRIPTION');
        const granolaName = this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.PRODUCTS.GRANOLA.NAME');
        const granolaCategory = this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.PRODUCTS.GRANOLA.CATEGORY');
        const granolaDescription = this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.PRODUCTS.GRANOLA.DESCRIPTION');

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
                createdAt: new Date(),
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
                createdAt: new Date(),
                isOwnedByCurrentUser: true,
                qualityScore: 45,
                qualityGrade: 'yellow',
            },
        ];
    }

    private buildPreviewRecipes(): Recipe[] {
        const bowlName = this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.RECIPES.BOWL.NAME');
        const bowlDescription = this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.RECIPES.BOWL.DESCRIPTION');
        const bowlCategory = this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.RECIPES.BOWL.CATEGORY');
        const bowlStep = this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.RECIPES.BOWL.STEP');
        const saladName = this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.RECIPES.SALAD.NAME');
        const saladDescription = this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.RECIPES.SALAD.DESCRIPTION');
        const saladCategory = this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.RECIPES.SALAD.CATEGORY');
        const saladStep = this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.RECIPES.SALAD.STEP');

        return [
            {
                id: 'preview-bowl',
                name: bowlName,
                description: bowlDescription,
                category: bowlCategory,
                imageUrl: 'assets/images/stubs/products/salmon-bowl-photo.webp',
                imageAssetId: null,
                prepTime: 10,
                cookTime: 10,
                servings: 2,
                visibility: RecipeVisibility.Public,
                usageCount: 0,
                createdAt: new Date().toISOString(),
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
                        instruction: bowlStep,
                        ingredients: [],
                    },
                ],
            },
            {
                id: 'preview-salad',
                name: saladName,
                description: saladDescription,
                category: saladCategory,
                imageUrl: 'assets/images/stubs/products/chicken-avocado-salad-photo.webp',
                imageAssetId: null,
                prepTime: 12,
                cookTime: 0,
                servings: 1,
                visibility: RecipeVisibility.Public,
                usageCount: 0,
                createdAt: new Date().toISOString(),
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
                        instruction: saladStep,
                        ingredients: [],
                    },
                ],
            },
        ];
    }

    private buildPreviewQuickItems(): QuickMealItem[] {
        const products = this.previewProducts;
        const recipes = this.previewRecipes;

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

    private refreshPreviewContent(): void {
        this.previewProducts = this.buildPreviewProducts();
        this.previewRecipes = this.buildPreviewRecipes();
        this.previewQuickItems = this.buildPreviewQuickItems();
        this.guestMealEntries = this.buildGuestMeals();

        if (this.isAuthenticated()) {
            return;
        }

        this.quickConsumptionService.setPreviewItems(this.previewQuickItems);
    }
}

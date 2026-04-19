import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ChartConfiguration, ChartOptions } from 'chart.js';

import { AuthDialogComponent } from '../../../auth/dialogs/auth-dialog/auth-dialog.component';
import { Meal } from '../../../meals/models/meal.data';
import { QuickMealItem, QuickMealService } from '../../../meals/lib/quick-meal.service';
import { QuickConsumptionDrawerComponent } from '../../../meals/components/quick-consumption-drawer/quick-consumption-drawer.component';
import { Product, MeasurementUnit, ProductType, ProductVisibility } from '../../../products/models/product.data';
import { Recipe, RecipeVisibility } from '../../../recipes/models/recipe.data';
import { MealsPreviewComponent, MealPreviewEntry } from '../../../../components/shared/meals-preview/meals-preview.component';
import {
    DashboardSummaryCardComponent,
    NutrientBar,
} from '../../../../components/shared/dashboard-summary-card/dashboard-summary-card.component';
import { SummaryMetrics } from '../../../../components/shared/statistics-summary/statistics-summary.component';
import { ProductCardComponent } from '../../../../components/shared/product-card/product-card.component';
import { RecipeCardComponent } from '../../../../components/shared/recipe-card/recipe-card.component';
import { AuthService } from '../../../../services/auth.service';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

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
    providers: [provideCharts(withDefaultRegisterables())],
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
    public guestSummary = this.buildGuestSummary();
    public guestSummarySparkline = this.buildSparkline();
    public guestMacroSparkline = this.buildMacroSparkline();
    public guestSummarySparklineOptions: ChartConfiguration['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false }, tooltip: { enabled: false } },
        elements: { line: { borderJoinStyle: 'round' } },
        scales: { x: { display: false }, y: { display: false } },
    };
    public nutritionTabs: FdUiTab[] = [
        { value: 'calories', labelKey: 'STATISTICS.NUTRITION_TABS.CALORIES' },
        { value: 'macros', labelKey: 'STATISTICS.NUTRITION_TABS.MACROS' },
        { value: 'distribution', labelKey: 'STATISTICS.NUTRITION_TABS.DISTRIBUTION' },
    ];
    public bodyTabs: FdUiTab[] = [
        { value: 'weight', labelKey: 'STATISTICS.BODY_TABS.WEIGHT' },
        { value: 'bmi', labelKey: 'STATISTICS.BODY_TABS.BMI' },
        { value: 'waist', labelKey: 'STATISTICS.BODY_TABS.WAIST' },
        { value: 'whtr', labelKey: 'STATISTICS.BODY_TABS.WHTR' },
    ];
    public selectedNutritionTab: string = 'calories';
    public selectedBodyTab: string = 'weight';
    public guestCaloriesLineData = this.buildLineData([1200, 1450, 1600, 1800, 1750, 1900, 2000]);
    public guestNutrientsLineData = this.buildMultiLineData();
    public guestPieData = this.buildPieData();
    public guestRadarData = this.buildRadarData();
    public guestBarData = this.buildBarData();
    public guestBodyDataByTab: Record<string, ChartConfiguration<'line'>['data']> = {
        weight: this.buildBodyLineData([72.4, 72.1, 71.9, 72.0, 71.8, 71.6, 71.5]),
        bmi: this.buildBodyLineData([23.6, 23.5, 23.4, 23.4, 23.3, 23.3, 23.2]),
        waist: this.buildBodyLineData([82.0, 81.6, 81.4, 81.3, 81.0, 80.8, 80.6]),
        whtr: this.buildBodyLineData([0.49, 0.49, 0.48, 0.48, 0.47, 0.47, 0.46]),
    };
    public previewProducts: Product[] = [];
    public previewRecipes: Recipe[] = [];
    public previewQuickItems: QuickMealItem[] = [];
    public lineOptions: ChartConfiguration['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
            y: { beginAtZero: true, ticks: { color: 'var(--fd-color-slate-600)' } },
            x: { ticks: { color: 'var(--fd-color-slate-600)', maxRotation: 0 } },
        },
    };
    public lineOptionsWithLegend: ChartConfiguration['options'] = {
        ...this.lineOptions,
        plugins: { legend: { position: 'bottom' } },
    };
    public pieOptions: ChartOptions<'pie'> = {};
    public radarOptions: ChartOptions<'radar'> = { scales: { r: { beginAtZero: true } } };
    public barOptions: ChartOptions<'bar'> = {
        plugins: { legend: { display: false } },
        responsive: true,
        scales: { y: { beginAtZero: true } },
    };

    private readonly clearPreviewOnAuth = effect(() => {
        if (this.isAuthenticated()) {
            this.quickConsumptionService.exitPreview();
        }
    });

    public constructor() {
        this.refreshPreviewContent();
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.refreshPreviewContent());
    }

    public addPreviewProduct(product: Product): void {
        this.quickConsumptionService.addProduct(product);
    }

    public addPreviewRecipe(recipe: Recipe): void {
        this.quickConsumptionService.addRecipe(recipe);
    }

    public openAuthDialog(mode: 'login' | 'register'): void {
        this.fdDialogService.open(AuthDialogComponent, {
            size: 'md',
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
            imageUrl: 'assets/images/stubs/meals/lunch-soup-photo.png',
            imageAssetId: null,
            items: [],
        };

        return [
            { slot: 'BREAKFAST', icon: 'wb_sunny', labelKey: 'MEAL_CARD.MEAL_TYPES.BREAKFAST', meal: null },
            { slot: 'LUNCH', icon: 'lunch_dining', labelKey: 'MEAL_CARD.MEAL_TYPES.LUNCH', meal: lunch },
            { slot: 'DINNER', icon: 'nights_stay', labelKey: 'MEAL_CARD.MEAL_TYPES.DINNER', meal: null },
        ];
    }

    private buildGuestSummary(): SummaryMetrics {
        return {
            totalCalories: 1820,
            averageCard: { consumption: 1740, steps: 6400, burned: 215 },
            macros: [
                { key: 'proteins', labelKey: 'GENERAL.NUTRIENTS.PROTEIN', value: 110, color: 'var(--fd-color-sky-500)' },
                { key: 'fats', labelKey: 'GENERAL.NUTRIENTS.FAT', value: 45, color: 'var(--fd-color-yellow-300)' },
                { key: 'carbs', labelKey: 'GENERAL.NUTRIENTS.CARB', value: 180, color: 'var(--fd-color-teal-500)' },
                { key: 'fiber', labelKey: 'SHARED.NUTRIENTS_SUMMARY.FIBER', value: 18, color: 'var(--fd-color-purple-500)' },
            ],
        };
    }

    private buildSparkline(): ChartConfiguration<'line'>['data'] {
        return {
            labels: this.getWeekdayLabels(),
            datasets: [
                {
                    data: [1600, 1700, 1800, 1500, 1900, 1750, 1820],
                    borderColor: 'var(--fd-color-primary-600)',
                    backgroundColor: 'color-mix(in srgb, var(--fd-color-primary-600) 18%, transparent)',
                    tension: 0.35,
                    borderWidth: 2,
                    fill: true,
                    pointRadius: 0,
                },
            ],
        };
    }

    private buildMacroSparkline(): Record<string, ChartConfiguration<'line'>['data']> {
        const template = (color: string): ChartConfiguration<'line'>['data'] => ({
            labels: this.getWeekdayLabels(),
            datasets: [
                {
                    data: [12, 14, 16, 11, 18, 15, 17],
                    borderColor: color,
                    backgroundColor: `${color}29`,
                    tension: 0.35,
                    borderWidth: 2,
                    fill: true,
                    pointRadius: 0,
                },
            ],
        });
        return {
            proteins: template('var(--fd-color-sky-500)'),
            fats: template('var(--fd-color-yellow-300)'),
            carbs: template('var(--fd-color-teal-500)'),
            fiber: template('var(--fd-color-purple-500)'),
        };
    }

    private buildLineData(values: number[]): ChartConfiguration<'line'>['data'] {
        return {
            labels: this.getWeekdayLabels(),
            datasets: [
                {
                    data: values,
                    borderColor: 'var(--fd-color-primary-600)',
                    backgroundColor: 'color-mix(in srgb, var(--fd-color-primary-600) 16%, transparent)',
                    tension: 0.35,
                    borderWidth: 2,
                    fill: true,
                    pointRadius: 0,
                },
            ],
        };
    }

    private buildMultiLineData(): ChartConfiguration<'line'>['data'] {
        const nutrientLabels = this.getNutrientLabels();
        return {
            labels: this.getWeekdayLabels(),
            datasets: [
                {
                    data: [110, 120, 130, 125, 140, 135, 138],
                    label: nutrientLabels.proteins,
                    borderColor: 'var(--fd-color-sky-500)',
                    tension: 0.35,
                    borderWidth: 2,
                },
                {
                    data: [40, 42, 45, 44, 46, 48, 47],
                    label: nutrientLabels.fats,
                    borderColor: 'var(--fd-color-yellow-300)',
                    tension: 0.35,
                    borderWidth: 2,
                },
                {
                    data: [160, 170, 180, 175, 185, 190, 195],
                    label: nutrientLabels.carbs,
                    borderColor: 'var(--fd-color-teal-500)',
                    tension: 0.35,
                    borderWidth: 2,
                },
            ],
        };
    }

    private buildPieData(): ChartConfiguration<'pie'>['data'] {
        const nutrientLabels = this.getNutrientLabels();
        return {
            labels: [nutrientLabels.proteins, nutrientLabels.fats, nutrientLabels.carbs],
            datasets: [
                {
                    data: [24, 18, 58],
                    backgroundColor: ['var(--fd-color-sky-500)', 'var(--fd-color-yellow-300)', 'var(--fd-color-teal-500)'],
                },
            ],
        };
    }

    private buildRadarData(): ChartConfiguration<'radar'>['data'] {
        const nutrientLabels = this.getNutrientLabels();
        return {
            labels: [nutrientLabels.proteins, nutrientLabels.fats, nutrientLabels.carbs, nutrientLabels.fiber],
            datasets: [
                {
                    data: [70, 55, 80, 60],
                    label: 'Macros',
                    backgroundColor: 'color-mix(in srgb, var(--fd-color-primary-600) 12%, transparent)',
                    borderColor: 'var(--fd-color-primary-600)',
                    pointBackgroundColor: 'var(--fd-color-primary-600)',
                },
            ],
        };
    }

    private buildBarData(): ChartConfiguration<'bar'>['data'] {
        const shortLabels = this.getNutrientShortLabels();
        return {
            labels: [shortLabels.proteins, shortLabels.fats, shortLabels.carbs],
            datasets: [
                {
                    data: [110, 45, 180],
                    backgroundColor: ['var(--fd-color-sky-500)', 'var(--fd-color-yellow-300)', 'var(--fd-color-teal-500)'],
                },
            ],
        };
    }

    private buildBodyLineData(values: number[]): ChartConfiguration<'line'>['data'] {
        return {
            labels: this.getWeekdayLabels(),
            datasets: [
                {
                    data: values,
                    borderColor: 'var(--fd-color-primary-600)',
                    backgroundColor: 'color-mix(in srgb, var(--fd-color-primary-600) 16%, transparent)',
                    tension: 0.3,
                    borderWidth: 2,
                    pointRadius: 3,
                    fill: true,
                },
            ],
        };
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
                imageUrl: 'assets/images/stubs/products/greek-yogurt-photo.png',
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
                imageUrl: 'assets/images/stubs/products/granola-photo.png',
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
                imageUrl: 'assets/images/stubs/products/salmon-bowl-photo.png',
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
                imageUrl: 'assets/images/stubs/products/chicken-avocado-salad-photo.png',
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
        this.guestSummarySparkline = this.buildSparkline();
        this.guestMacroSparkline = this.buildMacroSparkline();
        this.guestCaloriesLineData = this.buildLineData([1200, 1450, 1600, 1800, 1750, 1900, 2000]);
        this.guestNutrientsLineData = this.buildMultiLineData();
        this.guestPieData = this.buildPieData();
        this.guestRadarData = this.buildRadarData();
        this.guestBarData = this.buildBarData();
        this.guestBodyDataByTab = {
            weight: this.buildBodyLineData([72.4, 72.1, 71.9, 72.0, 71.8, 71.6, 71.5]),
            bmi: this.buildBodyLineData([23.6, 23.5, 23.4, 23.4, 23.3, 23.3, 23.2]),
            waist: this.buildBodyLineData([82.0, 81.6, 81.4, 81.3, 81.0, 80.8, 80.6]),
            whtr: this.buildBodyLineData([0.49, 0.49, 0.48, 0.48, 0.47, 0.47, 0.46]),
        };

        if (this.isAuthenticated()) {
            return;
        }

        this.quickConsumptionService.setPreviewItems(this.previewQuickItems);
    }

    private getWeekdayLabels(): string[] {
        return [
            this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.WEEKDAYS.MON'),
            this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.WEEKDAYS.TUE'),
            this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.WEEKDAYS.WED'),
            this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.WEEKDAYS.THU'),
            this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.WEEKDAYS.FRI'),
            this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.WEEKDAYS.SAT'),
            this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.WEEKDAYS.SUN'),
        ];
    }

    private getNutrientLabels(): { proteins: string; fats: string; carbs: string; fiber: string } {
        return {
            proteins: this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.NUTRIENTS.PROTEINS'),
            fats: this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.NUTRIENTS.FATS'),
            carbs: this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.NUTRIENTS.CARBS'),
            fiber: this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.NUTRIENTS.FIBER'),
        };
    }

    private getNutrientShortLabels(): { proteins: string; fats: string; carbs: string } {
        return {
            proteins: this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.NUTRIENTS_SHORT.PROTEINS'),
            fats: this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.NUTRIENTS_SHORT.FATS'),
            carbs: this.translateService.instant('LANDING_PREVIEW_TOUR.PREVIEW_DATA.NUTRIENTS_SHORT.CARBS'),
        };
    }
}

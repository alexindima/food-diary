import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import type { ItemSelection } from '../../../../../shared/dialogs/item-select-dialog/item-select-dialog-lib/item-select-dialog.types';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../../products/models/product.data';
import { RecipeManageFacade, type RecipeNutritionSummary } from '../../../lib/recipe-manage.facade';
import { type Recipe, RecipeVisibility } from '../../../models/recipe.data';
import type { IngredientFormValues, RecipeFormValues } from '../recipe-manage-lib/recipe-manage.types';
import { RecipeManageComponent } from './recipe-manage';

const RECIPE_ID = 'recipe-1';
const UPDATED_RECIPE_NAME = 'Updated recipe';
const DEFAULT_SERVINGS = 2;
const DEFAULT_COOK_TIME = 20;
const UPDATED_COOK_TIME = 35;
const PRODUCT_DEFAULT_AMOUNT = 100;
const SELECTED_PRODUCT_DEFAULT_AMOUNT = 150;
const SUMMARY_CALORIES = 300;
const SUMMARY_PROTEINS = 20;
const SUMMARY_FATS = 10;
const SUMMARY_CARBS = 30;
const SUMMARY_FIBER = 5;
const SUMMARY_ALCOHOL = 0;
const MANUAL_CALORIES = 100;
const MANUAL_PROTEINS = 10;
const MANUAL_FATS = 5;
const MANUAL_CARBS = 15;
const MANUAL_FIBER = 2;
const PRODUCT_CALORIES = 200;

type RecipeManageFacadeMock = {
    globalError: ReturnType<typeof signal<string | null>>;
    isSubmitting: ReturnType<typeof signal<boolean>>;
    addRecipe: ReturnType<typeof vi.fn>;
    applyItemSelection: ReturnType<typeof vi.fn>;
    calculateAutoSummary: ReturnType<typeof vi.fn>;
    cancelManageAsync: ReturnType<typeof vi.fn>;
    clearGlobalError: ReturnType<typeof vi.fn>;
    fromRecipeTotal: ReturnType<typeof vi.fn>;
    getSummaryFromRecipe: ReturnType<typeof vi.fn>;
    openItemSelectionDialog: ReturnType<typeof vi.fn>;
    roundNutritionValue: ReturnType<typeof vi.fn>;
    setGlobalError: ReturnType<typeof vi.fn>;
    toRecipeTotal: ReturnType<typeof vi.fn>;
    updateRecipe: ReturnType<typeof vi.fn>;
};

type RecipeManageSetup = {
    component: RecipeManageComponent;
    facade: RecipeManageFacadeMock;
    fixture: ComponentFixture<RecipeManageComponent>;
};

const EMPTY_SUMMARY: RecipeNutritionSummary = {
    calories: 0,
    proteins: 0,
    fats: 0,
    carbs: 0,
    fiber: 0,
    alcohol: 0,
};

const FILLED_SUMMARY: RecipeNutritionSummary = {
    calories: SUMMARY_CALORIES,
    proteins: SUMMARY_PROTEINS,
    fats: SUMMARY_FATS,
    carbs: SUMMARY_CARBS,
    fiber: SUMMARY_FIBER,
    alcohol: SUMMARY_ALCOHOL,
};

describe('RecipeManageComponent form population', () => {
    it('should create one empty step for new recipes', async () => {
        const { component } = await setupComponentAsync();

        expect(component['steps'].length).toBe(1);
        expect(component['steps'][0]?.ingredients.length).toBe(1);
    });

    it('should repopulate the form when recipe input is refreshed with the same id', async () => {
        const { component, fixture } = await setupComponentAsync();
        const recipe = createRecipe({ name: 'Initial recipe', cookTime: DEFAULT_COOK_TIME });
        const updatedRecipe = createRecipe({ name: UPDATED_RECIPE_NAME, cookTime: UPDATED_COOK_TIME });

        fixture.componentRef.setInput('recipe', recipe);
        fixture.detectChanges();
        expect(component['recipeFormModel']().name).toBe('Initial recipe');

        fixture.componentRef.setInput('recipe', updatedRecipe);
        fixture.detectChanges();

        expect(component['recipeFormModel']().name).toBe(UPDATED_RECIPE_NAME);
        expect(component['recipeFormModel']().cookTime).toBe(UPDATED_COOK_TIME);
    });
});

describe('RecipeManageComponent submission', () => {
    it('should submit a create DTO when the form is valid and no recipe is provided', async () => {
        const { component, facade } = await setupComponentAsync();
        patchValidManualRecipe(component);

        component['onSubmit']();

        expect(facade.clearGlobalError).toHaveBeenCalledTimes(1);
        expect(facade.addRecipe).toHaveBeenCalledWith(
            expect.objectContaining({
                name: 'Manual recipe',
                calculateNutritionAutomatically: false,
                manualCalories: MANUAL_CALORIES,
            }),
        );
        expect(facade.updateRecipe).not.toHaveBeenCalled();
    });

    it('should submit an update DTO when editing an existing recipe', async () => {
        const { component, facade, fixture } = await setupComponentAsync();
        fixture.componentRef.setInput('recipe', createRecipe());
        fixture.detectChanges();
        patchRecipeBasicValues(component, { name: UPDATED_RECIPE_NAME });
        patchFirstIngredient(component, {
            food: createProduct(),
            foodName: 'Product',
            amount: PRODUCT_DEFAULT_AMOUNT,
        });

        component['onSubmit']();

        expect(facade.updateRecipe).toHaveBeenCalledWith(
            RECIPE_ID,
            expect.objectContaining({
                name: UPDATED_RECIPE_NAME,
            }),
        );
        expect(facade.addRecipe).not.toHaveBeenCalled();
    });

    it('should set a global error and skip submit when the form is invalid', async () => {
        const { component, facade } = await setupComponentAsync();

        component['onSubmit']();

        expect(facade.setGlobalError).toHaveBeenCalledWith('FORM_ERRORS.UNKNOWN');
        expect(facade.addRecipe).not.toHaveBeenCalled();
        expect(facade.updateRecipe).not.toHaveBeenCalled();
    });

    it('should block manual submit when all macros are empty', async () => {
        const { component, facade } = await setupComponentAsync();
        patchValidRecipeBase(component);
        component['onNutritionModeChange']('manual');
        patchRecipeNutritionValues(component, { manualCalories: MANUAL_CALORIES });

        component['onSubmit']();

        expect(facade.addRecipe).not.toHaveBeenCalled();
        expect(facade.updateRecipe).not.toHaveBeenCalled();
    });
});

describe('RecipeManageComponent nutrition state', () => {
    it('should copy current summary into manual controls when switching to manual mode', async () => {
        const { component, facade } = await setupComponentAsync({
            calculateAutoSummary: vi.fn().mockReturnValue(FILLED_SUMMARY),
        });
        facade.fromRecipeTotal.mockImplementation((value: number | null | undefined) => Number(value ?? 0));
        component['totalCalories'].set(SUMMARY_CALORIES);
        component['totalFiber'].set(SUMMARY_FIBER);
        component['totalAlcohol'].set(SUMMARY_ALCOHOL);
        component['nutrientChartData'].set({
            proteins: SUMMARY_PROTEINS,
            fats: SUMMARY_FATS,
            carbs: SUMMARY_CARBS,
        });

        component['onNutritionModeChange']('manual');

        expect(component['nutritionMode']()).toBe('manual');
        expect(component['recipeFormModel']().calculateNutritionAutomatically).toBe(false);
        expect(component['recipeFormModel']().manualCalories).toBe(SUMMARY_CALORIES);
        expect(component['recipeFormModel']().manualProteins).toBe(SUMMARY_PROTEINS);
    });

    it('should convert manual nutrition values when switching between recipe and portion scale', async () => {
        const { component, facade } = await setupComponentAsync();
        patchValidManualRecipe(component);

        component['onNutritionScaleModeChange']('portion');

        expect(component['nutritionScaleMode']).toBe('portion');
        expect(component['recipeFormModel']().manualCalories).toBe(MANUAL_CALORIES / DEFAULT_SERVINGS);
        expect(component['recipeFormModel']().manualProteins).toBe(MANUAL_PROTEINS / DEFAULT_SERVINGS);

        component['onNutritionScaleModeChange']('recipe');

        expect(component['nutritionScaleMode']).toBe('recipe');
        expect(component['recipeFormModel']().manualCalories).toBe(MANUAL_CALORIES);
        expect(facade.roundNutritionValue).toHaveBeenCalled();
    });
});

describe('RecipeManageComponent steps and ingredients', () => {
    it('should add and remove steps while keeping the first step expanded', async () => {
        const { component } = await setupComponentAsync();

        component['addStep']();
        expect(component['steps'].length).toBe(DEFAULT_SERVINGS);
        expect(component['expandedStepsSet'].has(1)).toBe(true);

        component['removeStep'](0);

        expect(component['steps'].length).toBe(1);
        expect(component['expandedStepsSet'].has(0)).toBe(true);
    });

    it('should add and remove ingredients in a step', async () => {
        const { component } = await setupComponentAsync();

        component['addIngredientToStep'](0);
        expect(component['steps'][0]?.ingredients.length).toBe(DEFAULT_SERVINGS);

        component['removeIngredientFromStep']({ stepIndex: 0, ingredientIndex: 1 });

        expect(component['steps'][0]?.ingredients.length).toBe(1);
    });

    it('should apply selected product to an ingredient and recalculate automatic summary', async () => {
        const selectedProduct = createProduct({ defaultPortionAmount: SELECTED_PRODUCT_DEFAULT_AMOUNT });
        const selection: ItemSelection = { type: 'Product', product: selectedProduct };
        const { component, facade } = await setupComponentAsync({
            openItemSelectionDialog: vi.fn().mockReturnValue(of(selection)),
        });
        facade.applyItemSelection.mockImplementation((foodGroup: { patchValue: (value: Partial<IngredientFormValues>) => void }) => {
            foodGroup.patchValue({
                food: selectedProduct,
                foodName: selectedProduct.name,
                amount: selectedProduct.defaultPortionAmount,
            });
        });

        component['onProductSelectClick']({ stepIndex: 0, ingredientIndex: 0 });

        expect(facade.openItemSelectionDialog).toHaveBeenCalledTimes(1);
        expect(facade.applyItemSelection).toHaveBeenCalledTimes(1);
        expect(facade.calculateAutoSummary).toHaveBeenCalled();
        expect(component['steps'][0]?.ingredients[0]?.foodName).toBe(selectedProduct.name);
    });
});

async function setupComponentAsync(overrides: Partial<RecipeManageFacadeMock> = {}): Promise<RecipeManageSetup> {
    const facade = createRecipeManageFacadeMock(overrides);

    await TestBed.configureTestingModule({
        imports: [RecipeManageComponent, TranslateModule.forRoot()],
    })
        .overrideComponent(RecipeManageComponent, {
            remove: { providers: [RecipeManageFacade] },
            add: {
                providers: [
                    {
                        provide: RecipeManageFacade,
                        useValue: facade,
                    },
                ],
            },
        })
        .compileComponents();

    const fixture = TestBed.createComponent(RecipeManageComponent);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        facade,
        fixture,
    };
}

function createRecipeManageFacadeMock(overrides: Partial<RecipeManageFacadeMock>): RecipeManageFacadeMock {
    const facade: RecipeManageFacadeMock = {
        globalError: signal(null),
        isSubmitting: signal(false),
        addRecipe: vi.fn(),
        applyItemSelection: vi.fn(),
        calculateAutoSummary: vi.fn().mockReturnValue(EMPTY_SUMMARY),
        cancelManageAsync: vi.fn().mockResolvedValue(void 0),
        clearGlobalError: vi.fn(),
        fromRecipeTotal: vi.fn((value: number | null | undefined) => Number(value ?? 0)),
        getSummaryFromRecipe: vi.fn((recipe: Recipe | null, fallback: RecipeNutritionSummary) =>
            recipe === null ? EMPTY_SUMMARY : fallback,
        ),
        openItemSelectionDialog: vi.fn().mockReturnValue(of(null)),
        roundNutritionValue: vi.fn((value: number) => value),
        setGlobalError: vi.fn(),
        toRecipeTotal: vi.fn((value: number | null | undefined, scaleMode: string, servings: number) =>
            scaleMode === 'portion' ? Number(value ?? 0) * servings : Number(value ?? 0),
        ),
        updateRecipe: vi.fn(),
        ...overrides,
    };

    return facade;
}

function patchValidRecipeBase(component: RecipeManageComponent): void {
    patchRecipeBasicValues(component, {
        name: 'Manual recipe',
        cookTime: DEFAULT_COOK_TIME,
        servings: DEFAULT_SERVINGS,
    });
    patchFirstStep(component, {
        description: 'Cook',
    });
    patchFirstIngredient(component, {
        food: createProduct(),
        foodName: 'Product',
        amount: PRODUCT_DEFAULT_AMOUNT,
    });
}

function patchRecipeBasicValues(
    component: RecipeManageComponent,
    value: Partial<{ name: string; cookTime: number; servings: number }>,
): void {
    patchRecipeSignalAndLegacyValues(component, value);
}

function patchValidManualRecipe(component: RecipeManageComponent): void {
    patchValidRecipeBase(component);
    component['onNutritionModeChange']('manual');
    patchRecipeNutritionValues(component, {
        manualCalories: MANUAL_CALORIES,
        manualProteins: MANUAL_PROTEINS,
        manualFats: MANUAL_FATS,
        manualCarbs: MANUAL_CARBS,
        manualFiber: MANUAL_FIBER,
        manualAlcohol: SUMMARY_ALCOHOL,
    });
}

function patchRecipeNutritionValues(
    component: RecipeManageComponent,
    value: Partial<{
        manualCalories: number;
        manualProteins: number;
        manualFats: number;
        manualCarbs: number;
        manualFiber: number;
        manualAlcohol: number;
    }>,
): void {
    patchRecipeSignalAndLegacyValues(component, value);
}

function patchRecipeSignalAndLegacyValues(component: RecipeManageComponent, value: Partial<RecipeFormValues>): void {
    component['recipeFormModel'].update(current => ({
        ...current,
        ...value,
    }));
}

function patchFirstStep(component: RecipeManageComponent, value: Partial<RecipeFormValues['steps'][number]>): void {
    component['recipeFormModel'].update(current => ({
        ...current,
        steps: current.steps.map((step, index) =>
            index === 0
                ? {
                      ...step,
                      ...value,
                  }
                : step,
        ),
    }));
}

function patchFirstIngredient(component: RecipeManageComponent, value: Partial<IngredientFormValues>): void {
    component['recipeFormModel'].update(current => ({
        ...current,
        steps: current.steps.map((step, stepIndex) =>
            stepIndex === 0
                ? {
                      ...step,
                      ingredients: step.ingredients.map((ingredient, ingredientIndex) =>
                          ingredientIndex === 0
                              ? {
                                    ...ingredient,
                                    ...value,
                                }
                              : ingredient,
                      ),
                  }
                : step,
        ),
    }));
}

function createRecipe(overrides: Partial<Recipe> = {}): Recipe {
    return {
        id: RECIPE_ID,
        name: 'Initial recipe',
        description: 'Description',
        comment: null,
        category: null,
        imageUrl: null,
        imageAssetId: null,
        prepTime: 0,
        cookTime: DEFAULT_COOK_TIME,
        servings: DEFAULT_SERVINGS,
        visibility: RecipeVisibility.Public,
        usageCount: 0,
        createdAt: '2026-01-01T00:00:00Z',
        isOwnedByCurrentUser: true,
        totalCalories: SUMMARY_CALORIES,
        totalProteins: SUMMARY_PROTEINS,
        totalFats: SUMMARY_FATS,
        totalCarbs: SUMMARY_CARBS,
        totalFiber: SUMMARY_FIBER,
        totalAlcohol: SUMMARY_ALCOHOL,
        isNutritionAutoCalculated: false,
        manualCalories: MANUAL_CALORIES,
        manualProteins: MANUAL_PROTEINS,
        manualFats: MANUAL_FATS,
        manualCarbs: MANUAL_CARBS,
        manualFiber: MANUAL_FIBER,
        manualAlcohol: SUMMARY_ALCOHOL,
        steps: [
            {
                id: 'step-1',
                stepNumber: 1,
                title: null,
                instruction: 'Cook',
                imageUrl: null,
                imageAssetId: null,
                ingredients: [],
            },
        ],
        ...overrides,
    };
}

function createProduct(overrides: Partial<Product> = {}): Product {
    return {
        id: 'product-1',
        name: 'Product',
        baseUnit: MeasurementUnit.G,
        baseAmount: PRODUCT_DEFAULT_AMOUNT,
        defaultPortionAmount: PRODUCT_DEFAULT_AMOUNT,
        productType: ProductType.Unknown,
        barcode: null,
        brand: null,
        category: null,
        description: null,
        imageUrl: null,
        caloriesPerBase: PRODUCT_CALORIES,
        proteinsPerBase: MANUAL_PROTEINS,
        fatsPerBase: MANUAL_FATS,
        carbsPerBase: MANUAL_CARBS,
        fiberPerBase: MANUAL_FIBER,
        alcoholPerBase: SUMMARY_ALCOHOL,
        usageCount: 0,
        visibility: ProductVisibility.Private,
        createdAt: new Date('2026-01-01T00:00:00Z'),
        isOwnedByCurrentUser: true,
        qualityScore: 50,
        qualityGrade: 'yellow',
        ...overrides,
    };
}

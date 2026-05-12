import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';

import { ManageHeaderComponent } from '../../../../components/shared/manage-header/manage-header.component';
import { NutritionEditorComponent } from '../../../../components/shared/nutrition-editor/nutrition-editor.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import type { FormGroupControls } from '../../../../shared/lib/common.data';
import { NutritionCalculationService } from '../../../../shared/lib/nutrition-calculation.service';
import { calculateCalorieMismatchWarning, checkCaloriesError, checkMacrosError } from '../../../../shared/lib/nutrition-form.utils';
import type { NutrientData } from '../../../../shared/models/charts.data';
import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import { nonEmptyArrayValidator } from '../../../../validators/non-empty-array.validator';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../products/models/product.data';
import { RecipeManageFacade, type RecipeNutritionSummary } from '../../lib/recipe-manage.facade';
import { type Recipe, type RecipeDto, type RecipeIngredient, RecipeVisibility } from '../../models/recipe.data';
import { RecipeBasicInfoComponent } from './recipe-basic-info/recipe-basic-info.component';
import type {
    CalorieMismatchWarning,
    IngredientFormData,
    IngredientFormValues,
    MacroBarState,
    MacroKey,
    NutritionMode,
    NutritionScaleMode,
    RecipeFormData,
    RecipeFormValues,
    StepFormData,
    StepFormValues,
} from './recipe-manage.types';
import { RecipeStepsListComponent, type StepIngredientEvent } from './recipe-steps-list/recipe-steps-list.component';

const CALORIE_MISMATCH_THRESHOLD = 0.2;
const PERCENT_FULL = 100;
const LONG_TEXT_MAX_LENGTH = 1_000;
const STEP_TITLE_MAX_LENGTH = 120;
const MIN_INGREDIENT_AMOUNT = 0.01;
const DEFAULT_PRODUCT_BASE_AMOUNT = 100;
const DEFAULT_PRODUCT_QUALITY_SCORE = 50;

@Component({
    selector: 'fd-recipe-manage',
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiSegmentedToggleComponent,
        ManageHeaderComponent,
        FdPageContainerDirective,
        NutritionEditorComponent,
        RecipeBasicInfoComponent,
        RecipeStepsListComponent,
    ],
    templateUrl: './recipe-manage.component.html',
    styleUrls: ['./recipe-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [RecipeManageFacade],
})
export class RecipeManageComponent {
    private readonly translateService = inject(TranslateService);
    private readonly nutritionCalculationService = inject(NutritionCalculationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly expandedSteps = new Set<number>();
    private lastRecipeId: string | null = null;

    public readonly nutritionControlNames = {
        calories: 'manualCalories',
        proteins: 'manualProteins',
        fats: 'manualFats',
        carbs: 'manualCarbs',
        fiber: 'manualFiber',
        alcohol: 'manualAlcohol',
    };
    private readonly calorieMismatchThreshold = CALORIE_MISMATCH_THRESHOLD;
    private readonly recipeManageFacade = inject(RecipeManageFacade);

    public readonly nutritionWarning = signal<CalorieMismatchWarning | null>(null);

    public readonly recipe = input<Recipe | null>(null);
    public readonly totalCalories = signal<number>(0);
    public readonly totalFiber = signal<number>(0);
    public readonly totalAlcohol = signal<number>(0);
    public readonly nutrientChartData = signal<NutrientData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });
    public globalError = this.recipeManageFacade.globalError;
    public isSubmitting = this.recipeManageFacade.isSubmitting;
    public readonly macroBarState = computed<MacroBarState>(() => {
        const nutrients = this.nutrientChartData();
        const entries: Array<{ key: MacroKey; value: number }> = [
            { key: 'proteins', value: nutrients.proteins },
            { key: 'fats', value: nutrients.fats },
            { key: 'carbs', value: nutrients.carbs },
        ];
        const positive = entries.filter(entry => entry.value > 0);
        if (positive.length === 0) {
            return { isEmpty: true, segments: [] };
        }

        const total = positive.reduce((sum, entry) => sum + entry.value, 0);
        return {
            isEmpty: false,
            segments: positive.map(entry => ({
                key: entry.key,
                percent: (entry.value / total) * PERCENT_FULL,
            })),
        };
    });

    public recipeForm: FormGroup<RecipeFormData>;
    public visibilitySelectOptions: Array<FdUiSelectOption<RecipeVisibility>> = [];
    public readonly nutritionMode = signal<NutritionMode>('auto');
    public readonly isNutritionReadonly = computed(() => this.nutritionMode() === 'auto');
    public readonly showManualNutritionHint = computed(() => !this.isNutritionReadonly());
    public readonly manageHeaderState = computed<RecipeManageHeaderState>(() => {
        const isEdit = this.recipe() !== null;

        return {
            titleKey: isEdit ? 'RECIPE_MANAGE.EDIT_TITLE' : 'RECIPE_MANAGE.ADD_TITLE',
            submitLabelKey: isEdit ? 'RECIPE_MANAGE.SAVE_BUTTON' : 'RECIPE_MANAGE.ADD_BUTTON',
        };
    });
    public nutritionModeOptions: FdUiSegmentedToggleOption[] = [];
    public nutritionScaleMode: NutritionScaleMode = 'recipe';
    public nutritionScaleModeOptions: FdUiSegmentedToggleOption[] = [];

    private isFormReady = true;

    public constructor() {
        this.recipeForm = new FormGroup<RecipeFormData>({
            name: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
            description: new FormControl('', [Validators.maxLength(LONG_TEXT_MAX_LENGTH)]),
            comment: new FormControl<string | null>(null, [Validators.maxLength(LONG_TEXT_MAX_LENGTH)]),
            category: new FormControl<string | null>(null),
            imageUrl: new FormControl<ImageSelection | null>(null),
            prepTime: new FormControl<number | null>(0, [Validators.min(0)]),
            cookTime: new FormControl<number | null>(null, [Validators.required, Validators.min(1)]),
            servings: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
            visibility: new FormControl<RecipeVisibility>(RecipeVisibility.Public, { nonNullable: true }),
            calculateNutritionAutomatically: new FormControl<boolean>(true, { nonNullable: true }),
            manualCalories: new FormControl<number | null>(null, [Validators.min(0)]),
            manualProteins: new FormControl<number | null>(null, [Validators.min(0)]),
            manualFats: new FormControl<number | null>(null, [Validators.min(0)]),
            manualCarbs: new FormControl<number | null>(null, [Validators.min(0)]),
            manualFiber: new FormControl<number | null>(null, [Validators.min(0)]),
            manualAlcohol: new FormControl<number | null>(null, [Validators.min(0)]),
            steps: new FormArray<FormGroup<FormGroupControls<StepFormValues>>>([], nonEmptyArrayValidator()),
        });

        this.buildVisibilityOptions();
        this.buildNutritionModeOptions();
        this.buildNutritionScaleModeOptions();
        this.nutritionMode.set(this.recipeForm.controls.calculateNutritionAutomatically.value ? 'auto' : 'manual');
        this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
            this.buildVisibilityOptions();
            this.buildNutritionModeOptions();
            this.buildNutritionScaleModeOptions();
        });

        this.addStep();
        this.setupFormValueChangeTracking();
        this.recalculateNutrientsFromForm();
        this.updateManualNutritionValidators(this.recipeForm.controls.calculateNutritionAutomatically.value);
        this.updateCalorieWarning();

        this.recipeForm.controls.calculateNutritionAutomatically.valueChanges.pipe(takeUntilDestroyed()).subscribe(isAuto => {
            this.nutritionMode.set(isAuto ? 'auto' : 'manual');
            if (!this.isFormReady) {
                return;
            }
            if (!isAuto) {
                this.populateManualNutritionFromCurrentSummary();
            }
            this.updateManualNutritionValidators(isAuto);
            this.updateSummaryFromForm();
            this.updateCalorieWarning();
        });
        effect(() => {
            const recipe = this.recipe();
            untracked(() => {
                if (recipe !== null) {
                    if (this.lastRecipeId !== recipe.id) {
                        this.lastRecipeId = recipe.id;
                        this.populateForm(recipe);
                    }
                } else {
                    this.lastRecipeId = null;
                    this.updateNutrientSummary(null);
                }
            });
        });
    }

    public get steps(): FormArray<FormGroup<FormGroupControls<StepFormValues>>> {
        return this.recipeForm.controls.steps;
    }

    public get expandedStepsSet(): Set<number> {
        return this.expandedSteps;
    }

    // -- Step management (delegated from steps-list) --

    public addStep(): void {
        this.steps.push(this.createStepGroup());
        this.expandedSteps.add(this.steps.length - 1);
    }

    public removeStep(index: number): void {
        this.steps.removeAt(index);
        const nextExpanded = new Set<number>();
        this.expandedSteps.forEach(stepIndex => {
            if (stepIndex === index) {
                return;
            }
            nextExpanded.add(stepIndex > index ? stepIndex - 1 : stepIndex);
        });
        this.expandedSteps.clear();
        nextExpanded.forEach(stepIndex => this.expandedSteps.add(stepIndex));
    }

    public addIngredientToStep(stepIndex: number): void {
        const step = this.steps.at(stepIndex);
        step.controls.ingredients.push(this.createIngredientGroup());
    }

    public removeIngredientFromStep(event: StepIngredientEvent): void {
        const step = this.steps.at(event.stepIndex);
        step.controls.ingredients.removeAt(event.ingredientIndex);
    }

    public onProductSelectClick(event: StepIngredientEvent): void {
        const { stepIndex, ingredientIndex } = event;
        this.recipeManageFacade.openItemSelectionDialog().subscribe(selection => {
            if (selection === null) {
                return;
            }
            const ingredientsArray = this.steps.at(stepIndex).controls.ingredients;
            const foodGroup = ingredientsArray.at(ingredientIndex);
            this.recipeManageFacade.applyItemSelection(foodGroup, selection);
            if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
                this.recalculateNutrientsFromForm();
            }
        });
    }

    // -- Nutrition mode --

    public onNutritionModeChange(nextMode: string): void {
        const resolvedMode: NutritionMode = nextMode === 'manual' ? 'manual' : 'auto';
        if (this.nutritionMode() === resolvedMode) {
            return;
        }

        this.nutritionMode.set(resolvedMode);
        this.recipeForm.controls.calculateNutritionAutomatically.setValue(resolvedMode === 'auto');
    }

    public onNutritionScaleModeChange(nextMode: string): void {
        const resolvedMode: NutritionScaleMode = nextMode === 'portion' ? 'portion' : 'recipe';
        if (this.nutritionScaleMode === resolvedMode) {
            return;
        }

        const servings = this.getServingsValue();
        const factor = resolvedMode === 'portion' ? 1 / servings : servings;
        this.convertManualNutritionControls(factor);
        this.nutritionScaleMode = resolvedMode;
        this.updateSummaryFromForm();
    }

    public caloriesError(): string | null {
        if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
            return null;
        }

        return checkCaloriesError(this.recipeForm.controls.manualCalories)
            ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.CALORIES_REQUIRED')
            : null;
    }

    public macrosError(): string | null {
        if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
            return null;
        }

        return checkMacrosError([
            this.recipeForm.controls.manualProteins,
            this.recipeForm.controls.manualFats,
            this.recipeForm.controls.manualCarbs,
            this.recipeForm.controls.manualAlcohol,
        ])
            ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED')
            : null;
    }

    // -- Form submission --

    public onSubmit(): void {
        this.markFormGroupTouched(this.recipeForm);

        if (this.macrosError() !== null) {
            return;
        }

        if (this.recipeForm.valid === false) {
            this.recipeManageFacade.setGlobalError('FORM_ERRORS.UNKNOWN');
            return;
        }

        const recipeData = this.prepareRecipeDto();
        const existingRecipe = this.recipe();

        this.recipeManageFacade.clearGlobalError();

        if (existingRecipe !== null) {
            this.recipeManageFacade.updateRecipe(existingRecipe.id, recipeData);
        } else {
            this.recipeManageFacade.addRecipe(recipeData);
        }
    }

    public async onCancelAsync(): Promise<void> {
        await this.recipeManageFacade.cancelManageAsync();
    }

    private markFormGroupTouched(formGroup: FormGroup | FormArray): void {
        Object.values(formGroup.controls).forEach(control => {
            if (control instanceof FormGroup || control instanceof FormArray) {
                this.markFormGroupTouched(control);
            } else {
                control.markAllAsTouched();
                control.updateValueAndValidity();
            }
        });
    }

    private prepareRecipeDto(): RecipeDto {
        const formValue = this.recipeForm.getRawValue();

        return {
            name: formValue.name,
            description: formValue.description ?? null,
            comment: formValue.comment ?? null,
            category: formValue.category ?? null,
            imageUrl: formValue.imageUrl?.url ?? null,
            imageAssetId: formValue.imageUrl?.assetId ?? null,
            prepTime: formValue.prepTime ?? 0,
            cookTime: formValue.cookTime ?? 0,
            servings: formValue.servings,
            visibility: formValue.visibility,
            calculateNutritionAutomatically: formValue.calculateNutritionAutomatically,
            ...this.buildManualRecipeTotals(formValue),
            steps: this.mapRecipeSteps(formValue.steps),
        };
    }

    private mapRecipeSteps(steps: RecipeFormValues['steps']): RecipeDto['steps'] {
        return steps.map(step => this.mapRecipeStep(step)).filter(step => step.ingredients.length > 0);
    }

    private mapRecipeStep(step: RecipeFormValues['steps'][number]): RecipeDto['steps'][number] {
        return {
            title: step.title ?? null,
            imageUrl: step.imageUrl?.url ?? null,
            imageAssetId: step.imageUrl?.assetId ?? null,
            description: step.description,
            ingredients: step.ingredients
                .filter(ingredient => ingredient.food !== null || ingredient.nestedRecipeId !== null)
                .map(ingredient => ({
                    productId: ingredient.food?.id,
                    nestedRecipeId: ingredient.nestedRecipeId ?? undefined,
                    amount: ingredient.amount ?? 0,
                })),
        };
    }

    private buildManualRecipeTotals(formValue: RecipeFormValues): Partial<RecipeDto> {
        const calculateAutomatically = formValue.calculateNutritionAutomatically;
        return {
            manualCalories: calculateAutomatically ? null : this.toRecipeTotal(formValue.manualCalories),
            manualProteins: calculateAutomatically ? null : this.toRecipeTotal(formValue.manualProteins),
            manualFats: calculateAutomatically ? null : this.toRecipeTotal(formValue.manualFats),
            manualCarbs: calculateAutomatically ? null : this.toRecipeTotal(formValue.manualCarbs),
            manualFiber: calculateAutomatically ? null : this.toRecipeTotal(formValue.manualFiber),
            manualAlcohol: calculateAutomatically ? null : this.toRecipeTotal(formValue.manualAlcohol),
        };
    }

    private populateForm(recipeData: Recipe): void {
        this.isFormReady = false;
        this.recipeForm.patchValue(this.buildRecipeFormPatchValue(recipeData));

        this.resetSteps();
        this.expandedSteps.clear();

        this.populateRecipeSteps(recipeData);

        this.updateNutrientSummary(recipeData);
        this.isFormReady = true;
        if (this.hasNoRecipeNutritionTotals(recipeData)) {
            this.recalculateNutrientsFromForm();
        } else {
            this.updateSummaryFromForm();
        }
    }

    private buildRecipeFormPatchValue(recipeData: Recipe): Partial<RecipeFormValues> {
        return {
            name: recipeData.name,
            description: recipeData.description ?? '',
            comment: this.toNullable(recipeData.comment),
            category: this.toNullable(recipeData.category),
            imageUrl: {
                url: this.toNullable(recipeData.imageUrl),
                assetId: this.toNullable(recipeData.imageAssetId),
            },
            prepTime: this.withDefault(recipeData.prepTime, 0),
            cookTime: this.toNullable(recipeData.cookTime),
            servings: recipeData.servings,
            visibility: this.normalizeVisibility(recipeData.visibility),
            calculateNutritionAutomatically: recipeData.isNutritionAutoCalculated,
            ...this.buildRecipeManualNutritionPatchValue(recipeData),
        };
    }

    private buildRecipeManualNutritionPatchValue(recipeData: Recipe): Partial<RecipeFormValues> {
        return {
            manualCalories: this.resolveRecipeManualNutritionValue(recipeData.manualCalories, recipeData.totalCalories),
            manualProteins: this.resolveRecipeManualNutritionValue(recipeData.manualProteins, recipeData.totalProteins),
            manualFats: this.resolveRecipeManualNutritionValue(recipeData.manualFats, recipeData.totalFats),
            manualCarbs: this.resolveRecipeManualNutritionValue(recipeData.manualCarbs, recipeData.totalCarbs),
            manualFiber: this.resolveRecipeManualNutritionValue(recipeData.manualFiber, recipeData.totalFiber),
            manualAlcohol: this.resolveRecipeManualNutritionValue(recipeData.manualAlcohol, recipeData.totalAlcohol),
        };
    }

    private resolveRecipeManualNutritionValue(manual: number | null | undefined, total: number | null | undefined): number | null {
        return manual ?? total ?? null;
    }

    private toNullable<T>(value: T | null | undefined): T | null {
        return value ?? null;
    }

    private withDefault<T>(value: T | null | undefined, fallback: T): T {
        return value ?? fallback;
    }

    private populateRecipeSteps(recipeData: Recipe): void {
        if (recipeData.steps.length === 0) {
            this.addStep();
            return;
        }

        recipeData.steps.forEach((step, index) => {
            this.steps.push(this.createStepGroup(this.mapRecipeStepToFormValue(step)));
            this.expandedSteps.add(index);
        });
    }

    private mapRecipeStepToFormValue(step: Recipe['steps'][number]): StepFormValues {
        return {
            title: step.title ?? null,
            imageUrl: {
                url: step.imageUrl ?? null,
                assetId: step.imageAssetId ?? null,
            },
            description: step.instruction,
            ingredients: step.ingredients
                .map(ingredient => this.mapIngredientToFormValue(ingredient))
                .filter((ingredient): ingredient is IngredientFormValues => ingredient !== null),
        };
    }

    private hasNoRecipeNutritionTotals(recipeData: Recipe): boolean {
        return [recipeData.totalCalories, recipeData.totalProteins, recipeData.totalFats, recipeData.totalCarbs].every(
            value => value === null || value === undefined,
        );
    }

    private resetSteps(): void {
        while (this.steps.length > 0) {
            this.steps.removeAt(0);
        }
    }

    private createStepGroup(step?: StepFormValues): FormGroup<StepFormData> {
        const ingredientValues =
            step !== undefined && step.ingredients.length > 0
                ? step.ingredients
                : [{ food: null, amount: null, foodName: null, nestedRecipeId: null, nestedRecipeName: null }];

        return new FormGroup<StepFormData>({
            title: new FormControl(step?.title ?? null, [Validators.maxLength(STEP_TITLE_MAX_LENGTH)]),
            imageUrl: new FormControl<ImageSelection | null>(step?.imageUrl ?? null),
            description: new FormControl(step?.description ?? '', {
                nonNullable: true,
                validators: [Validators.required],
            }),
            ingredients: new FormArray<FormGroup<IngredientFormData>>(
                ingredientValues.map(ingredient =>
                    this.createIngredientGroup(ingredient.food, ingredient.amount, ingredient.nestedRecipeId, ingredient.nestedRecipeName),
                ),
                nonEmptyArrayValidator(),
            ),
        });
    }

    private createIngredientGroup(
        food: Product | null = null,
        amount: number | null = null,
        nestedRecipeId: string | null = null,
        nestedRecipeName: string | null = null,
    ): FormGroup<IngredientFormData> {
        return new FormGroup<IngredientFormData>({
            food: new FormControl(food),
            amount: new FormControl(amount, [Validators.required, Validators.min(MIN_INGREDIENT_AMOUNT)]),
            foodName: new FormControl<string | null>(food?.name ?? nestedRecipeName ?? null, [Validators.required]),
            nestedRecipeId: new FormControl<string | null>(nestedRecipeId),
            nestedRecipeName: new FormControl<string | null>(nestedRecipeName),
        });
    }

    private mapIngredientToFormValue(ingredient: RecipeIngredient): IngredientFormValues | null {
        if (ingredient.nestedRecipeId !== null && ingredient.nestedRecipeId !== undefined && ingredient.nestedRecipeId.length > 0) {
            return {
                food: null,
                amount: ingredient.amount,
                foodName: ingredient.nestedRecipeName ?? this.translateService.instant('RECIPE_MANAGE.SELECT_INGREDIENT'),
                nestedRecipeId: ingredient.nestedRecipeId,
                nestedRecipeName: ingredient.nestedRecipeName ?? null,
            };
        }
        const product = this.buildIngredientProduct(ingredient);
        if (product === null) {
            return null;
        }

        return {
            food: product,
            amount: ingredient.amount,
            foodName: product.name,
            nestedRecipeId: null,
            nestedRecipeName: null,
        };
    }

    private buildIngredientProduct(ingredient: RecipeIngredient): Product | null {
        if (ingredient.productId === null || ingredient.productId === undefined || ingredient.productId.length === 0) {
            return null;
        }

        const rawUnit = ingredient.productBaseUnit;
        const unit = this.isMeasurementUnit(rawUnit) ? rawUnit : MeasurementUnit.G;

        const baseAmount = ingredient.productBaseAmount ?? DEFAULT_PRODUCT_BASE_AMOUNT;
        return {
            id: ingredient.productId,
            name: ingredient.productName ?? this.translateService.instant('RECIPE_MANAGE.UNKNOWN_PRODUCT'),
            baseUnit: unit,
            productType: ProductType.Unknown,
            barcode: null,
            brand: null,
            category: null,
            description: null,
            imageUrl: null,
            baseAmount,
            defaultPortionAmount: baseAmount,
            ...this.buildIngredientProductNutrition(ingredient),
            usageCount: 0,
            visibility: ProductVisibility.Private,
            createdAt: new Date(),
            isOwnedByCurrentUser: true,
            qualityScore: DEFAULT_PRODUCT_QUALITY_SCORE,
            qualityGrade: 'yellow',
        };
    }

    private buildIngredientProductNutrition(
        ingredient: RecipeIngredient,
    ): Pick<Product, 'alcoholPerBase' | 'caloriesPerBase' | 'carbsPerBase' | 'fatsPerBase' | 'fiberPerBase' | 'proteinsPerBase'> {
        return {
            caloriesPerBase: ingredient.productCaloriesPerBase ?? 0,
            proteinsPerBase: ingredient.productProteinsPerBase ?? 0,
            fatsPerBase: ingredient.productFatsPerBase ?? 0,
            carbsPerBase: ingredient.productCarbsPerBase ?? 0,
            fiberPerBase: ingredient.productFiberPerBase ?? 0,
            alcoholPerBase: ingredient.productAlcoholPerBase ?? 0,
        };
    }

    private isMeasurementUnit(value: string | null | undefined): value is MeasurementUnit {
        return value === 'G' || value === 'ML' || value === 'PCS';
    }

    // -- Nutrition calculation --

    private setupFormValueChangeTracking(): void {
        this.recipeForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            if (!this.isFormReady) {
                return;
            }
            this.updateSummaryFromForm();
            this.updateCalorieWarning();
        });

        this.recipeForm.controls.steps.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            if (!this.isFormReady) {
                return;
            }
            if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
                this.recalculateNutrientsFromForm();
            }
        });
    }

    private updateNutrientSummary(recipeData: Recipe | null): void {
        const summary = this.recipeManageFacade.getSummaryFromRecipe(recipeData, {
            calories: this.totalCalories(),
            proteins: this.nutrientChartData().proteins,
            fats: this.nutrientChartData().fats,
            carbs: this.nutrientChartData().carbs,
            fiber: this.totalFiber(),
            alcohol: this.totalAlcohol(),
        });

        this.setNutrientSummary(summary);
    }

    private updateSummaryFromForm(): void {
        if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
            this.recalculateNutrientsFromForm();
            this.updateCalorieWarning();
            return;
        }

        this.setNutrientSummary({
            calories: this.toRecipeTotal(this.recipeForm.controls.manualCalories.value),
            proteins: this.toRecipeTotal(this.recipeForm.controls.manualProteins.value),
            fats: this.toRecipeTotal(this.recipeForm.controls.manualFats.value),
            carbs: this.toRecipeTotal(this.recipeForm.controls.manualCarbs.value),
            fiber: this.toRecipeTotal(this.recipeForm.controls.manualFiber.value),
            alcohol: this.toRecipeTotal(this.recipeForm.controls.manualAlcohol.value),
        });
        this.updateCalorieWarning();
    }

    private populateManualNutritionFromCurrentSummary(): void {
        this.patchManualNutritionFromCurrentSummary();
    }

    private syncManualControlsWithSummary(): void {
        this.patchManualNutritionFromCurrentSummary();
    }

    private patchManualNutritionFromCurrentSummary(): void {
        this.recipeForm.patchValue(
            {
                manualCalories: this.fromRecipeTotal(this.totalCalories()),
                manualProteins: this.fromRecipeTotal(this.nutrientChartData().proteins),
                manualFats: this.fromRecipeTotal(this.nutrientChartData().fats),
                manualCarbs: this.fromRecipeTotal(this.nutrientChartData().carbs),
                manualFiber: this.fromRecipeTotal(this.totalFiber()),
                manualAlcohol: this.fromRecipeTotal(this.totalAlcohol()),
            },
            { emitEvent: false },
        );
    }

    private recalculateNutrientsFromForm(): void {
        const summary = this.recipeManageFacade.calculateAutoSummary(this.recipeForm.controls.steps);
        this.setNutrientSummary(summary);
    }

    private setNutrientSummary({ calories, proteins, fats, carbs, fiber, alcohol }: RecipeNutritionSummary): void {
        this.totalCalories.set(this.recipeManageFacade.roundNutritionValue(calories));
        this.totalFiber.set(this.recipeManageFacade.roundNutritionValue(fiber));
        this.totalAlcohol.set(this.recipeManageFacade.roundNutritionValue(alcohol));
        this.nutrientChartData.set({
            proteins: this.recipeManageFacade.roundNutritionValue(proteins),
            fats: this.recipeManageFacade.roundNutritionValue(fats),
            carbs: this.recipeManageFacade.roundNutritionValue(carbs),
        });

        if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
            this.syncManualControlsWithSummary();
        }
    }

    private updateManualNutritionValidators(isAuto: boolean): void {
        const caloriesValidators = isAuto ? [Validators.min(0)] : [Validators.required, Validators.min(0)];
        this.recipeForm.controls.manualCalories.setValidators(caloriesValidators);
        this.recipeForm.controls.manualCalories.updateValueAndValidity({ emitEvent: false });

        this.getOptionalManualNutritionControls().forEach(control => {
            control.setValidators([Validators.min(0)]);
            control.updateValueAndValidity({ emitEvent: false });
        });
    }

    private updateCalorieWarning(): void {
        if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
            this.nutritionWarning.set(null);
            return;
        }

        const calories = this.getControlNumericValue(this.recipeForm.controls.manualCalories);
        const proteins = this.getControlNumericValue(this.recipeForm.controls.manualProteins);
        const fats = this.getControlNumericValue(this.recipeForm.controls.manualFats);
        const carbs = this.getControlNumericValue(this.recipeForm.controls.manualCarbs);
        const alcohol = this.getControlNumericValue(this.recipeForm.controls.manualAlcohol);
        this.nutritionWarning.set(
            calculateCalorieMismatchWarning({
                calories,
                proteins,
                fats,
                carbs,
                alcohol,
                threshold: this.calorieMismatchThreshold,
            }),
        );
    }

    private getControlNumericValue(control: FormControl<number | null>): number {
        const value = Number(control.value);
        return Number.isFinite(value) ? Math.max(0, value) : 0;
    }

    private getServingsValue(): number {
        const servings = Number(this.recipeForm.controls.servings.value);
        return Number.isFinite(servings) && servings > 0 ? servings : 1;
    }

    private fromRecipeTotal(value: number | null | undefined): number {
        return this.recipeManageFacade.fromRecipeTotal(value, this.nutritionScaleMode, this.getServingsValue());
    }

    private toRecipeTotal(value: number | null | undefined): number {
        return this.recipeManageFacade.toRecipeTotal(value, this.nutritionScaleMode, this.getServingsValue());
    }

    private convertManualNutritionControls(factor: number): void {
        const fields: Array<
            keyof Pick<
                RecipeFormValues,
                'manualCalories' | 'manualProteins' | 'manualFats' | 'manualCarbs' | 'manualFiber' | 'manualAlcohol'
            >
        > = ['manualCalories', 'manualProteins', 'manualFats', 'manualCarbs', 'manualFiber', 'manualAlcohol'];
        const patch: Partial<RecipeFormValues> = {};

        fields.forEach(field => {
            const raw = Number(this.recipeForm.controls[field].value);
            if (!Number.isFinite(raw)) {
                return;
            }
            patch[field] = this.recipeManageFacade.roundNutritionValue(raw * factor);
        });

        this.recipeForm.patchValue(patch, { emitEvent: false });
    }

    private getOptionalManualNutritionControls(): Array<FormControl<number | null>> {
        return [
            this.recipeForm.controls.manualProteins,
            this.recipeForm.controls.manualFats,
            this.recipeForm.controls.manualCarbs,
            this.recipeForm.controls.manualFiber,
            this.recipeForm.controls.manualAlcohol,
        ];
    }

    private buildVisibilityOptions(): void {
        this.visibilitySelectOptions = Object.values(RecipeVisibility).map(option => ({
            value: option,
            label: this.translateService.instant(`RECIPE_VISIBILITY.${option}`),
        }));
    }

    private buildNutritionModeOptions(): void {
        this.nutritionModeOptions = [
            {
                value: 'auto',
                label: this.translateService.instant('RECIPE_MANAGE.NUTRITION_MODE.AUTO'),
            },
            {
                value: 'manual',
                label: this.translateService.instant('RECIPE_MANAGE.NUTRITION_MODE.MANUAL'),
            },
        ];
    }

    private buildNutritionScaleModeOptions(): void {
        this.nutritionScaleModeOptions = [
            {
                value: 'recipe',
                label: this.translateService.instant('RECIPE_MANAGE.NUTRITION_SCALE_MODE.RECIPE'),
            },
            {
                value: 'portion',
                label: this.translateService.instant('RECIPE_MANAGE.NUTRITION_SCALE_MODE.PORTION'),
            },
        ];
    }

    private normalizeVisibility(value?: RecipeVisibility | string | null): RecipeVisibility {
        if (value === null || value === undefined || value.length === 0) {
            return RecipeVisibility.Public;
        }

        const upper = value.toString().toUpperCase();
        return upper === RecipeVisibility.Private.toUpperCase() ? RecipeVisibility.Private : RecipeVisibility.Public;
    }
}

interface RecipeManageHeaderState {
    titleKey: string;
    submitLabelKey: string;
}

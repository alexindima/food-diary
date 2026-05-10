import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
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
import { RecipeManageFacade } from '../../lib/recipe-manage.facade';
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
    private readonly calorieMismatchThreshold = 0.2;
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
                percent: (entry.value / total) * 100,
            })),
        };
    });

    public recipeForm: FormGroup<RecipeFormData>;
    public visibilitySelectOptions: FdUiSelectOption<RecipeVisibility>[] = [];
    public readonly nutritionMode = signal<NutritionMode>('auto');
    public readonly isNutritionReadonly = computed(() => this.nutritionMode() === 'auto');
    public readonly showManualNutritionHint = computed(() => !this.isNutritionReadonly());
    public readonly manageHeaderState = computed<RecipeManageHeaderState>(() => {
        const isEdit = !!this.recipe();

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
            description: new FormControl('', [Validators.maxLength(1000)]),
            comment: new FormControl<string | null>(null, [Validators.maxLength(1000)]),
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
                if (recipe) {
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
            if (!selection) {
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

        if (this.macrosError()) {
            return;
        }

        if (!this.recipeForm.valid) {
            this.recipeManageFacade.setGlobalError('FORM_ERRORS.UNKNOWN');
            return;
        }

        const recipeData = this.prepareRecipeDto();
        const existingRecipe = this.recipe();

        this.recipeManageFacade.clearGlobalError();

        if (existingRecipe) {
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
        const formValue = this.recipeForm.value as RecipeFormValues;

        const steps = formValue.steps
            .map(step => {
                const ingredients = step.ingredients
                    .filter(ingredient => !!ingredient.food || !!ingredient.nestedRecipeId)
                    .map(ingredient => ({
                        productId: ingredient.food?.id,
                        nestedRecipeId: ingredient.nestedRecipeId ?? undefined,
                        amount: ingredient.amount ?? 0,
                    }));

                return {
                    title: step.title || null,
                    imageUrl: step.imageUrl?.url || null,
                    imageAssetId: step.imageUrl?.assetId || null,
                    description: step.description,
                    ingredients,
                };
            })
            .filter(step => step.ingredients.length > 0);

        return {
            name: formValue.name,
            description: formValue.description || null,
            comment: formValue.comment || null,
            category: formValue.category || null,
            imageUrl: formValue.imageUrl?.url || null,
            imageAssetId: formValue.imageUrl?.assetId || null,
            prepTime: formValue.prepTime ?? 0,
            cookTime: formValue.cookTime ?? 0,
            servings: formValue.servings,
            visibility: formValue.visibility,
            calculateNutritionAutomatically: formValue.calculateNutritionAutomatically,
            manualCalories: formValue.calculateNutritionAutomatically ? null : this.toRecipeTotal(formValue.manualCalories),
            manualProteins: formValue.calculateNutritionAutomatically ? null : this.toRecipeTotal(formValue.manualProteins),
            manualFats: formValue.calculateNutritionAutomatically ? null : this.toRecipeTotal(formValue.manualFats),
            manualCarbs: formValue.calculateNutritionAutomatically ? null : this.toRecipeTotal(formValue.manualCarbs),
            manualFiber: formValue.calculateNutritionAutomatically ? null : this.toRecipeTotal(formValue.manualFiber),
            manualAlcohol: formValue.calculateNutritionAutomatically ? null : this.toRecipeTotal(formValue.manualAlcohol),
            steps,
        };
    }

    private populateForm(recipeData: Recipe): void {
        this.isFormReady = false;
        this.recipeForm.patchValue({
            name: recipeData.name,
            description: recipeData.description ?? '',
            comment: recipeData.comment ?? null,
            category: recipeData.category ?? null,
            imageUrl: {
                url: recipeData.imageUrl ?? null,
                assetId: recipeData.imageAssetId ?? null,
            },
            prepTime: recipeData.prepTime ?? 0,
            cookTime: recipeData.cookTime ?? null,
            servings: recipeData.servings,
            visibility: this.normalizeVisibility(recipeData.visibility),
            calculateNutritionAutomatically: recipeData.isNutritionAutoCalculated,
            manualCalories: recipeData.manualCalories ?? recipeData.totalCalories ?? null,
            manualProteins: recipeData.manualProteins ?? recipeData.totalProteins ?? null,
            manualFats: recipeData.manualFats ?? recipeData.totalFats ?? null,
            manualCarbs: recipeData.manualCarbs ?? recipeData.totalCarbs ?? null,
            manualFiber: recipeData.manualFiber ?? recipeData.totalFiber ?? null,
            manualAlcohol: recipeData.manualAlcohol ?? recipeData.totalAlcohol ?? null,
        });

        this.resetSteps();
        this.expandedSteps.clear();

        if (recipeData.steps.length === 0) {
            this.addStep();
            return;
        }

        recipeData.steps.forEach((step, index) => {
            const stepValue: StepFormValues = {
                title: step.title ?? null,
                imageUrl: {
                    url: step.imageUrl ?? null,
                    assetId: step.imageAssetId ?? null,
                },
                description: step.instruction,
                ingredients: step.ingredients
                    .map(ingredient => this.mapIngredientToFormValue(ingredient))
                    .filter(Boolean) as IngredientFormValues[],
            };
            this.steps.push(this.createStepGroup(stepValue));
            this.expandedSteps.add(index);
        });

        this.updateNutrientSummary(recipeData);
        this.isFormReady = true;
        if (
            (recipeData.totalCalories === null || recipeData.totalCalories === undefined) &&
            (recipeData.totalProteins === null || recipeData.totalProteins === undefined) &&
            (recipeData.totalFats === null || recipeData.totalFats === undefined) &&
            (recipeData.totalCarbs === null || recipeData.totalCarbs === undefined)
        ) {
            this.recalculateNutrientsFromForm();
        } else {
            this.updateSummaryFromForm();
        }
    }

    private resetSteps(): void {
        while (this.steps.length > 0) {
            this.steps.removeAt(0);
        }
    }

    private createStepGroup(step?: StepFormValues): FormGroup<StepFormData> {
        const ingredientValues = step?.ingredients.length
            ? step.ingredients
            : [{ food: null, amount: null, foodName: null, nestedRecipeId: null, nestedRecipeName: null }];

        return new FormGroup<StepFormData>({
            title: new FormControl(step?.title ?? null, [Validators.maxLength(120)]),
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
            amount: new FormControl(amount, [Validators.required, Validators.min(0.01)]),
            foodName: new FormControl<string | null>(food?.name ?? nestedRecipeName ?? null, [Validators.required]),
            nestedRecipeId: new FormControl<string | null>(nestedRecipeId),
            nestedRecipeName: new FormControl<string | null>(nestedRecipeName),
        });
    }

    private mapIngredientToFormValue(ingredient: RecipeIngredient): IngredientFormValues | null {
        if (ingredient.nestedRecipeId) {
            return {
                food: null,
                amount: ingredient.amount,
                foodName: ingredient.nestedRecipeName ?? this.translateService.instant('RECIPE_MANAGE.SELECT_INGREDIENT'),
                nestedRecipeId: ingredient.nestedRecipeId,
                nestedRecipeName: ingredient.nestedRecipeName ?? null,
            };
        }
        const product = this.buildIngredientProduct(ingredient);
        if (!product) {
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
        if (!ingredient.productId) {
            return null;
        }

        const rawUnit = ingredient.productBaseUnit as MeasurementUnit | string | undefined;
        const unit = Object.values(MeasurementUnit).includes(rawUnit as MeasurementUnit) ? (rawUnit as MeasurementUnit) : MeasurementUnit.G;

        const baseAmount = ingredient.productBaseAmount ?? 100;
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
            caloriesPerBase: ingredient.productCaloriesPerBase ?? 0,
            proteinsPerBase: ingredient.productProteinsPerBase ?? 0,
            fatsPerBase: ingredient.productFatsPerBase ?? 0,
            carbsPerBase: ingredient.productCarbsPerBase ?? 0,
            fiberPerBase: ingredient.productFiberPerBase ?? 0,
            alcoholPerBase: ingredient.productAlcoholPerBase ?? 0,
            usageCount: 0,
            visibility: ProductVisibility.Private,
            createdAt: new Date(),
            isOwnedByCurrentUser: true,
            qualityScore: 50,
            qualityGrade: 'yellow',
        };
    }

    // -- Nutrition calculation --

    private setupFormValueChangeTracking(): void {
        this.recipeForm.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
            if (!this.isFormReady) {
                return;
            }
            this.updateSummaryFromForm();
            this.updateCalorieWarning();
        });

        this.recipeForm.controls.steps.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
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

        this.setNutrientSummary(summary.calories, summary.proteins, summary.fats, summary.carbs, summary.fiber, summary.alcohol);
    }

    private updateSummaryFromForm(): void {
        if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
            this.recalculateNutrientsFromForm();
            this.updateCalorieWarning();
            return;
        }

        this.setNutrientSummary(
            this.toRecipeTotal(this.recipeForm.controls.manualCalories.value),
            this.toRecipeTotal(this.recipeForm.controls.manualProteins.value),
            this.toRecipeTotal(this.recipeForm.controls.manualFats.value),
            this.toRecipeTotal(this.recipeForm.controls.manualCarbs.value),
            this.toRecipeTotal(this.recipeForm.controls.manualFiber.value),
            this.toRecipeTotal(this.recipeForm.controls.manualAlcohol.value),
        );
        this.updateCalorieWarning();
    }

    private populateManualNutritionFromCurrentSummary(): void {
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
        this.setNutrientSummary(summary.calories, summary.proteins, summary.fats, summary.carbs, summary.fiber, summary.alcohol);
    }

    private setNutrientSummary(calories: number, proteins: number, fats: number, carbs: number, fiber: number, alcohol: number): void {
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

    private syncManualControlsWithSummary(): void {
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
        this.nutritionWarning.set(calculateCalorieMismatchWarning(calories, proteins, fats, carbs, alcohol, this.calorieMismatchThreshold));
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
        if (!value) {
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

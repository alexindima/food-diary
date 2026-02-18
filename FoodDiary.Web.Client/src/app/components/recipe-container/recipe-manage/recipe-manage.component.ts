import {
    ChangeDetectionStrategy,
    Component,
    computed,
    effect,
    inject,
    input,
    OnInit,
    signal
} from '@angular/core';
import { AbstractControl, FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormGroupControls } from '../../../types/common.data';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { MeasurementUnit, Product, ProductVisibility, ProductType } from '../../../types/product.data';
import { nonEmptyArrayValidator } from '../../../validators/non-empty-array.validator';
import {
    ConsumptionItemSelectDialogComponent,
    ConsumptionItemSelection,
    ConsumptionItemSelectDialogData,
} from '../../consumption-container/consumption-item-select-dialog/consumption-item-select-dialog.component';
import { NutrientData } from '../../../types/charts.data';
import { HttpErrorResponse } from '@angular/common/http';
import { Recipe, RecipeDto, RecipeVisibility, RecipeIngredient } from '../../../types/recipe.data';
import { RecipeService } from '../../../services/recipe.service';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { NavigationService } from '../../../services/navigation.service';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiSegmentedToggleComponent, FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';
import { CommonModule } from '@angular/common';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiDialogRef } from 'fd-ui-kit/material';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { ImageUploadFieldComponent } from '../../shared/image-upload-field/image-upload-field.component';
import { ImageSelection } from '../../../types/image-upload.data';
import { NutritionCalculationService } from '../../../services/nutrition-calculation.service';
import { NutritionEditorComponent } from '../../shared/nutrition-editor/nutrition-editor.component';

@Component({
    selector: 'fd-recipe-manage',
    imports: [
        CommonModule,
        ReactiveFormsModule,
        TranslatePipe,
        DragDropModule,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiTextareaComponent,
        FdUiSelectComponent,
        FdUiSegmentedToggleComponent,
        PageHeaderComponent,
        FdPageContainerDirective,
        ImageUploadFieldComponent,
        NutritionEditorComponent,
    ],
    templateUrl: './recipe-manage.component.html',
    styleUrls: ['./recipe-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class RecipeManageComponent implements OnInit {
    private readonly recipeService = inject(RecipeService);
    private readonly translateService = inject(TranslateService);
    private readonly navigationService = inject(NavigationService);
    private readonly nutritionCalculationService = inject(NutritionCalculationService);
    private readonly editingStepTitles = new Set<number>();
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

    public readonly nutritionWarning = signal<CalorieMismatchWarning | null>(null);

    public recipe = input<Recipe | null>(null);
    public totalCalories = signal<number>(0);
    public totalFiber = signal<number>(0);
    public totalAlcohol = signal<number>(0);
    public nutrientChartData = signal<NutrientData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });
    public globalError = signal<string | null>(null);
    public isSubmitting = signal<boolean>(false);
    public readonly macroBarState = computed<MacroBarState>(() => {
        const nutrients = this.nutrientChartData();
        const entries: Array<{ key: MacroKey; value: number }> = [
            { key: 'proteins', value: nutrients.proteins ?? 0 },
            { key: 'fats', value: nutrients.fats ?? 0 },
            { key: 'carbs', value: nutrients.carbs ?? 0 },
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
    public selectedStepIndex: number = 0;
    public selectedIngredientIndex: number = 0;
    public visibilityOptions = Object.values(RecipeVisibility);
    public visibilitySelectOptions: FdUiSelectOption<RecipeVisibility>[] = [];
    public nutritionMode: NutritionMode = 'auto';
    public nutritionModeOptions: FdUiSegmentedToggleOption[] = [];
    public nutritionScaleMode: NutritionScaleMode = 'recipe';
    public nutritionScaleModeOptions: FdUiSegmentedToggleOption[] = [];

    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly dialogRef = inject(FdUiDialogRef<RecipeManageComponent, Recipe | null>, { optional: true });
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
        this.nutritionMode = this.recipeForm.controls.calculateNutritionAutomatically.value ? 'auto' : 'manual';
        this.translateService.onLangChange
            .pipe(takeUntilDestroyed())
            .subscribe(() => {
                this.buildVisibilityOptions();
                this.buildNutritionModeOptions();
                this.buildNutritionScaleModeOptions();
            });

        this.addStep();
        this.setupFormValueChangeTracking();
        this.recalculateNutrientsFromForm();
        this.updateManualNutritionValidators(this.recipeForm.controls.calculateNutritionAutomatically.value);
        this.updateCalorieWarning();

        this.recipeForm.controls.calculateNutritionAutomatically.valueChanges
            .pipe(takeUntilDestroyed())
            .subscribe(isAuto => {
                this.nutritionMode = isAuto ? 'auto' : 'manual';
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
    }
    public getStepCollapsedHint(stepIndex: number): string | null {
        const descriptionControl = this.steps.at(stepIndex)?.controls.description;
        const text = descriptionControl?.value?.trim();
        if (!text) {
            return null;
        }
        return text.length > 80 ? `${text.slice(0, 77)}...` : text;
    }

    public ngOnInit(): void {}

    public get steps(): FormArray<FormGroup<FormGroupControls<StepFormValues>>> {
        return this.recipeForm.controls.steps;
    }

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

    public getStepIngredients(stepIndex: number):  FormArray<FormGroup<FormGroupControls<IngredientFormValues>>> {
        const step = this.steps.at(stepIndex);
        return step.controls.ingredients;
    }

    public getIngredientName(stepIndex: number, ingredientIndex: number): string {
        const ingredientsArray = this.getStepIngredients(stepIndex);
        const ingredient = ingredientsArray.at(ingredientIndex);
        return (
            ingredient.controls.food.value?.name ??
            ingredient.controls.nestedRecipeName.value ??
            this.translateService.instant('RECIPE_MANAGE.SELECT_INGREDIENT')
        );
    }

    public getProductUnit(stepIndex: number, ingredientIndex: number): string | null {
        const ingredientsArray = this.getStepIngredients(stepIndex);
        const foodControl = ingredientsArray.at(ingredientIndex).controls.food;
        const unit = foodControl.value?.baseUnit;
        return unit
            ? this.translateService.instant('PRODUCT_AMOUNT_UNITS.' + unit.toUpperCase())
            : null;
    }

    public getIngredientAmountLabel(stepIndex: number, ingredientIndex: number): string {
        const ingredientsArray = this.getStepIngredients(stepIndex);
        const ingredient = ingredientsArray.at(ingredientIndex);
        if (ingredient.controls.nestedRecipeId.value) {
            return this.translateService.instant('RECIPE_SELECT_DIALOG.SERVINGS');
        }
        const baseLabel = this.translateService.instant('RECIPE_MANAGE.INGREDIENT_AMOUNT');
        const unit = this.getProductUnit(stepIndex, ingredientIndex);
        return unit ? `${baseLabel} (${unit})` : baseLabel;
    }

    public getIngredientIcon(stepIndex: number, ingredientIndex: number): string {
        const ingredientsArray = this.getStepIngredients(stepIndex);
        const ingredient = ingredientsArray.at(ingredientIndex);
        if (ingredient.controls.nestedRecipeId.value) {
            return 'menu_book';
        }
        if (ingredient.controls.food.value) {
            return 'restaurant';
        }
        return 'search';
    }

    public getFieldError(controlName: keyof RecipeFormData): string | null {
        return this.resolveControlError(this.recipeForm.controls[controlName]);
    }

    public isStepTitleEditing(stepIndex: number): boolean {
        return this.editingStepTitles.has(stepIndex);
    }

    public toggleStepTitleEdit(stepIndex: number): void {
        if (this.editingStepTitles.has(stepIndex)) {
            this.commitStepTitle(stepIndex);
            this.editingStepTitles.delete(stepIndex);
            return;
        }

        this.editingStepTitles.add(stepIndex);
    }

    public onStepTitleBlur(stepIndex: number): void {
        this.commitStepTitle(stepIndex);
        this.editingStepTitles.delete(stepIndex);
    }

    public getStepTitleDisplay(stepIndex: number): string {
        const step = this.steps.at(stepIndex);
        const titleValue = step?.controls.title.value;
        const trimmedTitle = typeof titleValue === 'string' ? titleValue.trim() : '';
        if (trimmedTitle.length > 0) {
            return trimmedTitle;
        }

        return this.translateService.instant('RECIPE_MANAGE.STEP_TITLE', { index: stepIndex + 1 });
    }

    public getStepDescriptionError(stepIndex: number): string | null {
        const step = this.steps.at(stepIndex);
        return this.resolveControlError(step.controls.description);
    }

    public isStepExpanded(stepIndex: number): boolean {
        return this.expandedSteps.has(stepIndex);
    }

    public toggleStepExpanded(stepIndex: number): void {
        if (this.expandedSteps.has(stepIndex)) {
            this.expandedSteps.delete(stepIndex);
        } else {
            this.expandedSteps.add(stepIndex);
        }
    }

    public onNutritionModeChange(nextMode: string): void {
        const resolvedMode: NutritionMode = nextMode === 'manual' ? 'manual' : 'auto';
        if (this.nutritionMode === resolvedMode) {
            return;
        }

        this.nutritionMode = resolvedMode;
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

        const control = this.recipeForm.controls.manualCalories;
        if (!control.touched && !control.dirty) {
            return null;
        }

        const calories = this.getControlNumericValue(control);
        return calories <= 0
            ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.CALORIES_REQUIRED')
            : null;
    }

    public macrosError(): string | null {
        if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
            return null;
        }

        const controls = [
            this.recipeForm.controls.manualProteins,
            this.recipeForm.controls.manualFats,
            this.recipeForm.controls.manualCarbs,
            this.recipeForm.controls.manualAlcohol,
        ];

        const shouldShow = controls.some(control => control.touched || control.dirty);
        if (!shouldShow) {
            return null;
        }

        const proteins = this.getControlNumericValue(this.recipeForm.controls.manualProteins);
        const fats = this.getControlNumericValue(this.recipeForm.controls.manualFats);
        const carbs = this.getControlNumericValue(this.recipeForm.controls.manualCarbs);
        const alcohol = this.getControlNumericValue(this.recipeForm.controls.manualAlcohol);

        return proteins <= 0 && fats <= 0 && carbs <= 0 && alcohol <= 0
            ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED')
            : null;
    }

    public getStepIngredientsCount(stepIndex: number): number {
        const step = this.steps.at(stepIndex);
        return step?.controls.ingredients.length ?? 0;
    }

    public getStepDescriptionSummary(stepIndex: number): string {
        const step = this.steps.at(stepIndex);
        const description = step?.controls.description.value?.trim() ?? '';
        if (!description) {
            return this.translateService.instant('RECIPE_MANAGE.STEP_NO_DESCRIPTION');
        }

        return description;
    }

    public onStepDrop(event: CdkDragDrop<FormGroup<StepFormData>[]>): void {
        if (event.previousIndex === event.currentIndex) {
            return;
        }

        moveItemInArray(this.steps.controls, event.previousIndex, event.currentIndex);
        this.steps.updateValueAndValidity();
        this.steps.markAsDirty();
    }

    public getIngredientControlError(
        stepIndex: number,
        ingredientIndex: number,
        controlName: 'food' | 'foodName' | 'amount',
    ): string | null {
        const ingredient = this.getStepIngredients(stepIndex).at(ingredientIndex);
        return this.resolveControlError(ingredient.controls[controlName]);
    }

    public async onProductSelectClick(stepIndex: number, ingredientIndex: number): Promise<void> {
        this.selectedStepIndex = stepIndex;
        this.selectedIngredientIndex = ingredientIndex;
        this.fdDialogService
            .open<ConsumptionItemSelectDialogComponent, ConsumptionItemSelectDialogData, ConsumptionItemSelection | null>(
                ConsumptionItemSelectDialogComponent,
                {
                    size: 'lg',
                    data: { initialTab: 'Product' },
                },
            )
            .afterClosed()
            .subscribe(selection => {
                if (!selection) {
                    return;
                }
                const ingredientsArray = this.getStepIngredients(stepIndex);
                const foodGroup = ingredientsArray.at(ingredientIndex);
                if (selection.type === 'Product') {
                    const food = selection.product;
                    const defaultAmount = food.defaultPortionAmount ?? food.baseAmount ?? 0;
                    foodGroup.patchValue({
                        food,
                        foodName: food.name,
                        nestedRecipeId: null,
                        nestedRecipeName: null,
                        amount: defaultAmount,
                    });
                } else {
                    const recipe = selection.recipe;
                    foodGroup.patchValue({
                        food: null,
                        foodName: recipe.name,
                        nestedRecipeId: recipe.id,
                        nestedRecipeName: recipe.name,
                        amount: 1,
                    });
                }
                if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
                    this.recalculateNutrientsFromForm();
                }
            });
    }

    public addIngredientToStep(stepIndex: number): void {
        const step = this.steps.at(stepIndex);
        const ingredients = step.controls.ingredients;

        ingredients.push(this.createIngredientGroup());
    }

    public removeIngredientFromStep(stepIndex: number, ingredientIndex: number): void {
        const step = this.steps.at(stepIndex);
        const ingredients = step.controls.ingredients;
        ingredients.removeAt(ingredientIndex);
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
            recipeData.totalCalories == null &&
            recipeData.totalProteins == null &&
            recipeData.totalFats == null &&
            recipeData.totalCarbs == null
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
        const ingredientValues = step?.ingredients?.length
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
                    this.createIngredientGroup(
                        ingredient.food,
                        ingredient.amount,
                        ingredient.nestedRecipeId,
                        ingredient.nestedRecipeName,
                    ),
                ),
                nonEmptyArrayValidator(),
            ),
        });
    }

    private commitStepTitle(stepIndex: number): void {
        const step = this.steps.at(stepIndex);
        const titleControl = step?.controls.title;
        if (!titleControl) {
            return;
        }

        const titleValue = titleControl.value;
        const trimmedTitle = typeof titleValue === 'string' ? titleValue.trim() : '';
        titleControl.setValue(trimmedTitle.length > 0 ? trimmedTitle : null);
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
        const unit = Object.values(MeasurementUnit).includes(rawUnit as MeasurementUnit)
            ? (rawUnit as MeasurementUnit)
            : MeasurementUnit.G;

        const baseAmount = ingredient.productBaseAmount ?? 100;
        const product: Product = {
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
        };

        return product;
    }

    public onSubmit(): void {
        this.markFormGroupTouched(this.recipeForm);

        if (this.macrosError()) {
            return;
        }

        if (!this.recipeForm.valid) {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
            return;
        }

        const recipeData = this.prepareRecipeDto();
        const existingRecipe = this.recipe();

        this.clearGlobalError();

        if (existingRecipe) {
            this.updateRecipe(existingRecipe.id, recipeData);
        } else {
            this.addRecipe(recipeData);
        }
    }

    private addRecipe(recipeData: RecipeDto): void {
        this.isSubmitting.set(true);
        this.recipeService
            .create(recipeData)
            .pipe(finalize(() => this.isSubmitting.set(false)))
            .subscribe({
                next: recipe => this.handleSubmitResponse(recipe),
                error: error => this.handleSubmitError(error),
            });
    }

    private updateRecipe(id: string, recipeData: RecipeDto): void {
        this.isSubmitting.set(true);
        this.recipeService
            .update(id, recipeData)
            .pipe(finalize(() => this.isSubmitting.set(false)))
            .subscribe({
                next: recipe => this.handleSubmitResponse(recipe),
                error: error => this.handleSubmitError(error),
            });
    }

    public async onCancel(): Promise<void> {
        if (this.dialogRef) {
            this.dialogRef.close(null);
            return;
        }
        await this.navigationService.navigateToRecipeList();
    }

    private async handleSubmitResponse(_response: Recipe): Promise<void> {
        this.clearGlobalError();
        if (this.dialogRef) {
            this.dialogRef.close(_response);
            return;
        }
        await this.navigationService.navigateToRecipeList();
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

    private handleSubmitError(error?: HttpErrorResponse): void {
        const message = error?.error?.message ?? this.translateService.instant('FORM_ERRORS.UNKNOWN');
        this.setGlobalError(message, false);
    }

    private setGlobalError(message: string, translate: boolean = true): void {
        this.globalError.set(translate ? this.translateService.instant(message) : message);
    }

    private clearGlobalError(): void {
        this.globalError.set(null);
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
            visibility: formValue.visibility ?? RecipeVisibility.Private,
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

    private setupFormValueChangeTracking(): void {
        this.recipeForm.valueChanges
            .pipe(takeUntilDestroyed())
            .subscribe(() => {
                if (!this.isFormReady) {
                    return;
                }
                this.updateSummaryFromForm();
                this.updateCalorieWarning();
            });

        this.recipeForm.controls.steps.valueChanges
            .pipe(takeUntilDestroyed())
            .subscribe(() => {
                if (!this.isFormReady) {
                    return;
                }
                if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
                    this.recalculateNutrientsFromForm();
                }
            });
    }

    private updateNutrientSummary(recipeData: Recipe | null): void {
        if (!recipeData) {
            this.setNutrientSummary(0, 0, 0, 0, 0, 0);
            return;
        }

        this.setNutrientSummary(
            recipeData.totalCalories ?? this.totalCalories(),
            recipeData.totalProteins ?? this.nutrientChartData().proteins,
            recipeData.totalFats ?? this.nutrientChartData().fats,
            recipeData.totalCarbs ?? this.nutrientChartData().carbs,
            recipeData.totalFiber ?? this.totalFiber(),
            recipeData.totalAlcohol ?? this.totalAlcohol(),
        );
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
        this.recipeForm.patchValue({
            manualCalories: this.fromRecipeTotal(this.totalCalories()),
            manualProteins: this.fromRecipeTotal(this.nutrientChartData().proteins),
            manualFats: this.fromRecipeTotal(this.nutrientChartData().fats),
            manualCarbs: this.fromRecipeTotal(this.nutrientChartData().carbs),
            manualFiber: this.fromRecipeTotal(this.totalFiber()),
            manualAlcohol: this.fromRecipeTotal(this.totalAlcohol()),
        }, { emitEvent: false });
    }

    private resolveControlError(control: AbstractControl | null): string | null {
        if (!control) {
            return null;
        }

        if (!control.touched && !control.dirty) {
            return null;
        }

        const errors = control.errors;
        if (!errors) {
            return null;
        }

        if (errors['required']) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        if (errors['min']) {
            const min = errors['min'].min ?? 0;
            return this.translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', { min });
        }

        if (errors['nonEmptyArray']) {
            return this.translateService.instant('FORM_ERRORS.NON_EMPTY_ARRAY');
        }

        return this.translateService.instant('FORM_ERRORS.UNKNOWN');
    }

    private recalculateNutrientsFromForm(): void {
        const stepsArray = this.recipeForm.controls.steps;
        if (!stepsArray || stepsArray.length === 0) {
            this.setNutrientSummary(0, 0, 0, 0, 0, 0);
            return;
        }

        let totalCalories = 0;
        let totalProteins = 0;
        let totalFats = 0;
        let totalCarbs = 0;
        let totalFiber = 0;
        let totalAlcohol = 0;

        stepsArray.controls.forEach(stepGroup => {
            const ingredients = stepGroup.controls.ingredients;
            ingredients.controls.forEach(ingredientGroup => {
                const food = ingredientGroup.controls.food.value;
                const amount = ingredientGroup.controls.amount.value;

                if (!food || !amount || amount <= 0) {
                    return;
                }

                const baseAmount = food.baseAmount || 1;
                const multiplier = amount / baseAmount;

                totalCalories += (food.caloriesPerBase ?? 0) * multiplier;
                totalProteins += (food.proteinsPerBase ?? 0) * multiplier;
                totalFats += (food.fatsPerBase ?? 0) * multiplier;
                totalCarbs += (food.carbsPerBase ?? 0) * multiplier;
                totalFiber += (food.fiberPerBase ?? 0) * multiplier;
                totalAlcohol += (food.alcoholPerBase ?? 0) * multiplier;
            });
        });

        this.setNutrientSummary(
            this.roundNutrient(totalCalories),
            this.roundNutrient(totalProteins),
            this.roundNutrient(totalFats),
            this.roundNutrient(totalCarbs),
            this.roundNutrient(totalFiber),
            this.roundNutrient(totalAlcohol),
        );
    }

    private setNutrientSummary(
        calories: number,
        proteins: number,
        fats: number,
        carbs: number,
        fiber: number,
        alcohol: number,
    ): void {
        this.totalCalories.set(this.roundNutrient(calories));
        this.totalFiber.set(this.roundNutrient(fiber));
        this.totalAlcohol.set(this.roundNutrient(alcohol));
        this.nutrientChartData.set({
            proteins: this.roundNutrient(proteins),
            fats: this.roundNutrient(fats),
            carbs: this.roundNutrient(carbs),
        });

        if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
            this.syncManualControlsWithSummary();
        }
    }

    private roundNutrient(value: number): number {
        return Math.round(value * 100) / 100;
    }

    private syncManualControlsWithSummary(): void {
        this.recipeForm.patchValue({
            manualCalories: this.fromRecipeTotal(this.totalCalories()),
            manualProteins: this.fromRecipeTotal(this.nutrientChartData().proteins),
            manualFats: this.fromRecipeTotal(this.nutrientChartData().fats),
            manualCarbs: this.fromRecipeTotal(this.nutrientChartData().carbs),
            manualFiber: this.fromRecipeTotal(this.totalFiber()),
            manualAlcohol: this.fromRecipeTotal(this.totalAlcohol()),
        }, { emitEvent: false });
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
        const expectedCalories = this.nutritionCalculationService.calculateCaloriesFromMacros(proteins, fats, carbs, alcohol);

        if (expectedCalories <= 0 || calories <= 0) {
            this.nutritionWarning.set(null);
            return;
        }

        const deviation = Math.abs(calories - expectedCalories) / expectedCalories;
        if (deviation <= this.calorieMismatchThreshold) {
            this.nutritionWarning.set(null);
            return;
        }

        this.nutritionWarning.set({
            expectedCalories: Math.round(expectedCalories),
            actualCalories: Math.round(calories),
        });
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
        const normalized = Number(value ?? 0);
        if (!Number.isFinite(normalized)) {
            return 0;
        }

        if (this.nutritionScaleMode === 'recipe') {
            return normalized;
        }

        return this.roundNutrient(normalized / this.getServingsValue());
    }

    private toRecipeTotal(value: number | null | undefined): number {
        const normalized = Number(value ?? 0);
        if (!Number.isFinite(normalized)) {
            return 0;
        }

        if (this.nutritionScaleMode === 'recipe') {
            return normalized;
        }

        return this.roundNutrient(normalized * this.getServingsValue());
    }

    private convertManualNutritionControls(factor: number): void {
        const fields: Array<keyof Pick<RecipeFormValues, 'manualCalories' | 'manualProteins' | 'manualFats' | 'manualCarbs' | 'manualFiber' | 'manualAlcohol'>> = [
            'manualCalories',
            'manualProteins',
            'manualFats',
            'manualCarbs',
            'manualFiber',
            'manualAlcohol',
        ];
        const patch: Partial<RecipeFormValues> = {};

        fields.forEach(field => {
            const raw = Number(this.recipeForm.controls[field].value);
            if (!Number.isFinite(raw)) {
                return;
            }
            patch[field] = this.roundNutrient(raw * factor);
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
        this.visibilitySelectOptions = this.visibilityOptions.map(option => ({
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
        return upper === RecipeVisibility.Private.toUpperCase()
            ? RecipeVisibility.Private
            : RecipeVisibility.Public;
    }

}

interface RecipeFormValues {
    name: string;
    description: string | null;
    comment: string | null;
    category: string | null;
    imageUrl: ImageSelection | null;
    prepTime: number | null;
    cookTime: number | null;
    servings: number;
    visibility: RecipeVisibility;
    calculateNutritionAutomatically: boolean;
    manualCalories: number | null;
    manualProteins: number | null;
    manualFats: number | null;
    manualCarbs: number | null;
    manualFiber: number | null;
    manualAlcohol: number | null;
    steps: StepFormValues[];
}

interface StepFormValues {
    title: string | null;
    imageUrl: ImageSelection | null;
    description: string;
    ingredients: IngredientFormValues[];
}

interface IngredientFormValues {
    food: Product | null;
    amount: number | null;
    foodName: string | null;
    nestedRecipeId: string | null;
    nestedRecipeName: string | null;
}

type RecipeFormData = FormGroupControls<RecipeFormValues>;
type StepFormData = FormGroupControls<StepFormValues>;
type IngredientFormData = FormGroupControls<IngredientFormValues>;

interface CalorieMismatchWarning {
    expectedCalories: number;
    actualCalories: number;
}

type NutritionMode = 'auto' | 'manual';
type NutritionScaleMode = 'recipe' | 'portion';
type MacroKey = 'proteins' | 'fats' | 'carbs';

interface MacroBarSegment {
    key: MacroKey;
    percent: number;
}

interface MacroBarState {
    isEmpty: boolean;
    segments: MacroBarSegment[];
}


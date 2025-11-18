import {
    ChangeDetectionStrategy,
    Component,
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
    ProductListDialogComponent
} from '../../product-container/product-list/product-list-dialog/product-list-dialog.component';
import { NutrientChartData } from '../../../types/charts.data';
import { HttpErrorResponse } from '@angular/common/http';
import {
    NutrientsSummaryComponent
} from '../../shared/nutrients-summary/nutrients-summary.component';
import { Recipe, RecipeDto, RecipeVisibility, RecipeIngredient } from '../../../types/recipe.data';
import { RecipeService } from '../../../services/recipe.service';
import { DropZoneDirective } from '../../../directives/drop-zone.directive';
import { DraggableDirective } from '../../../directives/draggable.directive';
import { NavigationService } from '../../../services/navigation.service';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiCardComponent } from '../../../ui-kit/card/fd-ui-card.component';
import { FdUiInputComponent } from '../../../ui-kit/input/fd-ui-input.component';
import { FdUiTextareaComponent } from '../../../ui-kit/textarea/fd-ui-textarea.component';
import { FdUiButtonComponent } from '../../../ui-kit/button/fd-ui-button.component';
import { FdUiSelectComponent, FdUiSelectOption } from '../../../ui-kit/select/fd-ui-select.component';
import { FdUiCheckboxComponent } from '../../../ui-kit/checkbox/fd-ui-checkbox.component';
import { CommonModule } from '@angular/common';
import { FdUiDialogService } from '../../../ui-kit/dialog/fd-ui-dialog.service';

@Component({
    selector: 'fd-recipe-manage',
    imports: [
        CommonModule,
        ReactiveFormsModule,
        TranslatePipe,
        NutrientsSummaryComponent,
        DropZoneDirective,
        DraggableDirective,
        FdUiCardComponent,
        FdUiInputComponent,
        FdUiTextareaComponent,
        FdUiButtonComponent,
        FdUiSelectComponent,
        FdUiCheckboxComponent,
    ],
    templateUrl: './recipe-manage.component.html',
    styleUrls: ['./recipe-manage.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class RecipeManageComponent implements OnInit {
    private readonly recipeService = inject(RecipeService);
    private readonly translateService = inject(TranslateService);
    private readonly navigationService = inject(NavigationService);

    public recipe = input<Recipe | null>(null);
    public totalCalories = signal<number>(0);
    public totalFiber = signal<number>(0);
    public nutrientChartData = signal<NutrientChartData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });
    public globalError = signal<string | null>(null);
    public isSubmitting = signal<boolean>(false);

    public recipeForm: FormGroup<RecipeFormData>;
    public selectedStepIndex: number = 0;
    public selectedIngredientIndex: number = 0;
    public visibilityOptions = Object.values(RecipeVisibility);
    public visibilitySelectOptions: FdUiSelectOption<RecipeVisibility>[] = [];

    private readonly fdDialogService = inject(FdUiDialogService);
    private isFormReady = true;

    public constructor() {
        this.recipeForm = new FormGroup<RecipeFormData>({
            name: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
            description: new FormControl('', [Validators.maxLength(1000)]),
            category: new FormControl<string | null>(null),
            imageUrl: new FormControl<string | null>(null),
            prepTime: new FormControl<number | null>(null, [Validators.required, Validators.min(1)]),
            cookTime: new FormControl<number | null>(null, [Validators.required, Validators.min(1)]),
            servings: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
            visibility: new FormControl<RecipeVisibility>(RecipeVisibility.Public, { nonNullable: true }),
            calculateNutritionAutomatically: new FormControl<boolean>(true, { nonNullable: true }),
            manualCalories: new FormControl<number | null>(null, [Validators.min(0)]),
            manualProteins: new FormControl<number | null>(null, [Validators.min(0)]),
            manualFats: new FormControl<number | null>(null, [Validators.min(0)]),
            manualCarbs: new FormControl<number | null>(null, [Validators.min(0)]),
            manualFiber: new FormControl<number | null>(null, [Validators.min(0)]),
            steps: new FormArray<FormGroup<FormGroupControls<StepFormValues>>>([], nonEmptyArrayValidator()),
        });

        this.buildVisibilityOptions();
        this.translateService.onLangChange
            .pipe(takeUntilDestroyed())
            .subscribe(() => this.buildVisibilityOptions());

        this.addStep();
        this.setupFormValueChangeTracking();
        this.recalculateNutrientsFromForm();
        this.updateManualNutritionValidators(this.recipeForm.controls.calculateNutritionAutomatically.value);

        this.recipeForm.controls.calculateNutritionAutomatically.valueChanges
            .pipe(takeUntilDestroyed())
            .subscribe(isAuto => {
                if (!this.isFormReady) {
                    return;
                }
                if (!isAuto) {
                    this.populateManualNutritionFromCurrentSummary();
                }
                 this.updateManualNutritionValidators(isAuto);
                this.updateSummaryFromForm();
            });
        effect(() => {
            const recipe = this.recipe();
            if (recipe) {
                this.populateForm(recipe);
            } else {
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
    }

    public removeStep(index: number): void {
        this.steps.removeAt(index);
    }

    public getStepIngredients(stepIndex: number):  FormArray<FormGroup<FormGroupControls<IngredientFormValues>>> {
        const step = this.steps.at(stepIndex);
        return step.controls.ingredients;
    }

    public getProductName(stepIndex: number, ingredientIndex: number): string {
        const ingredientsArray = this.getStepIngredients(stepIndex);
        const foodControl = ingredientsArray.at(ingredientIndex).controls.food;
        return foodControl?.value?.name || this.translateService.instant('RECIPE_MANAGE.SELECT_INGREDIENT');
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
        const baseLabel = this.translateService.instant('RECIPE_MANAGE.INGREDIENT_AMOUNT');
        const unit = this.getProductUnit(stepIndex, ingredientIndex);
        return unit ? `${baseLabel} (${unit})` : baseLabel;
    }

    public getFieldError(controlName: keyof RecipeFormData): string | null {
        return this.resolveControlError(this.recipeForm.controls[controlName]);
    }

    public getStepDescriptionError(stepIndex: number): string | null {
        const step = this.steps.at(stepIndex);
        return this.resolveControlError(step.controls.description);
    }

    public getIngredientControlError(
        stepIndex: number,
        ingredientIndex: number,
        controlName: 'food' | 'amount',
    ): string | null {
        const ingredient = this.getStepIngredients(stepIndex).at(ingredientIndex);
        return this.resolveControlError(ingredient.controls[controlName]);
    }

    public async onProductSelectClick(stepIndex: number, ingredientIndex: number): Promise<void> {
        this.selectedStepIndex = stepIndex;
        this.selectedIngredientIndex = ingredientIndex;
        this.fdDialogService
            .open<ProductListDialogComponent, Product | null, Product | null>(ProductListDialogComponent, {
                size: 'lg',
            })
            .afterClosed()
            .subscribe(food => {
                if (!food) {
                    return;
                }
                const ingredientsArray = this.getStepIngredients(stepIndex);
                const foodGroup = ingredientsArray.at(ingredientIndex);
                foodGroup.patchValue({ food, foodName: food.name });
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
            category: recipeData.category ?? null,
            imageUrl: recipeData.imageUrl ?? null,
            prepTime: recipeData.prepTime ?? null,
            cookTime: recipeData.cookTime ?? null,
            servings: recipeData.servings,
            visibility: this.normalizeVisibility(recipeData.visibility),
            calculateNutritionAutomatically: recipeData.isNutritionAutoCalculated,
            manualCalories: recipeData.manualCalories ?? recipeData.totalCalories ?? null,
            manualProteins: recipeData.manualProteins ?? recipeData.totalProteins ?? null,
            manualFats: recipeData.manualFats ?? recipeData.totalFats ?? null,
            manualCarbs: recipeData.manualCarbs ?? recipeData.totalCarbs ?? null,
            manualFiber: recipeData.manualFiber ?? recipeData.totalFiber ?? null,
        });

        this.resetSteps();

        if (recipeData.steps.length === 0) {
            this.addStep();
            return;
        }

        recipeData.steps.forEach(step => {
            const stepValue: StepFormValues = {
                description: step.instruction,
                ingredients: step.ingredients
                    .map(ingredient => this.mapIngredientToFormValue(ingredient))
                    .filter(Boolean) as IngredientFormValues[],
            };
            this.steps.push(this.createStepGroup(stepValue));
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
            : [{ food: null, amount: null }];

        return new FormGroup<StepFormData>({
            description: new FormControl(step?.description ?? '', {
                nonNullable: true,
                validators: [Validators.required],
            }),
            ingredients: new FormArray<FormGroup<IngredientFormData>>(
                ingredientValues.map(ingredient => this.createIngredientGroup(ingredient.food, ingredient.amount)),
                nonEmptyArrayValidator(),
            ),
        });
    }

    private createIngredientGroup(food: Product | null = null, amount: number | null = null): FormGroup<IngredientFormData> {
        return new FormGroup<IngredientFormData>({
            food: new FormControl(food, [Validators.required]),
            amount: new FormControl(amount, [Validators.required, Validators.min(0.01)]),
            foodName: new FormControl<string | null>(food?.name ?? null),
        });
    }

    private mapIngredientToFormValue(ingredient: RecipeIngredient): IngredientFormValues | null {
        const product = this.buildIngredientProduct(ingredient);
        if (!product) {
            return null;
        }

        return {
            food: product,
            amount: ingredient.amount,
            foodName: product.name,
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
            caloriesPerBase: ingredient.productCaloriesPerBase ?? 0,
            proteinsPerBase: ingredient.productProteinsPerBase ?? 0,
            fatsPerBase: ingredient.productFatsPerBase ?? 0,
            carbsPerBase: ingredient.productCarbsPerBase ?? 0,
            fiberPerBase: ingredient.productFiberPerBase ?? 0,
            usageCount: 0,
            visibility: ProductVisibility.Private,
            createdAt: new Date(),
            isOwnedByCurrentUser: true,
        };

        return product;
    }

    public onSubmit(): void {
        this.markFormGroupTouched(this.recipeForm);

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
        await this.navigationService.navigateToRecipeList();
    }

    private async handleSubmitResponse(_response: Recipe): Promise<void> {
        this.clearGlobalError();
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
                    .filter(ingredient => !!ingredient.food)
                    .map(ingredient => ({
                        productId: ingredient.food!.id,
                        amount: ingredient.amount ?? 0,
                    }));

                return {
                    description: step.description,
                    ingredients,
                };
            })
            .filter(step => step.ingredients.length > 0);

        return {
            name: formValue.name,
            description: formValue.description || null,
            category: formValue.category || null,
            imageUrl: formValue.imageUrl || null,
            prepTime: formValue.prepTime ?? 0,
            cookTime: formValue.cookTime ?? 0,
            servings: formValue.servings,
            visibility: formValue.visibility ?? RecipeVisibility.Private,
            calculateNutritionAutomatically: formValue.calculateNutritionAutomatically,
            manualCalories: formValue.calculateNutritionAutomatically ? null : (formValue.manualCalories ?? 0),
            manualProteins: formValue.calculateNutritionAutomatically ? null : (formValue.manualProteins ?? 0),
            manualFats: formValue.calculateNutritionAutomatically ? null : (formValue.manualFats ?? 0),
            manualCarbs: formValue.calculateNutritionAutomatically ? null : (formValue.manualCarbs ?? 0),
            manualFiber: formValue.calculateNutritionAutomatically ? null : (formValue.manualFiber ?? 0),
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
            });
    }

    private updateNutrientSummary(recipeData: Recipe | null): void {
        if (!recipeData) {
            this.setNutrientSummary(0, 0, 0, 0, 0);
            return;
        }

        this.setNutrientSummary(
            recipeData.totalCalories ?? this.totalCalories(),
            recipeData.totalProteins ?? this.nutrientChartData().proteins,
            recipeData.totalFats ?? this.nutrientChartData().fats,
            recipeData.totalCarbs ?? this.nutrientChartData().carbs,
            recipeData.totalFiber ?? this.totalFiber(),
        );
    }

    private updateSummaryFromForm(): void {
        if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
            this.recalculateNutrientsFromForm();
            return;
        }

        this.setNutrientSummary(
            this.recipeForm.controls.manualCalories.value ?? 0,
            this.recipeForm.controls.manualProteins.value ?? 0,
            this.recipeForm.controls.manualFats.value ?? 0,
            this.recipeForm.controls.manualCarbs.value ?? 0,
            this.recipeForm.controls.manualFiber.value ?? 0,
        );
    }

    private populateManualNutritionFromCurrentSummary(): void {
        this.recipeForm.patchValue({
            manualCalories: this.totalCalories(),
            manualProteins: this.nutrientChartData().proteins,
            manualFats: this.nutrientChartData().fats,
            manualCarbs: this.nutrientChartData().carbs,
            manualFiber: this.totalFiber(),
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
            this.setNutrientSummary(0, 0, 0, 0, 0);
            return;
        }

        let totalCalories = 0;
        let totalProteins = 0;
        let totalFats = 0;
        let totalCarbs = 0;
        let totalFiber = 0;

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
            });
        });

        this.setNutrientSummary(
            this.roundNutrient(totalCalories),
            this.roundNutrient(totalProteins),
            this.roundNutrient(totalFats),
            this.roundNutrient(totalCarbs),
            this.roundNutrient(totalFiber),
        );
    }

    private setNutrientSummary(calories: number, proteins: number, fats: number, carbs: number, fiber: number): void {
        this.totalCalories.set(this.roundNutrient(calories));
        this.totalFiber.set(this.roundNutrient(fiber));
        this.nutrientChartData.set({
            proteins: this.roundNutrient(proteins),
            fats: this.roundNutrient(fats),
            carbs: this.roundNutrient(carbs),
        });
    }

    private roundNutrient(value: number): number {
        return Math.round(value * 100) / 100;
    }

    private updateManualNutritionValidators(isAuto: boolean): void {
        const validators = isAuto ? [] : [Validators.required, Validators.min(0)];
        this.getManualNutritionControls().forEach(control => {
            control.setValidators(validators);
            control.updateValueAndValidity({ emitEvent: false });
        });
    }

    private getManualNutritionControls(): Array<FormControl<number | null>> {
        return [
            this.recipeForm.controls.manualCalories,
            this.recipeForm.controls.manualProteins,
            this.recipeForm.controls.manualFats,
            this.recipeForm.controls.manualCarbs,
            this.recipeForm.controls.manualFiber,
        ];
    }

    private buildVisibilityOptions(): void {
        this.visibilitySelectOptions = this.visibilityOptions.map(option => ({
            value: option,
            label: this.translateService.instant(`RECIPE_VISIBILITY.${option}`),
        }));
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
    category: string | null;
    imageUrl: string | null;
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
    steps: StepFormValues[];
}

interface StepFormValues {
    description: string;
    ingredients: IngredientFormValues[];
}

interface IngredientFormValues {
    food: Product | null;
    amount: number | null;
    foodName: string | null;
}

type RecipeFormData = FormGroupControls<RecipeFormValues>;
type StepFormData = FormGroupControls<StepFormValues>;
type IngredientFormData = FormGroupControls<IngredientFormValues>;




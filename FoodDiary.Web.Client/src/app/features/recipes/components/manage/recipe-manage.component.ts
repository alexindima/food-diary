import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, type FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { ManageHeaderComponent } from '../../../../components/shared/manage-header/manage-header.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { checkMacrosError } from '../../../../shared/lib/nutrition-form.utils';
import type { NutrientData } from '../../../../shared/models/charts.data';
import type { Product } from '../../../products/models/product.data';
import { RecipeManageFacade, type RecipeNutritionSummary } from '../../lib/recipe-manage.facade';
import type { Recipe, RecipeDto } from '../../models/recipe.data';
import { RecipeBasicInfoComponent } from './recipe-basic-info/recipe-basic-info.component';
import type {
    IngredientFormData,
    NutritionMode,
    NutritionScaleMode,
    RecipeFormData,
    RecipeFormValues,
    StepFormData,
    StepFormValues,
} from './recipe-manage.types';
import {
    buildRecipeDto,
    buildRecipeFormPatchValue,
    createRecipeForm,
    createRecipeIngredientGroup,
    createRecipeStepGroup,
    hasNoRecipeNutritionTotals,
    mapRecipeStepToFormValue,
} from './recipe-manage-form.mapper';
import { RecipeNutritionEditorComponent } from './recipe-nutrition-editor/recipe-nutrition-editor.component';
import { RecipeStepsListComponent, type StepIngredientEvent } from './recipe-steps-list/recipe-steps-list.component';

@Component({
    selector: 'fd-recipe-manage',
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiButtonComponent,
        ManageHeaderComponent,
        FdPageContainerDirective,
        RecipeBasicInfoComponent,
        RecipeNutritionEditorComponent,
        RecipeStepsListComponent,
    ],
    templateUrl: './recipe-manage.component.html',
    styleUrls: ['./recipe-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [RecipeManageFacade],
})
export class RecipeManageComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly expandedSteps = new Set<number>();
    private lastRecipe: Recipe | null = null;

    private readonly recipeManageFacade = inject(RecipeManageFacade);

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

    public recipeForm: FormGroup<RecipeFormData>;
    public readonly nutritionMode = signal<NutritionMode>('auto');
    public readonly manageHeaderState = computed<RecipeManageHeaderState>(() => {
        const isEdit = this.recipe() !== null;

        return {
            titleKey: isEdit ? 'RECIPE_MANAGE.EDIT_TITLE' : 'RECIPE_MANAGE.ADD_TITLE',
            submitLabelKey: isEdit ? 'RECIPE_MANAGE.SAVE_BUTTON' : 'RECIPE_MANAGE.ADD_BUTTON',
        };
    });
    public nutritionScaleMode: NutritionScaleMode = 'recipe';

    private isFormReady = true;

    public constructor() {
        this.recipeForm = createRecipeForm();
        this.nutritionMode.set(this.recipeForm.controls.calculateNutritionAutomatically.value ? 'auto' : 'manual');

        this.addStep();
        this.setupFormValueChangeTracking();
        this.recalculateNutrientsFromForm();
        this.updateManualNutritionValidators(this.recipeForm.controls.calculateNutritionAutomatically.value);

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
        });
        effect(() => {
            const recipe = this.recipe();
            untracked(() => {
                if (recipe !== null) {
                    if (this.lastRecipe !== recipe) {
                        this.lastRecipe = recipe;
                        this.populateForm(recipe);
                    }
                } else {
                    this.lastRecipe = null;
                    this.updateNutrientSummary(null);
                }
            });
        });
    }

    public get steps(): FormArray<FormGroup<StepFormData>> {
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

    public toggleStepExpanded(index: number): void {
        if (this.expandedSteps.has(index)) {
            this.expandedSteps.delete(index);
            return;
        }

        this.expandedSteps.add(index);
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

    private hasMacrosError(): boolean {
        if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
            return false;
        }

        return checkMacrosError([
            this.recipeForm.controls.manualProteins,
            this.recipeForm.controls.manualFats,
            this.recipeForm.controls.manualCarbs,
            this.recipeForm.controls.manualAlcohol,
        ]);
    }

    // -- Form submission --

    public onSubmit(): void {
        this.markFormGroupTouched(this.recipeForm);

        if (this.hasMacrosError()) {
            return;
        }

        if (!this.recipeForm.valid) {
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

        return buildRecipeDto(formValue, this.nutritionScaleMode, this.getServingsValue(), (value, scaleMode, servings) =>
            this.recipeManageFacade.toRecipeTotal(value, scaleMode, servings),
        );
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
        return buildRecipeFormPatchValue(recipeData);
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
        return mapRecipeStepToFormValue(step, {
            selectIngredient: this.translateService.instant('RECIPE_MANAGE.SELECT_INGREDIENT'),
            unknownProduct: this.translateService.instant('RECIPE_MANAGE.UNKNOWN_PRODUCT'),
        });
    }

    private hasNoRecipeNutritionTotals(recipeData: Recipe): boolean {
        return hasNoRecipeNutritionTotals(recipeData);
    }

    private resetSteps(): void {
        while (this.steps.length > 0) {
            this.steps.removeAt(0);
        }
    }

    private createStepGroup(step?: StepFormValues): FormGroup<StepFormData> {
        return createRecipeStepGroup(step);
    }

    private createIngredientGroup(
        food: Product | null = null,
        amount: number | null = null,
        nestedRecipeId: string | null = null,
        nestedRecipeName: string | null = null,
    ): FormGroup<IngredientFormData> {
        return createRecipeIngredientGroup(food, amount, nestedRecipeId, nestedRecipeName);
    }

    // -- Nutrition calculation --

    private setupFormValueChangeTracking(): void {
        this.recipeForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            if (!this.isFormReady) {
                return;
            }
            this.updateSummaryFromForm();
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
}

type RecipeManageHeaderState = {
    titleKey: string;
    submitLabelKey: string;
};

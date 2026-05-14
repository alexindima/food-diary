import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { ManageHeaderComponent } from '../../../../../components/shared/manage-header/manage-header.component';
import { FdPageContainerDirective } from '../../../../../directives/layout/page-container.directive';
import { RecipeManageFacade, type RecipeNutritionSummary } from '../../../lib/recipe-manage.facade';
import type { Recipe, RecipeDto } from '../../../models/recipe.data';
import { RecipeBasicInfoComponent } from '../recipe-basic-info/recipe-basic-info.component';
import type { NutritionScaleMode, RecipeFormData, RecipeFormValues, StepFormData } from '../recipe-manage-lib/recipe-manage.types';
import {
    buildRecipeDto,
    buildRecipeFormPatchValue,
    createRecipeForm,
    hasNoRecipeNutritionTotals,
} from '../recipe-manage-lib/recipe-manage-form.mapper';
import { RecipeNutritionFormManager } from '../recipe-manage-lib/recipe-nutrition-form.manager';
import { RecipeStepFormManager } from '../recipe-manage-lib/recipe-step-form.manager';
import { RecipeNutritionEditorComponent } from '../recipe-nutrition-editor/recipe-nutrition-editor.component';
import { RecipeStepsListComponent, type StepIngredientEvent } from '../recipe-steps-list/recipe-steps-list.component';

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
    private readonly stepFormManager: RecipeStepFormManager;
    private readonly nutritionFormManager: RecipeNutritionFormManager;
    private lastRecipe: Recipe | null = null;

    private readonly recipeManageFacade = inject(RecipeManageFacade);

    public readonly recipe = input<Recipe | null>(null);
    public globalError = this.recipeManageFacade.globalError;
    public isSubmitting = this.recipeManageFacade.isSubmitting;

    public recipeForm: FormGroup<RecipeFormData>;
    public readonly manageHeaderState = computed<RecipeManageHeaderState>(() => {
        const isEdit = this.recipe() !== null;

        return {
            titleKey: isEdit ? 'RECIPE_MANAGE.EDIT_TITLE' : 'RECIPE_MANAGE.ADD_TITLE',
            submitLabelKey: isEdit ? 'RECIPE_MANAGE.SAVE_BUTTON' : 'RECIPE_MANAGE.ADD_BUTTON',
        };
    });
    public readonly totalCalories: RecipeNutritionFormManager['totalCalories'];
    public readonly totalFiber: RecipeNutritionFormManager['totalFiber'];
    public readonly totalAlcohol: RecipeNutritionFormManager['totalAlcohol'];
    public readonly nutrientChartData: RecipeNutritionFormManager['nutrientChartData'];
    public readonly nutritionMode: RecipeNutritionFormManager['nutritionMode'];

    private isFormReady = true;

    public constructor() {
        this.recipeForm = createRecipeForm();
        this.stepFormManager = new RecipeStepFormManager(this.recipeForm.controls.steps, () => ({
            selectIngredient: this.translateService.instant('RECIPE_MANAGE.SELECT_INGREDIENT'),
            unknownProduct: this.translateService.instant('RECIPE_MANAGE.UNKNOWN_PRODUCT'),
        }));
        this.nutritionFormManager = new RecipeNutritionFormManager(this.recipeForm, {
            calculateAutoSummary: (steps): RecipeNutritionSummary => this.recipeManageFacade.calculateAutoSummary(steps),
            fromRecipeTotal: (value, scaleMode, servings): number => this.recipeManageFacade.fromRecipeTotal(value, scaleMode, servings),
            getSummaryFromRecipe: (recipeData, fallback): RecipeNutritionSummary =>
                this.recipeManageFacade.getSummaryFromRecipe(recipeData, fallback),
            roundNutritionValue: (value): number => this.recipeManageFacade.roundNutritionValue(value),
            toRecipeTotal: (value, scaleMode, servings): number => this.recipeManageFacade.toRecipeTotal(value, scaleMode, servings),
        });
        this.totalCalories = this.nutritionFormManager.totalCalories;
        this.totalFiber = this.nutritionFormManager.totalFiber;
        this.totalAlcohol = this.nutritionFormManager.totalAlcohol;
        this.nutrientChartData = this.nutritionFormManager.nutrientChartData;
        this.nutritionMode = this.nutritionFormManager.nutritionMode;

        this.addStep();
        this.setupFormValueChangeTracking();
        this.nutritionFormManager.initialize();

        this.recipeForm.controls.calculateNutritionAutomatically.valueChanges.pipe(takeUntilDestroyed()).subscribe(isAuto => {
            if (!this.isFormReady) {
                return;
            }
            this.nutritionFormManager.handleAutoCalculationChange(isAuto);
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
                    this.nutritionFormManager.updateNutrientSummary(null);
                }
            });
        });
    }

    public get steps(): FormArray<FormGroup<StepFormData>> {
        return this.recipeForm.controls.steps;
    }

    public get expandedStepsSet(): Set<number> {
        return this.stepFormManager.expandedSteps;
    }

    // -- Step management (delegated from steps-list) --

    public addStep(): void {
        this.stepFormManager.addStep();
    }

    public removeStep(index: number): void {
        this.stepFormManager.removeStep(index);
    }

    public addIngredientToStep(stepIndex: number): void {
        this.stepFormManager.addIngredientToStep(stepIndex);
    }

    public toggleStepExpanded(index: number): void {
        this.stepFormManager.toggleStepExpanded(index);
    }

    public removeIngredientFromStep(event: StepIngredientEvent): void {
        this.stepFormManager.removeIngredientFromStep(event);
    }

    public onProductSelectClick(event: StepIngredientEvent): void {
        const { stepIndex, ingredientIndex } = event;
        this.recipeManageFacade.openItemSelectionDialog().subscribe(selection => {
            if (selection === null) {
                return;
            }
            const foodGroup = this.stepFormManager.getIngredientGroup({ stepIndex, ingredientIndex });
            this.recipeManageFacade.applyItemSelection(foodGroup, selection);
            if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
                this.nutritionFormManager.recalculateNutrientsFromForm();
            }
        });
    }

    // -- Nutrition mode --

    public onNutritionModeChange(nextMode: string): void {
        this.nutritionFormManager.onNutritionModeChange(nextMode);
    }

    public onNutritionScaleModeChange(nextMode: string): void {
        this.nutritionFormManager.onNutritionScaleModeChange(nextMode);
    }

    // -- Form submission --

    public onSubmit(): void {
        this.markFormGroupTouched(this.recipeForm);

        if (this.nutritionFormManager.hasMacrosError()) {
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

        return buildRecipeDto(
            formValue,
            this.nutritionScaleMode,
            this.nutritionFormManager.getServingsValue(),
            (value, scaleMode, servings) => this.recipeManageFacade.toRecipeTotal(value, scaleMode, servings),
        );
    }

    private populateForm(recipeData: Recipe): void {
        this.isFormReady = false;
        this.recipeForm.patchValue(this.buildRecipeFormPatchValue(recipeData));

        this.stepFormManager.resetSteps();
        this.stepFormManager.populateRecipeSteps(recipeData);

        this.nutritionFormManager.updateNutrientSummary(recipeData);
        this.isFormReady = true;
        if (this.hasNoRecipeNutritionTotals(recipeData)) {
            this.nutritionFormManager.recalculateNutrientsFromForm();
        } else {
            this.nutritionFormManager.updateSummaryFromForm();
        }
    }

    private buildRecipeFormPatchValue(recipeData: Recipe): Partial<RecipeFormValues> {
        return buildRecipeFormPatchValue(recipeData);
    }

    private hasNoRecipeNutritionTotals(recipeData: Recipe): boolean {
        return hasNoRecipeNutritionTotals(recipeData);
    }

    // -- Nutrition calculation --

    private setupFormValueChangeTracking(): void {
        this.recipeForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            if (!this.isFormReady) {
                return;
            }
            this.nutritionFormManager.updateSummaryFromForm();
        });

        this.recipeForm.controls.steps.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            if (!this.isFormReady) {
                return;
            }
            if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
                this.nutritionFormManager.recalculateNutrientsFromForm();
            }
        });
    }

    public get nutritionScaleMode(): NutritionScaleMode {
        return this.nutritionFormManager.nutritionScaleMode;
    }
}

type RecipeManageHeaderState = {
    titleKey: string;
    submitLabelKey: string;
};

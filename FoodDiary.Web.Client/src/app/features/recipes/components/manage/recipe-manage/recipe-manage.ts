import { moveItemInArray } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormGroup } from '@angular/forms';
import { form, min, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { ManageHeaderComponent } from '../../../../../components/shared/manage-header/manage-header';
import { FdPageContainerDirective } from '../../../../../shared/ui/layout/page-container.directive';
import { RecipeManageFacade, type RecipeNutritionSummary } from '../../../lib/recipe-manage.facade';
import type { Recipe, RecipeDto } from '../../../models/recipe.data';
import { RecipeBasicInfoComponent } from '../recipe-basic-info/recipe-basic-info';
import type { NutritionScaleMode, RecipeFormData, RecipeFormValues, StepFormData } from '../recipe-manage-lib/recipe-manage.types';
import {
    buildRecipeDto,
    buildRecipeFormPatchValue,
    createRecipeForm,
    createRecipeFormValue,
    hasNoRecipeNutritionTotals,
} from '../recipe-manage-lib/recipe-manage-form.mapper';
import { RecipeNutritionFormManager } from '../recipe-manage-lib/recipe-nutrition-form.manager';
import { RecipeStepFormManager } from '../recipe-manage-lib/recipe-step-form.manager';
import { RecipeNutritionEditorComponent } from '../recipe-nutrition-editor/recipe-nutrition-editor';
import {
    type RecipeStepListItem,
    RecipeStepsListComponent,
    type StepDropEvent,
    type StepIngredientEvent,
} from '../recipe-steps-list/recipe-steps-list';

@Component({
    selector: 'fd-recipe-manage',
    imports: [
        TranslatePipe,
        FdUiButtonComponent,
        ManageHeaderComponent,
        FdPageContainerDirective,
        RecipeBasicInfoComponent,
        RecipeNutritionEditorComponent,
        RecipeStepsListComponent,
    ],
    templateUrl: './recipe-manage.html',
    styleUrls: ['./recipe-manage.scss'],
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
    protected globalError = this.recipeManageFacade.globalError;
    protected isSubmitting = this.recipeManageFacade.isSubmitting;
    protected readonly stepsRenderVersion = signal(0);

    protected recipeForm: FormGroup<RecipeFormData> = createRecipeForm();
    protected readonly recipeFormModel = signal<RecipeFormValues>(createRecipeFormValue());
    protected readonly recipeSignalForm = form(this.recipeFormModel, path => {
        required(path.name);
        min(path.prepTime, 0);
        required(path.cookTime);
        min(path.cookTime, 1);
        required(path.servings);
        min(path.servings, 1);
        required(path.visibility);
        min(path.manualCalories, 0);
        min(path.manualProteins, 0);
        min(path.manualFats, 0);
        min(path.manualCarbs, 0);
        min(path.manualFiber, 0);
        min(path.manualAlcohol, 0);
    });
    protected readonly manageHeaderState = computed<RecipeManageHeaderState>(() => {
        const isEdit = this.recipe() !== null;

        return {
            titleKey: isEdit ? 'RECIPE_MANAGE.EDIT_TITLE' : 'RECIPE_MANAGE.ADD_TITLE',
            submitLabelKey: isEdit ? 'RECIPE_MANAGE.SAVE_BUTTON' : 'RECIPE_MANAGE.ADD_BUTTON',
        };
    });
    protected readonly stepListItems = computed<readonly RecipeStepListItem[]>(() => {
        this.stepsRenderVersion();
        return this.steps.controls.map(stepForm => ({ form: stepForm }));
    });
    protected readonly stepsError = computed(() => {
        this.stepsRenderVersion();
        return this.steps.invalid && this.steps.touched ? this.translateService.instant('FORM_ERRORS.NON_EMPTY_ARRAY') : null;
    });
    protected readonly totalCalories: RecipeNutritionFormManager['totalCalories'];
    protected readonly totalFiber: RecipeNutritionFormManager['totalFiber'];
    protected readonly totalAlcohol: RecipeNutritionFormManager['totalAlcohol'];
    protected readonly nutrientChartData: RecipeNutritionFormManager['nutrientChartData'];
    protected readonly nutritionMode: RecipeNutritionFormManager['nutritionMode'];

    private isFormReady = true;

    public constructor() {
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
        this.syncSignalManagedValuesToLegacyForm();
        this.setupFormValueChangeTracking();
        this.watchSignalFormModelChanges();
        this.nutritionFormManager.initialize();
        this.syncLegacyNutritionValuesToSignalForm();

        this.recipeForm.controls.calculateNutritionAutomatically.valueChanges.pipe(takeUntilDestroyed()).subscribe(isAuto => {
            if (!this.isFormReady) {
                return;
            }
            this.nutritionFormManager.handleAutoCalculationChange(isAuto);
            this.syncLegacyNutritionValuesToSignalForm();
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

    protected get steps(): FormArray<FormGroup<StepFormData>> {
        return this.recipeForm.controls.steps;
    }

    protected get expandedStepsSet(): Set<number> {
        return this.stepFormManager.expandedSteps;
    }

    // -- Step management (delegated from steps-list) --

    protected addStep(): void {
        this.stepFormManager.addStep();
        this.bumpStepsRenderVersion();
    }

    protected removeStep(index: number): void {
        this.stepFormManager.removeStep(index);
        this.bumpStepsRenderVersion();
    }

    protected addIngredientToStep(stepIndex: number): void {
        this.stepFormManager.addIngredientToStep(stepIndex);
        this.bumpStepsRenderVersion();
    }

    protected toggleStepExpanded(index: number): void {
        this.stepFormManager.toggleStepExpanded(index);
    }

    protected removeIngredientFromStep(event: StepIngredientEvent): void {
        this.stepFormManager.removeIngredientFromStep(event);
        this.bumpStepsRenderVersion();
    }

    protected onStepDrop(event: StepDropEvent): void {
        moveItemInArray(this.steps.controls, event.previousIndex, event.currentIndex);
        this.steps.updateValueAndValidity();
        this.steps.markAsDirty();
        this.bumpStepsRenderVersion();
    }

    protected onProductSelectClick(event: StepIngredientEvent): void {
        const { stepIndex, ingredientIndex } = event;
        this.recipeManageFacade.openItemSelectionDialog(this.recipe()?.id ?? null).subscribe(selection => {
            if (selection === null) {
                return;
            }
            const foodGroup = this.stepFormManager.getIngredientGroup({ stepIndex, ingredientIndex });
            this.recipeManageFacade.applyItemSelection(foodGroup, selection);
            if (this.recipeForm.controls.calculateNutritionAutomatically.value) {
                this.nutritionFormManager.recalculateNutrientsFromForm();
                this.syncLegacyNutritionValuesToSignalForm();
            }
        });
    }

    // -- Nutrition mode --

    protected onNutritionModeChange(nextMode: string): void {
        this.nutritionFormManager.onNutritionModeChange(nextMode);
        this.syncLegacyNutritionValuesToSignalForm();
    }

    protected onNutritionScaleModeChange(nextMode: string): void {
        this.nutritionFormManager.onNutritionScaleModeChange(nextMode);
        this.syncLegacyNutritionValuesToSignalForm();
    }

    // -- Form submission --

    protected onSubmit(): void {
        this.recipeSignalForm().markAsTouched();
        this.syncSignalManagedValuesToLegacyForm();
        this.markFormGroupTouched(this.recipeForm);
        this.bumpStepsRenderVersion();

        if (this.nutritionFormManager.hasMacrosError()) {
            return;
        }

        if (this.recipeSignalForm().invalid() || !this.recipeForm.valid) {
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

    protected async onCancelAsync(): Promise<void> {
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
        const formPatchValue = this.buildRecipeFormPatchValue(recipeData);
        this.recipeForm.patchValue(formPatchValue);
        this.patchRecipeFormModel(formPatchValue);

        this.stepFormManager.resetSteps();
        this.stepFormManager.populateRecipeSteps(recipeData);
        this.bumpStepsRenderVersion();

        this.nutritionFormManager.updateNutrientSummary(recipeData);
        this.isFormReady = true;
        if (this.hasNoRecipeNutritionTotals(recipeData)) {
            this.nutritionFormManager.recalculateNutrientsFromForm();
        } else {
            this.nutritionFormManager.updateSummaryFromForm();
        }
        this.syncLegacyNutritionValuesToSignalForm();
    }

    private bumpStepsRenderVersion(): void {
        this.stepsRenderVersion.update(version => version + 1);
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
                this.syncLegacyNutritionValuesToSignalForm();
            }
        });
    }

    private watchSignalFormModelChanges(): void {
        effect(() => {
            const value = this.pickSignalManagedFormValue(this.recipeFormModel());

            untracked(() => {
                if (!this.isFormReady) {
                    return;
                }

                this.recipeForm.patchValue(value, { emitEvent: false });
                this.nutritionFormManager.updateSummaryFromForm();
                this.syncLegacyNutritionValuesToSignalForm();
            });
        });
    }

    private syncSignalManagedValuesToLegacyForm(): void {
        this.recipeForm.patchValue(this.pickSignalManagedFormValue(this.recipeFormModel()), { emitEvent: false });
    }

    private patchRecipeFormModel(value: Partial<RecipeFormValues>): void {
        this.recipeFormModel.update(current => {
            const changedValue = this.pickChangedSignalManagedFormValue(current, {
                ...current,
                ...value,
            });
            return Object.keys(changedValue).length === 0
                ? current
                : {
                      ...current,
                      ...changedValue,
                  };
        });
    }

    private pickChangedSignalManagedFormValue(current: RecipeFormValues, next: RecipeFormValues): Partial<RecipeFormValues> {
        const nextSignalValue = this.pickSignalManagedFormValue(next);
        return Object.fromEntries(
            recipeSignalManagedFields
                .filter(field => current[field] !== nextSignalValue[field])
                .map(field => [field, nextSignalValue[field]]),
        );
    }

    private pickSignalManagedFormValue(value: RecipeFormValues): Pick<RecipeFormValues, RecipeSignalManagedField> {
        return {
            name: value.name,
            description: value.description,
            comment: value.comment,
            category: value.category,
            imageUrl: value.imageUrl,
            prepTime: value.prepTime,
            cookTime: value.cookTime,
            servings: value.servings,
            visibility: value.visibility,
            calculateNutritionAutomatically: value.calculateNutritionAutomatically,
            manualCalories: value.manualCalories,
            manualProteins: value.manualProteins,
            manualFats: value.manualFats,
            manualCarbs: value.manualCarbs,
            manualFiber: value.manualFiber,
            manualAlcohol: value.manualAlcohol,
        };
    }

    private syncLegacyNutritionValuesToSignalForm(): void {
        this.patchRecipeFormModel({
            calculateNutritionAutomatically: this.recipeForm.controls.calculateNutritionAutomatically.value,
            manualCalories: this.recipeForm.controls.manualCalories.value,
            manualProteins: this.recipeForm.controls.manualProteins.value,
            manualFats: this.recipeForm.controls.manualFats.value,
            manualCarbs: this.recipeForm.controls.manualCarbs.value,
            manualFiber: this.recipeForm.controls.manualFiber.value,
            manualAlcohol: this.recipeForm.controls.manualAlcohol.value,
        });
    }

    protected get nutritionScaleMode(): NutritionScaleMode {
        return this.nutritionFormManager.nutritionScaleMode;
    }
}

type RecipeSignalManagedField =
    | 'name'
    | 'description'
    | 'comment'
    | 'category'
    | 'imageUrl'
    | 'prepTime'
    | 'cookTime'
    | 'servings'
    | 'visibility'
    | 'calculateNutritionAutomatically'
    | 'manualCalories'
    | 'manualProteins'
    | 'manualFats'
    | 'manualCarbs'
    | 'manualFiber'
    | 'manualAlcohol';

const recipeSignalManagedFields = [
    'name',
    'description',
    'comment',
    'category',
    'imageUrl',
    'prepTime',
    'cookTime',
    'servings',
    'visibility',
    'calculateNutritionAutomatically',
    'manualCalories',
    'manualProteins',
    'manualFats',
    'manualCarbs',
    'manualFiber',
    'manualAlcohol',
] as const satisfies readonly RecipeSignalManagedField[];

type RecipeManageHeaderState = {
    titleKey: string;
    submitLabelKey: string;
};

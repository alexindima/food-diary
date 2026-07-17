import { moveItemInArray } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormRoot, max, min, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { PageBodyComponent } from '../../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../../components/shared/page-header/page-header';
import { createCollectionTouchedState } from '../../../../../shared/lib/collection-touched-state.utils';
import { MANUAL_NUTRITION_MAX_CALORIES, MANUAL_NUTRITION_MAX_NUTRIENT } from '../../../../../shared/lib/nutrition.constants';
import { patchSignalFormModel } from '../../../../../shared/lib/signal-form-model.utils';
import { LocalizedTourDefinitionService } from '../../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../../shared/ui/layout/page-container.directive';
import { RecipeManageFacade, type RecipeNutritionSummary } from '../../../lib/recipe-manage.facade';
import type { Recipe, RecipeDto } from '../../../models/recipe.data';
import { RecipeBasicInfoComponent } from '../recipe-basic-info/recipe-basic-info';
import { parseRecipeImportDraft } from '../recipe-manage-lib/recipe-import-draft.mapper';
import type { IngredientFormValues, NutritionScaleMode, RecipeFormValues, StepFormValues } from '../recipe-manage-lib/recipe-manage.types';
import {
    buildRecipeDto,
    buildRecipeFormPatchValue,
    createRecipeFormValue,
    hasNoRecipeNutritionTotals,
    RECIPE_MIN_INGREDIENT_AMOUNT,
} from '../recipe-manage-lib/recipe-manage-form.mapper';
import { RecipeNutritionFormManager } from '../recipe-manage-lib/recipe-nutrition-form.manager';
import { RecipeStepFormManager } from '../recipe-manage-lib/recipe-step-form.manager';
import { RecipeNutritionEditorComponent } from '../recipe-nutrition-editor/recipe-nutrition-editor';
import {
    type RecipeStepListItem,
    RecipeStepsListComponent,
    type StepDropEvent,
    type StepFieldEvent,
    type StepIngredientAmountEvent,
    type StepIngredientEvent,
    type StepIngredientSelectEvent,
} from '../recipe-steps-list/recipe-steps-list';
import { RECIPE_MANAGE_TOUR } from './recipe-manage-tour';

@Component({
    selector: 'fd-recipe-manage',
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiInputComponent,
        FdUiTextareaComponent,
        FormRoot,
        PageBodyComponent,
        PageHeaderComponent,
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
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    private readonly stepFormManager: RecipeStepFormManager;
    private readonly nutritionFormManager: RecipeNutritionFormManager;
    private lastRecipe: Recipe | null = null;

    private readonly recipeManageFacade = inject(RecipeManageFacade);
    private readonly languageVersion = signal(0);
    private readonly stepsTouchedState = createCollectionTouchedState({
        hasItems: () => this.steps.length > 0,
        errorMessage: () => this.translateService.instant('FORM_ERRORS.NON_EMPTY_ARRAY'),
        dependencies: [this.languageVersion],
    });

    public readonly recipe = input<Recipe | null>(null);
    protected globalError = this.recipeManageFacade.globalError;
    protected isSubmitting = this.recipeManageFacade.isSubmitting;
    protected readonly stepsTouched = this.stepsTouchedState.touched;
    protected readonly importUrl = signal('');
    protected readonly importText = signal('');
    protected readonly importErrorKey = signal<string | null>(null);
    protected readonly isImportPanelVisible = computed(() => this.recipe() === null);

    protected readonly recipeFormModel = signal<RecipeFormValues>(createRecipeFormValue());
    protected readonly recipeSignalForm = form(this.recipeFormModel, path => {
        required(path.name);
        min(path.prepTime, 0);
        min(path.cookTime, 1);
        required(path.servings);
        min(path.servings, 1);
        required(path.visibility);
        min(path.manualCalories, 0);
        max(path.manualCalories, MANUAL_NUTRITION_MAX_CALORIES);
        min(path.manualProteins, 0);
        max(path.manualProteins, MANUAL_NUTRITION_MAX_NUTRIENT);
        min(path.manualFats, 0);
        max(path.manualFats, MANUAL_NUTRITION_MAX_NUTRIENT);
        min(path.manualCarbs, 0);
        max(path.manualCarbs, MANUAL_NUTRITION_MAX_NUTRIENT);
        min(path.manualFiber, 0);
        max(path.manualFiber, MANUAL_NUTRITION_MAX_NUTRIENT);
        min(path.manualAlcohol, 0);
        max(path.manualAlcohol, MANUAL_NUTRITION_MAX_NUTRIENT);
    });
    protected readonly manageHeaderState = computed<RecipeManageHeaderState>(() => {
        const isEdit = this.recipe() !== null;

        return {
            titleKey: isEdit ? 'RECIPE_MANAGE.EDIT_TITLE' : 'RECIPE_MANAGE.ADD_TITLE',
            submitLabelKey: isEdit ? 'RECIPE_MANAGE.SAVE_BUTTON' : 'RECIPE_MANAGE.ADD_BUTTON',
        };
    });
    protected readonly stepListItems = computed<readonly RecipeStepListItem[]>(() => {
        const touched = this.stepsTouched();
        return this.steps.map(step => ({ state: this.createStepCardState(step, touched) }));
    });
    protected readonly stepsError = this.stepsTouchedState.error;
    protected readonly totalCalories: RecipeNutritionFormManager['totalCalories'];
    protected readonly totalFiber: RecipeNutritionFormManager['totalFiber'];
    protected readonly totalAlcohol: RecipeNutritionFormManager['totalAlcohol'];
    protected readonly nutrientChartData: RecipeNutritionFormManager['nutrientChartData'];
    protected readonly nutritionMode: RecipeNutritionFormManager['nutritionMode'];

    private summaryUpdatesSuspended = false;

    public constructor() {
        this.stepFormManager = new RecipeStepFormManager(
            () => this.steps,
            steps => {
                this.patchRecipeFormModel({ steps });
            },
            () => ({
                selectIngredient: this.translateService.instant('RECIPE_MANAGE.SELECT_INGREDIENT'),
                unknownProduct: this.translateService.instant('RECIPE_MANAGE.UNKNOWN_PRODUCT'),
            }),
        );
        this.nutritionFormManager = new RecipeNutritionFormManager(this.createNutritionFormAdapter(), {
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

        this.watchLanguageChanges();
        this.addStep();
        this.watchSignalFormModelChanges();
        this.nutritionFormManager.initialize();
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

    protected get steps(): readonly StepFormValues[] {
        return this.recipeFormModel().steps;
    }

    protected get expandedStepsSet(): Set<number> {
        return this.stepFormManager.expandedSteps;
    }

    // -- Step management (delegated from steps-list) --

    protected addStep(): void {
        this.stepFormManager.addStep();
        this.updateSummaryFromCurrentForm();
    }

    protected removeStep(index: number): void {
        this.stepFormManager.removeStep(index);
        this.stepsTouchedState.markTouched();
        this.updateSummaryFromCurrentForm();
    }

    protected addIngredientToStep(stepIndex: number): void {
        this.stepFormManager.addIngredientToStep(stepIndex);
        this.stepsTouchedState.markTouched();
        this.updateSummaryFromCurrentForm();
    }

    protected toggleStepExpanded(index: number): void {
        this.stepFormManager.toggleStepExpanded(index);
    }

    protected removeIngredientFromStep(event: StepIngredientEvent): void {
        this.stepFormManager.removeIngredientFromStep(event);
        this.stepsTouchedState.markTouched();
        this.updateSummaryFromCurrentForm();
    }

    protected onStepDrop(event: StepDropEvent): void {
        const steps = [...this.steps];
        moveItemInArray(steps, event.previousIndex, event.currentIndex);
        this.patchRecipeFormModel({ steps });
        this.stepsTouchedState.markTouched();
        this.updateSummaryFromCurrentForm();
    }

    protected onProductSelectClick(event: StepIngredientSelectEvent): void {
        const { stepIndex, ingredientIndex, itemType } = event;
        this.recipeManageFacade.openItemSelectionDialog(itemType, this.recipe()?.id ?? null).subscribe(selection => {
            if (selection === null) {
                return;
            }
            this.recipeManageFacade.applyItemSelection(
                {
                    patchValue: value => {
                        this.stepFormManager.patchIngredient({ stepIndex, ingredientIndex }, value);
                    },
                },
                selection,
            );
            this.stepsTouchedState.markTouched();
            this.updateSummaryFromCurrentForm();
        });
    }

    protected onStepTitleChange(event: StepFieldEvent<string | null>): void {
        this.patchStep(event.stepIndex, { title: event.value });
    }

    protected onStepImageChange(event: StepFieldEvent<StepFormValues['imageUrl']>): void {
        this.patchStep(event.stepIndex, { imageUrl: event.value });
    }

    protected onStepDescriptionChange(event: StepFieldEvent<string>): void {
        this.patchStep(event.stepIndex, { description: event.value });
    }

    protected onIngredientAmountChange(event: StepIngredientAmountEvent): void {
        this.stepFormManager.patchIngredient(
            { stepIndex: event.stepIndex, ingredientIndex: event.ingredientIndex },
            { amount: event.amount },
        );
        this.stepsTouchedState.markTouched();
        this.updateSummaryFromCurrentForm();
    }

    // -- Nutrition mode --

    protected onNutritionModeChange(nextMode: string): void {
        this.nutritionFormManager.onNutritionModeChange(nextMode);
        this.nutritionFormManager.handleAutoCalculationChange(this.recipeFormModel().calculateNutritionAutomatically);
        this.updateSummaryFromCurrentForm();
    }

    protected onNutritionScaleModeChange(nextMode: string): void {
        this.nutritionFormManager.onNutritionScaleModeChange(nextMode);
        this.updateSummaryFromCurrentForm();
    }

    // -- Form submission --

    protected onFormSubmit(event: SubmitEvent): void {
        event.preventDefault();
        this.onSubmit();
    }

    protected startRecipeManageTour(force = true): void {
        this.tourService.start(this.localizedTour.build(RECIPE_MANAGE_TOUR), { force });
    }

    protected onSubmit(): void {
        if (this.isSubmitting()) {
            return;
        }

        this.recipeSignalForm().markAsTouched();
        this.stepsTouchedState.markTouched();

        if (this.nutritionFormManager.hasMacrosError()) {
            return;
        }

        if (this.recipeSignalForm().invalid() || !this.areStepsValid()) {
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
        if (this.recipeSignalForm().dirty()) {
            const shouldLeave = await this.recipeManageFacade.confirmDiscardChangesAsync({
                title: this.translateService.instant('UNSAVED_CHANGES.TITLE'),
                message: this.translateService.instant('UNSAVED_CHANGES.MESSAGE'),
                confirmLabel: this.translateService.instant('UNSAVED_CHANGES.DISCARD'),
                cancelLabel: this.translateService.instant('UNSAVED_CHANGES.STAY'),
                confirmIcon: 'logout',
            });
            if (!shouldLeave) {
                return;
            }
        }

        await this.recipeManageFacade.cancelManageAsync();
    }

    protected onImportUrlChange(value: string | number | null): void {
        this.importUrl.set(String(value ?? ''));
        this.importErrorKey.set(null);
    }

    protected onImportTextChange(value: string | number | null): void {
        this.importText.set(String(value ?? ''));
        this.importErrorKey.set(null);
    }

    protected applyImportDraft(): void {
        const sourceUrl = this.importUrl().trim();
        const draft = parseRecipeImportDraft({
            text: this.importText(),
            sourceUrl,
            sourceLabel: this.translateService.instant('RECIPE_MANAGE.IMPORT.SOURCE_LABEL'),
            ingredientsLabel: this.translateService.instant('RECIPE_MANAGE.IMPORT.INGREDIENTS_LABEL'),
            existingIngredients: this.steps[0]?.ingredients ?? [],
        });

        if (draft === null) {
            this.importErrorKey.set('RECIPE_MANAGE.IMPORT.EMPTY_ERROR');
            return;
        }

        this.patchRecipeFormModel({
            name: draft.name,
            description: draft.description,
            comment: draft.comment,
            steps: draft.steps,
        });
        this.stepFormManager.expandedSteps.clear();
        draft.steps.forEach((_, index) => this.stepFormManager.expandedSteps.add(index));
        this.importErrorKey.set(null);
        this.updateSummaryFromCurrentForm();
    }

    private prepareRecipeDto(): RecipeDto {
        const formValue = this.recipeFormModel();

        return buildRecipeDto(
            formValue,
            this.nutritionScaleMode,
            this.nutritionFormManager.getServingsValue(),
            (value, scaleMode, servings) => this.recipeManageFacade.toRecipeTotal(value, scaleMode, servings),
        );
    }

    private populateForm(recipeData: Recipe): void {
        this.runWithSummaryUpdatesSuspended(() => {
            const formPatchValue = this.buildRecipeFormPatchValue(recipeData);
            this.patchRecipeFormModel(formPatchValue);

            this.stepFormManager.resetSteps();
            this.stepFormManager.populateRecipeSteps(recipeData);
            this.stepsTouchedState.reset();

            this.nutritionFormManager.updateNutrientSummary(recipeData);
        });
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

    private watchSignalFormModelChanges(): void {
        effect(() => {
            this.recipeFormModel();

            untracked(() => {
                if (this.summaryUpdatesSuspended) {
                    return;
                }

                this.updateSummaryFromCurrentForm();
            });
        });
    }

    private watchLanguageChanges(): void {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
    }

    private updateSummaryFromCurrentForm(): void {
        if (this.summaryUpdatesSuspended) {
            return;
        }

        this.nutritionFormManager.updateSummaryFromForm();
    }

    private runWithSummaryUpdatesSuspended(action: () => void): void {
        this.summaryUpdatesSuspended = true;
        try {
            action();
        } finally {
            this.summaryUpdatesSuspended = false;
        }
    }

    private patchRecipeFormModel(value: Partial<RecipeFormValues>): void {
        patchSignalFormModel(this.recipeFormModel, value);
    }

    private createNutritionFormAdapter(): RecipeNutritionFormAdapter {
        const getSteps = (): readonly StepFormValues[] => this.recipeFormModel().steps;
        return {
            controls: {
                calculateNutritionAutomatically: this.createNutritionControl('calculateNutritionAutomatically'),
                manualAlcohol: this.createNutritionControl('manualAlcohol'),
                manualCalories: this.createNutritionControl('manualCalories'),
                manualCarbs: this.createNutritionControl('manualCarbs'),
                manualFats: this.createNutritionControl('manualFats'),
                manualFiber: this.createNutritionControl('manualFiber'),
                manualProteins: this.createNutritionControl('manualProteins'),
                servings: this.createNutritionControl('servings'),
                get steps(): readonly StepFormValues[] {
                    return getSteps();
                },
            },
            patchValue: (value): void => {
                this.patchRecipeFormModel(value);
            },
        };
    }

    private createNutritionControl<Field extends RecipeNutritionField>(field: Field): RecipeNutritionValueControl<RecipeFormValues[Field]> {
        const getFieldState = (): RecipeSignalFieldState => this.recipeSignalForm[field]();
        const getValue = (): RecipeFormValues[Field] => this.recipeFormModel()[field];
        const setValue = (value: RecipeFormValues[Field]): void => {
            this.patchRecipeFormModel({ [field]: value });
        };
        return {
            get dirty(): boolean {
                return getFieldState().dirty();
            },
            get touched(): boolean {
                return getFieldState().touched();
            },
            get value(): RecipeFormValues[Field] {
                return getValue();
            },
            setValue,
        };
    }

    private createStepCardState(step: StepFormValues, touched: boolean): RecipeStepListItem['state'] {
        return {
            title: {
                value: step.title,
                error: null,
            },
            imageUrl: {
                value: step.imageUrl,
                error: null,
            },
            description: {
                value: step.description,
                error: touched && step.description.trim().length === 0 ? this.translateService.instant('FORM_ERRORS.REQUIRED') : null,
            },
            ingredients: step.ingredients.map(ingredient => this.createIngredientCardState(ingredient, touched)),
        };
    }

    private createIngredientCardState(
        ingredient: IngredientFormValues,
        touched: boolean,
    ): RecipeStepListItem['state']['ingredients'][number] {
        const hasFoodName = ingredient.foodName !== null && ingredient.foodName.trim().length > 0;
        return {
            amount: {
                value: ingredient.amount,
                error: this.getIngredientAmountError(ingredient, touched),
            },
            food: ingredient.food,
            foodName: {
                value: ingredient.foodName,
                error: touched && !hasFoodName ? this.translateService.instant('FORM_ERRORS.REQUIRED') : null,
            },
            nestedRecipeId: ingredient.nestedRecipeId,
        };
    }

    private getIngredientAmountError(ingredient: IngredientFormValues, touched: boolean): string | null {
        if (!touched) {
            return null;
        }

        if (ingredient.amount === null) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        if (ingredient.amount < RECIPE_MIN_INGREDIENT_AMOUNT) {
            return this.translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', {
                min: RECIPE_MIN_INGREDIENT_AMOUNT,
            });
        }

        return null;
    }

    protected areStepsValid(): boolean {
        return this.steps.length > 0 && this.steps.every(step => this.isStepValid(step));
    }

    private isStepValid(step: StepFormValues): boolean {
        return (
            step.description.trim().length > 0 &&
            step.ingredients.length > 0 &&
            step.ingredients.every(ingredient => this.isIngredientValid(ingredient))
        );
    }

    private isIngredientValid(ingredient: IngredientFormValues): boolean {
        return (
            ingredient.foodName !== null &&
            ingredient.foodName.trim().length > 0 &&
            ingredient.amount !== null &&
            ingredient.amount >= RECIPE_MIN_INGREDIENT_AMOUNT
        );
    }

    private patchStep(stepIndex: number, patch: Partial<StepFormValues>): void {
        this.patchRecipeFormModel({
            steps: this.steps.map((step, currentIndex) =>
                currentIndex === stepIndex
                    ? {
                          ...step,
                          ...patch,
                      }
                    : step,
            ),
        });
        this.stepsTouchedState.markTouched();
        this.updateSummaryFromCurrentForm();
    }

    protected get nutritionScaleMode(): NutritionScaleMode {
        return this.nutritionFormManager.nutritionScaleMode;
    }
}

type RecipeManageHeaderState = {
    titleKey: string;
    submitLabelKey: string;
};

type RecipeNutritionField =
    | 'calculateNutritionAutomatically'
    | 'manualAlcohol'
    | 'manualCalories'
    | 'manualCarbs'
    | 'manualFats'
    | 'manualFiber'
    | 'manualProteins'
    | 'servings';

type RecipeNutritionValueControl<T> = {
    readonly dirty: boolean;
    readonly touched: boolean;
    readonly value: T;
    setValue: (value: T) => void;
};

type RecipeSignalFieldState = {
    dirty: () => boolean;
    touched: () => boolean;
};

type RecipeNutritionFormAdapter = {
    controls: {
        calculateNutritionAutomatically: RecipeNutritionValueControl<boolean>;
        manualAlcohol: RecipeNutritionValueControl<number | null>;
        manualCalories: RecipeNutritionValueControl<number | null>;
        manualCarbs: RecipeNutritionValueControl<number | null>;
        manualFats: RecipeNutritionValueControl<number | null>;
        manualFiber: RecipeNutritionValueControl<number | null>;
        manualProteins: RecipeNutritionValueControl<number | null>;
        servings: RecipeNutritionValueControl<number>;
        readonly steps: readonly StepFormValues[];
    };
    patchValue: (value: Partial<RecipeFormValues>, options?: { emitEvent?: boolean }) => void;
};

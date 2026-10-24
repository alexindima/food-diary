import { moveItemInArray } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, max, maxLength, min, required, validate } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective, FdUiPreventUnloadDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiFormDirective } from 'fd-ui-kit/form/fd-ui-form.directive';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { PageBodyComponent } from '../../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../../components/shared/page-header/page-header';
import { connectDevelopmentScreen, DevelopmentToolError } from '../../../../../shared/development/development-tools';
import type { UnsavedEditor } from '../../../../../shared/forms/unsaved-editor';
import { createCollectionTouchedState } from '../../../../../shared/lib/collection-touched-state.utils';
import { MANUAL_NUTRITION_MAX_CALORIES, MANUAL_NUTRITION_MAX_NUTRIENT } from '../../../../../shared/lib/nutrition.constants';
import { patchSignalFormModel } from '../../../../../shared/lib/signal-form-model.utils';
import type { Recipe, RecipeDto } from '../../../../../shared/models/recipe.data';
import type { RecipeImportResult } from '../../../../../shared/models/recipe-import.data';
import { LocalizedTourDefinitionService } from '../../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../../shared/ui/layout/page-container.directive';
import { RecipeDevelopmentDraft, type RecipeDevelopmentIngredient } from '../../../contracts/recipe-development-draft';
import { detectRecipeLanguage, hasSubstantiallyChangedRecipeText, recipeLanguageText } from '../../../lib/recipe-language.utils';
import { RecipeManageFacade, type RecipeNutritionSummary } from '../../../lib/recipe-manage.facade';
import { RecipeBasicInfoComponent } from '../recipe-basic-info/recipe-basic-info';
import { RECIPE_DEVELOPMENT_SCHEMA } from '../recipe-manage-lib/recipe-development-schema';
import { parseRecipeImportDraft } from '../recipe-manage-lib/recipe-import-draft.mapper';
import type { IngredientFormValues, NutritionScaleMode, RecipeFormValues, StepFormValues } from '../recipe-manage-lib/recipe-manage.types';
import {
    buildRecipeDto,
    buildRecipeFormPatchValue,
    createRecipeFormValue,
    createRecipeIngredientValue,
    hasNoRecipeNutritionTotals,
    RECIPE_MAX_INGREDIENT_AMOUNT,
    RECIPE_MAX_INTEGER_VALUE,
    RECIPE_NAME_MAX_LENGTH,
    RECIPE_OPTIONAL_TEXT_MAX_LENGTH,
    RECIPE_STEP_DESCRIPTION_MAX_LENGTH,
    RECIPE_STEP_TITLE_MAX_LENGTH,
    RECIPE_TEXT_AMOUNT_MAX_LENGTH,
    RECIPE_TEXT_NAME_MAX_LENGTH,
} from '../recipe-manage-lib/recipe-manage-form.mapper';
import { RecipeNutritionFormManager } from '../recipe-manage-lib/recipe-nutrition-form.manager';
import { mapRecognizedRecipe } from '../recipe-manage-lib/recipe-recognition.mapper';
import { RecipeStepFormManager } from '../recipe-manage-lib/recipe-step-form.manager';
import { RecipeNutritionEditorComponent } from '../recipe-nutrition-editor/recipe-nutrition-editor';
import { RecipeRecognitionComponent } from '../recipe-recognition/recipe-recognition';
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
        FdUiPreventUnloadDirective,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiTextareaComponent,
        FdUiFormDirective,
        PageBodyComponent,
        PageHeaderComponent,
        FdPageContainerDirective,
        RecipeBasicInfoComponent,
        RecipeNutritionEditorComponent,
        RecipeStepsListComponent,
        RecipeRecognitionComponent,
    ],
    templateUrl: './recipe-manage.html',
    styleUrls: ['./recipe-manage.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [RecipeManageFacade],
})
export class RecipeManageComponent implements UnsavedEditor {
    private confirmedNavigation = false;
    private leaveDecision: Promise<boolean> | null = null;
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    private readonly stepFormManager: RecipeStepFormManager;
    private readonly nutritionFormManager: RecipeNutritionFormManager;
    private lastRecipe: Recipe | null = null;
    private readonly checkingLanguage = signal(false);
    private readonly checkingPublication = signal(false);

    private readonly recipeManageFacade = inject(RecipeManageFacade);
    private readonly languageVersion = signal(0);
    private readonly stepsTouchedState = createCollectionTouchedState({
        hasItems: () => this.steps.length > 0,
        errorMessage: () => this.translateService.instant('FORM_ERRORS.NON_EMPTY_ARRAY'),
        dependencies: [this.languageVersion],
    });

    public readonly recipe = input<Recipe | null>(null);
    protected globalError = this.recipeManageFacade.globalError;
    protected readonly stepPhotosUploading = signal(false);
    protected readonly photosUploading = signal(false);
    protected readonly anyPhotosUploading = computed(() => this.photosUploading() || this.stepPhotosUploading());
    protected readonly isSubmitting = computed(() => this.recipeManageFacade.isSubmitting() || this.checkingPublication());
    protected readonly isImporting = signal(false);
    protected readonly isFormLocked = computed(() => this.isSubmitting() || this.checkingLanguage() || this.isImporting());
    protected readonly stepsTouched = this.stepsTouchedState.touched;
    protected readonly importUrl = signal('');
    protected readonly importText = signal('');
    protected readonly importErrorKey = signal<string | null>(null);
    protected readonly isImportPanelVisible = computed(() => this.recipe() === null);

    protected readonly recipeFormModel = signal<RecipeFormValues>(createRecipeFormValue(this.recipeManageFacade.preferredLanguage()));
    protected readonly recipeSignalForm = form(this.recipeFormModel, path => {
        required(path.name);
        validate(path.name, ({ value }) => (value().trim().length === 0 ? { kind: 'required' } : undefined));
        maxLength(path.name, RECIPE_NAME_MAX_LENGTH);
        validate(path.description, ({ value }) =>
            (value()?.length ?? 0) > RECIPE_OPTIONAL_TEXT_MAX_LENGTH
                ? { kind: 'maxLength', maxLength: RECIPE_OPTIONAL_TEXT_MAX_LENGTH }
                : undefined,
        );
        validate(path.comment, ({ value }) =>
            (value()?.length ?? 0) > RECIPE_OPTIONAL_TEXT_MAX_LENGTH
                ? { kind: 'maxLength', maxLength: RECIPE_OPTIONAL_TEXT_MAX_LENGTH }
                : undefined,
        );
        min(path.prepTime, 0);
        max(path.prepTime, RECIPE_MAX_INTEGER_VALUE);
        validate(path.prepTime, ({ value }) => (value() === null || Number.isInteger(value()) ? undefined : { kind: 'integer' }));
        min(path.cookTime, 1);
        max(path.cookTime, RECIPE_MAX_INTEGER_VALUE);
        validate(path.cookTime, ({ value }) => (value() === null || Number.isInteger(value()) ? undefined : { kind: 'integer' }));
        required(path.servings);
        min(path.servings, 1);
        max(path.servings, RECIPE_MAX_INTEGER_VALUE);
        validate(path.servings, ({ value }) => (Number.isInteger(value()) ? undefined : { kind: 'integer' }));
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
        this.recipeManageFacade.committed$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.recipeSignalForm().reset(this.recipeFormModel());
        });
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
        this.stepFormManager.addStep();
        this.updateSummaryFromCurrentForm();
        this.watchSignalFormModelChanges();
        this.nutritionFormManager.initialize();
        this.connectDevelopmentTools();
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
        this.recipeSignalForm().markAsDirty();
        this.updateSummaryFromCurrentForm();
    }

    protected removeStep(index: number): void {
        this.stepFormManager.removeStep(index);
        this.recipeSignalForm().markAsDirty();
        this.updateSummaryFromCurrentForm();
    }

    protected addIngredientToStep(stepIndex: number): void {
        this.stepFormManager.addIngredientToStep(stepIndex);
        this.recipeSignalForm().markAsDirty();
        this.updateSummaryFromCurrentForm();
    }

    protected toggleStepExpanded(index: number): void {
        this.stepFormManager.toggleStepExpanded(index);
    }

    protected removeIngredientFromStep(event: StepIngredientEvent): void {
        this.stepFormManager.removeIngredientFromStep(event);
        this.recipeSignalForm().markAsDirty();
        this.updateSummaryFromCurrentForm();
    }

    protected onStepDrop(event: StepDropEvent): void {
        if (event.previousIndex === event.currentIndex) {
            return;
        }
        const steps = [...this.steps];
        moveItemInArray(steps, event.previousIndex, event.currentIndex);
        this.patchRecipeFormModel({ steps });
        this.recipeSignalForm().markAsDirty();
        this.updateSummaryFromCurrentForm();
    }

    protected onProductSelectClick(event: StepIngredientSelectEvent): void {
        const { stepIndex, ingredientIndex, itemType } = event;
        if (itemType === 'Text') {
            this.recipeManageFacade.applyItemSelection(
                {
                    patchValue: value => {
                        this.stepFormManager.patchIngredient({ stepIndex, ingredientIndex }, value);
                    },
                },
                { type: 'Text', name: '' },
            );
            this.recipeSignalForm().markAsDirty();
            this.updateSummaryFromCurrentForm();
            return;
        }
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
            this.recipeSignalForm().markAsDirty();
            this.updateSummaryFromCurrentForm();
        });
    }

    protected onIngredientTextChange(event: {
        stepIndex: number;
        ingredientIndex: number;
        field: 'textName' | 'amountText';
        value: string;
    }): void {
        this.stepFormManager.patchIngredient(event, {
            [event.field]: event.value,
            ...(event.field === 'textName' ? { foodName: event.value } : {}),
        });
        this.recipeSignalForm().markAsDirty();
        this.updateSummaryFromCurrentForm();
    }

    protected onStepTitleChange(event: StepFieldEvent<string | null>): void {
        this.patchStep(event.stepIndex, { title: event.value });
        this.recipeSignalForm().markAsDirty();
    }

    protected onStepPhotosChange(event: StepFieldEvent<NonNullable<StepFormValues['images']>>): void {
        this.patchStep(event.stepIndex, { images: event.value, imageUrl: event.value[0] ?? null });
        this.recipeSignalForm().markAsDirty();
    }

    protected onStepImageChange(event: StepFieldEvent<StepFormValues['imageUrl']>): void {
        this.patchStep(event.stepIndex, { imageUrl: event.value });
        this.recipeSignalForm().markAsDirty();
    }

    protected onStepFieldBlur(event: { stepIndex: number; field: 'description' | 'amount' | 'foodName'; ingredientIndex?: number }): void {
        if (event.field === 'description') {
            this.patchStep(event.stepIndex, { descriptionTouched: true });
        } else if (event.ingredientIndex !== undefined) {
            this.stepFormManager.patchIngredient(
                { stepIndex: event.stepIndex, ingredientIndex: event.ingredientIndex },
                event.field === 'amount' ? { amountTouched: true } : { foodNameTouched: true },
            );
        }
    }

    protected onStepDescriptionChange(event: StepFieldEvent<string>): void {
        this.patchStep(event.stepIndex, { description: event.value });
        this.recipeSignalForm().markAsDirty();
    }

    protected onIngredientAmountChange(event: StepIngredientAmountEvent): void {
        this.stepFormManager.patchIngredient(
            { stepIndex: event.stepIndex, ingredientIndex: event.ingredientIndex },
            { amount: event.amount },
        );
        this.recipeSignalForm().markAsDirty();
        this.updateSummaryFromCurrentForm();
    }

    // -- Nutrition mode --

    protected onNutritionModeChange(nextMode: string): void {
        const wasAutomatic = this.recipeFormModel().calculateNutritionAutomatically;
        this.nutritionFormManager.onNutritionModeChange(nextMode);
        this.nutritionFormManager.handleAutoCalculationChange(this.recipeFormModel().calculateNutritionAutomatically);
        if (wasAutomatic !== this.recipeFormModel().calculateNutritionAutomatically) {
            this.recipeSignalForm().markAsDirty();
        }
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
        if (
            this.isSubmitting() ||
            this.checkingLanguage() ||
            this.checkingPublication() ||
            this.photosUploading() ||
            this.stepPhotosUploading()
        ) {
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

        this.saveWithLanguageCheck(this.prepareRecipeDto());
    }

    private saveWithLanguageCheck(recipeData: RecipeDto): void {
        const previous = this.recipe();
        const text = recipeLanguageText(recipeData);
        const detected = detectRecipeLanguage(text);
        const acknowledged =
            (this.languageAcknowledgement !== null &&
                this.languageAcknowledgement.language === recipeData.language &&
                !hasSubstantiallyChangedRecipeText(this.languageAcknowledgement.text, text)) ||
            (previous?.languageConfirmed === true &&
                previous.language === recipeData.language &&
                !hasSubstantiallyChangedRecipeText(recipeLanguageText(previous), text));
        if (!acknowledged && detected !== null && detected !== recipeData.language) {
            void this.confirmLanguageAndSaveAsync(recipeData, detected);
            return;
        }
        this.persistRecipe({ ...recipeData, languageConfirmed: acknowledged });
    }

    private languageAcknowledgement: { language: string; text: string } | null = null;

    private async confirmLanguageAndSaveAsync(data: RecipeDto, detected: string): Promise<void> {
        this.checkingLanguage.set(true);
        try {
            const choice = await this.recipeManageFacade.confirmRecipeLanguageAsync(detected, data.language ?? 'en');
            if (choice === undefined) {
                return;
            }
            // Re-read on another save if the form changed while the dialog was open.
            if (
                recipeLanguageText(this.prepareRecipeDto()) !== recipeLanguageText(data) ||
                this.recipeFormModel().language !== data.language
            ) {
                return;
            }
            const language = choice === 'change' ? detected : (data.language ?? 'en');
            this.languageAcknowledgement = choice === 'keep' ? { language, text: recipeLanguageText(data) } : null;
            this.patchRecipeFormModel({ language });
            this.persistRecipe({ ...data, language, languageConfirmed: choice === 'keep' });
        } finally {
            this.checkingLanguage.set(false);
        }
    }

    private persistRecipe(recipeData: RecipeDto): void {
        void this.persistPublishedRecipeAsync(recipeData);
    }

    private async persistPublishedRecipeAsync(recipeData: RecipeDto): Promise<void> {
        this.checkingPublication.set(true);
        const formVersion = JSON.stringify(this.prepareRecipeDto());
        try {
            const prepared = await this.recipeManageFacade.preparePublicationAsync(recipeData);
            if (prepared !== null && formVersion === JSON.stringify(this.prepareRecipeDto())) {
                this.savePreparedRecipe(prepared);
            }
        } catch {
            this.recipeManageFacade.setGlobalError('RECIPE_PUBLICATION.ERROR');
        } finally {
            this.checkingPublication.set(false);
        }
    }

    private savePreparedRecipe(recipeData: RecipeDto): void {
        const existingRecipe = this.recipe();
        this.recipeManageFacade.clearGlobalError();

        if (existingRecipe !== null) {
            this.recipeManageFacade.updateRecipe(existingRecipe.id, recipeData);
        } else {
            this.recipeManageFacade.addRecipe(recipeData);
        }
    }

    protected async onCancelAsync(): Promise<void> {
        if (!(await this.canLeaveAsync())) {
            return;
        }
        this.confirmedNavigation = true;
        try {
            await this.recipeManageFacade.cancelManageAsync();
        } finally {
            this.confirmedNavigation = false;
        }
    }

    // eslint-disable-next-line local/prefer-protected-template-members -- Public editor departure contract used by the router and parent view queries.
    public async canLeaveAsync(): Promise<boolean> {
        if (this.confirmedNavigation || !this.recipeSignalForm().dirty()) {
            return true;
        }
        if (this.isFormLocked()) {
            return false;
        }
        this.leaveDecision ??= this.recipeManageFacade
            .confirmDiscardChangesAsync({
                title: this.translateService.instant('UNSAVED_CHANGES.TITLE'),
                message: this.translateService.instant('UNSAVED_CHANGES.MESSAGE'),
                confirmLabel: this.translateService.instant('UNSAVED_CHANGES.DISCARD'),
                cancelLabel: this.translateService.instant('UNSAVED_CHANGES.STAY'),
                confirmIcon: 'logout',
            })
            .finally(() => {
                this.leaveDecision = null;
            });
        return this.leaveDecision;
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
            existingIngredients: this.steps.flatMap(step => step.ingredients),
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
        this.recipeSignalForm().markAsDirty();
        this.stepFormManager.expandedSteps.clear();
        draft.steps.forEach((_, index) => this.stepFormManager.expandedSteps.add(index));
        this.importErrorKey.set(null);
        this.updateSummaryFromCurrentForm();
    }

    protected applyRecognizedDraft(draft: RecipeImportResult): void {
        this.nutritionFormManager.nutritionScaleMode = 'recipe';
        this.patchRecipeFormModel(
            mapRecognizedRecipe(
                draft,
                this.translateService.instant('RECIPE_MANAGE.IMPORT.SOURCE_LABEL'),
                this.translateService.instant('RECIPE_MANAGE.IMPORT.AUTHOR_NUTRITION'),
            ),
        );
        this.recipeSignalForm().markAsDirty();
        this.stepFormManager.expandedSteps.clear();
        this.steps.forEach((_, index) => this.stepFormManager.expandedSteps.add(index));
        this.importErrorKey.set(null);
        this.nutritionFormManager.initialize();
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
        this.nutritionFormManager.initialize();
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
                error:
                    (step.title?.length ?? 0) > RECIPE_STEP_TITLE_MAX_LENGTH
                        ? this.translateService.instant('FORM_ERRORS.MAX_LENGTH', { requiredLength: RECIPE_STEP_TITLE_MAX_LENGTH })
                        : null,
            },
            images: step.images,
            imageUrl: {
                value: step.imageUrl,
                error: null,
            },
            description: {
                value: step.description,
                error:
                    step.description.length > RECIPE_STEP_DESCRIPTION_MAX_LENGTH
                        ? this.translateService.instant('FORM_ERRORS.MAX_LENGTH', { requiredLength: RECIPE_STEP_DESCRIPTION_MAX_LENGTH })
                        : (touched || step.descriptionTouched === true) && step.description.trim().length === 0
                          ? this.translateService.instant('RECIPE_MANAGE.STEP_DESCRIPTION_REQUIRED')
                          : null,
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
                error: this.getIngredientAmountError(ingredient, touched || ingredient.amountTouched === true),
            },
            textName: ingredient.textName,
            amountText: ingredient.amountText,
            food: ingredient.food,
            foodName: {
                value: ingredient.foodName,
                error:
                    !hasFoodName && (touched || ingredient.foodNameTouched === true)
                        ? this.translateService.instant('RECIPE_MANAGE.INGREDIENT_REQUIRED')
                        : null,
            },
            nestedRecipeId: ingredient.nestedRecipeId,
        };
    }

    private getIngredientAmountError(ingredient: IngredientFormValues, touched: boolean): string | null {
        if (!touched || typeof ingredient.textName === 'string') {
            return null;
        }

        if (ingredient.amount === null) {
            return this.translateService.instant('RECIPE_MANAGE.INGREDIENT_AMOUNT_REQUIRED');
        }

        if (!Number.isFinite(ingredient.amount)) {
            return this.translateService.instant('FORM_ERRORS.INVALID_NUMBER');
        }

        if (ingredient.amount > RECIPE_MAX_INGREDIENT_AMOUNT) {
            return this.translateService.instant('FORM_ERRORS.INVALID_MAX_AMOUNT', { max: RECIPE_MAX_INGREDIENT_AMOUNT });
        }

        if (ingredient.amount <= 0) {
            return this.translateService.instant('FORM_ERRORS.MEAL.INVALID_MIN_AMOUNT');
        }

        return null;
    }

    protected areStepsValid(): boolean {
        return this.steps.length > 0 && this.steps.every(step => this.isStepValid(step));
    }

    private isStepValid(step: StepFormValues): boolean {
        return (
            step.description.trim().length > 0 &&
            step.description.length <= RECIPE_STEP_DESCRIPTION_MAX_LENGTH &&
            (step.title?.length ?? 0) <= RECIPE_STEP_TITLE_MAX_LENGTH &&
            step.ingredients.every(ingredient => this.isIngredientValid(ingredient))
        );
    }

    private isIngredientValid(ingredient: IngredientFormValues): boolean {
        if (typeof ingredient.textName === 'string') {
            return this.isTextIngredientValid(ingredient.textName, ingredient.amountText);
        }
        return (
            ingredient.foodName !== null &&
            ingredient.foodName.trim().length > 0 &&
            ingredient.amount !== null &&
            Number.isFinite(ingredient.amount) &&
            ingredient.amount > 0 &&
            ingredient.amount <= RECIPE_MAX_INGREDIENT_AMOUNT
        );
    }

    private isTextIngredientValid(name: string, amountText: string | null | undefined): boolean {
        return (
            name.trim().length > 0 &&
            name.length <= RECIPE_TEXT_NAME_MAX_LENGTH &&
            (amountText?.length ?? 0) <= RECIPE_TEXT_AMOUNT_MAX_LENGTH
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
        this.updateSummaryFromCurrentForm();
    }

    protected get nutritionScaleMode(): NutritionScaleMode {
        return this.nutritionFormManager.nutritionScaleMode;
    }

    private connectDevelopmentTools(): void {
        const requireNewEditor = (): void => {
            if (this.recipe() !== null || this.isFormLocked() || this.anyPhotosUploading()) {
                throw new DevelopmentToolError('A ready NEW recipe editor is required.');
            }
        };
        connectDevelopmentScreen({
            kind: 'recipe-editor',
            read: () => this.readDevelopmentContext(),
            isDirty: () => this.recipeSignalForm().dirty(),
            draftSchema: RECIPE_DEVELOPMENT_SCHEMA,
            fillDraft: draftInput => {
                requireNewEditor();
                this.fillDevelopmentDraft(draftInput);
            },
            clearDraft: () => {
                requireNewEditor();
                this.recipeSignalForm().reset(createRecipeFormValue(this.recipeManageFacade.preferredLanguage()));
                this.stepFormManager.addStep();
                this.updateSummaryFromCurrentForm();
            },
        });
    }

    private fillDevelopmentDraft(draftInput: unknown): void {
        if (this.recipeSignalForm().dirty() || !(draftInput instanceof RecipeDevelopmentDraft)) {
            throw new DevelopmentToolError('A clean editor and resolved product ingredients are required.');
        }
        this.patchRecipeFormModel({
            name: draftInput.name,
            servings: draftInput.servings,
            cookTime: draftInput.cookTime,
            steps: draftInput.steps.map(step => ({
                title: null,
                imageUrl: null,
                description: step.description,
                ingredients: step.ingredients.map(item => this.developmentIngredientValue(item)),
            })),
        });
        this.recipeSignalForm().markAsDirty();
        this.updateSummaryFromCurrentForm();
    }

    private developmentIngredientValue(item: RecipeDevelopmentIngredient): IngredientFormValues {
        if (item.product !== undefined) {
            return createRecipeIngredientValue({ food: item.product, productId: item.product.id, amount: item.amount });
        }
        return createRecipeIngredientValue({
            amount: item.amount,
            nestedRecipe: item.recipe,
            nestedRecipeId: item.recipe.id,
            nestedRecipeName: item.recipe.name,
        });
    }

    private readDevelopmentContext(): unknown {
        return {
            isNew: this.recipe() === null,
            id: this.recipe()?.id ?? null,
            name: this.recipeFormModel().name,
            servings: this.recipeFormModel().servings,
            steps: this.steps.map(step => ({
                description: step.description,
                ingredients: step.ingredients.map(item => ({
                    productId: item.productId,
                    recipeId: item.nestedRecipeId,
                    amount: item.amount,
                    unit: item.food?.baseUnit ?? 'servings',
                })),
            })),
            nutrition: {
                mode: this.nutritionMode(),
                calories: this.totalCalories(),
                fiber: this.totalFiber(),
                alcohol: this.totalAlcohol(),
            },
            valid: this.recipeSignalForm().valid() && this.areStepsValid(),
            loading: this.isFormLocked(),
            error: this.globalError(),
            validation: {
                stepsValid: this.areStepsValid(),
                name: this.recipeSignalForm
                    .name()
                    .errors()
                    .map(error => error.kind),
                cookTime: this.recipeSignalForm
                    .cookTime()
                    .errors()
                    .map(error => error.kind),
            },
        };
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

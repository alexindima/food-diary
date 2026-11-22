import { HttpErrorResponse } from '@angular/common/http';
import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    type EffectRef,
    inject,
    Injector,
    input,
    linkedSignal,
    signal,
    untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, max, min, required, validate } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective, FdUiPreventUnloadDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiFormDirective } from 'fd-ui-kit/form/fd-ui-form.directive';
import { FD_VALIDATION_ERRORS, type FdValidationErrors, resolveSignalFormFieldError } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { finalize, firstValueFrom, type Subscription } from 'rxjs';

import type { AiInputBarResult } from '../../../../components/shared/ai-input-bar/ai-input-bar.types';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { NavigationService } from '../../../../services/navigation.service';
import { createCollectionTouchedState } from '../../../../shared/lib/collection-touched-state.utils';
import { normalizeMealType, resolveMealTypeByTime } from '../../../../shared/lib/meal-type.util';
import {
    DEFAULT_CALORIE_MISMATCH_THRESHOLD,
    MANUAL_NUTRITION_MAX_CALORIES,
    MANUAL_NUTRITION_MAX_NUTRIENT,
} from '../../../../shared/lib/nutrition.constants';
import { calculateMacroBarState, checkCaloriesError, checkMacrosError } from '../../../../shared/lib/nutrition-form.utils';
import { normalizeSatietyLevel } from '../../../../shared/lib/satiety-level.utils';
import { patchSignalFormModel } from '../../../../shared/lib/signal-form-model.utils';
import { getRecordProperty, getStringProperty } from '../../../../shared/lib/unknown-value.utils';
import type { NutrientData } from '../../../../shared/models/charts.data';
import { type Meal, type MealAiSessionManageDto, type MealManageDto, MealSourceType } from '../../../../shared/models/meal.data';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { MEAL_DEVELOPMENT_TOOLS, type MealDevelopmentDraft } from '../../contracts/meal-development-tools';
import { MEAL_MANAGE_COMMENT_LIMIT, MEAL_MANAGE_MIN_ITEM_AMOUNT } from '../../lib/manage/meal-manage.config';
import { MealManageFacade } from '../../lib/manage/meal-manage.facade';
import { isExistingMealLocalDateTime } from '../../lib/meal-date-input.utils';
import { recipeServingsFromInput } from '../../lib/recipe-serving/recipe-display-amount';
import type { MealAiSessionRemoval } from './meal-ai-sessions/meal-ai-sessions';
import { type MealGeneralFieldErrors, MealGeneralInfoComponent } from './meal-general-info/meal-general-info';
import type { MealItemsListItemState } from './meal-items-list/meal-items-list';
import { MealItemsSectionComponent } from './meal-items-section/meal-items-section';
import type {
    CalorieMismatchWarning,
    CommittedMealFormValues,
    CommittedMealItemFormValues,
    MacroBarState,
    MealFormValues,
    MealItemFormValues,
    MealNutritionSummaryState,
    NutritionMode,
    NutritionTotals,
} from './meal-manage-lib/meal-manage.types';
import {
    buildMealDateTime,
    buildMealManageDto,
    buildMealManageFormPatchValue,
    createMealItemValue,
    createMealManageFormValue,
    findReusableEmptyMealItemIndex,
    getDateInputValue,
    getTimeInputValue,
    hasSelectedMealItems,
} from './meal-manage-lib/meal-manage-form.mapper';
import { buildMealTypeSelectOptions, type MealSatietyControlName } from './meal-manage-lib/meal-manage-options.mapper';
import { MEAL_MANAGE_TOUR } from './meal-manage-tour';
import {
    MealManualItemDialogComponent,
    type MealManualItemDialogData,
    type MealManualItemDialogResult,
} from './meal-manual-item-dialog/meal-manual-item-dialog';
import { MealNutritionSidebarComponent } from './meal-nutrition-sidebar/meal-nutrition-sidebar';
import { MealSatietyCardComponent } from './meal-satiety-card/meal-satiety-card';

const MANUAL_NUTRITION_FIELDS = ['manualCalories', 'manualProteins', 'manualFats', 'manualCarbs', 'manualFiber', 'manualAlcohol'] as const;
const GENERAL_ERROR_FIELDS = ['date', 'time', 'mealType', 'comment'] as const;
import type { UnsavedEditor } from '../../../../shared/forms/unsaved-editor';
import { isHealthConsentRequiredError } from '../../../../shared/privacy/health-consent-error';
import { PrivacyConsentFacade } from '../../../../shared/privacy/privacy-consent.facade';

@Component({
    selector: 'fd-meal-manage-form',
    templateUrl: './meal-manage-form.html',
    styleUrls: ['./meal-manage-form.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiPreventUnloadDirective,
        FdUiButtonComponent,
        FdUiLoaderComponent,
        PageBodyComponent,
        PageHeaderComponent,
        FdPageContainerDirective,
        FdUiFormDirective,
        MealGeneralInfoComponent,
        MealSatietyCardComponent,
        MealItemsSectionComponent,
        MealNutritionSidebarComponent,
    ],
})
export class MealManageFormComponent implements UnsavedEditor {
    private confirmedNavigation = false;
    private leaveDecision: Promise<boolean> | null = null;
    private createIntent: { fingerprint: string; key: string } | null = null;
    private readonly translateService = inject(TranslateService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly injector = inject(Injector);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly privacy = inject(PrivacyConsentFacade);
    private readonly mealManageFacade = inject(MealManageFacade);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    private readonly calorieMismatchThreshold = DEFAULT_CALORIE_MISMATCH_THRESHOLD;
    private readonly validationErrors = {
        ...inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true }),
        nonexistentLocalTime: (): string => 'MEAL_MANAGE.NONEXISTENT_TIME',
    };
    private readonly languageVersion = signal(0);

    public readonly meal = input<Meal | null>(null);
    protected readonly totalCalories = signal<number>(0);
    protected readonly totalFiber = signal<number>(0);
    protected readonly totalAlcohol = signal<number>(0);
    protected readonly nutrientChartData = signal<NutrientData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });
    protected readonly globalError = signal<string | null>(null);
    protected readonly isSubmitting = signal(false);
    protected readonly photoOwnerId = computed(() => this.meal()?.id ?? null);
    protected readonly photosUploading = linkedSignal({ source: this.photoOwnerId, computation: () => false });
    protected readonly isLoadingRecipeAmounts = signal(false);
    protected readonly isBusy = computed(() => this.isSubmitting() || this.isLoadingRecipeAmounts());
    protected readonly isSubmitBusy = computed(() => this.isBusy() || this.photosUploading());
    protected readonly isSubmitDisabled = computed(() => this.isSubmitBusy() || !this.hasSelectedItems());
    protected readonly aiSessions = signal<MealAiSessionManageDto[]>([]);
    private readonly itemsTouchedState = createCollectionTouchedState({
        hasItems: () => this.hasSelectedItems(),
        errorMessage: () => this.translateService.instant('FORM_ERRORS.NON_EMPTY_ARRAY'),
        dependencies: [this.languageVersion],
    });
    protected readonly itemsTouched = this.itemsTouchedState.touched;
    protected readonly nutritionMode = signal<NutritionMode>('auto');
    protected readonly preMealSatietyLevel = signal<number | null>(null);
    protected readonly postMealSatietyLevel = signal<number | null>(null);
    protected readonly selectedMealType = signal<string | null>(null);
    protected readonly nutritionWarning = signal<CalorieMismatchWarning | null>(null);
    protected readonly generalFieldErrors = computed<MealGeneralFieldErrors>(() => {
        this.languageVersion();

        return GENERAL_ERROR_FIELDS.reduce<MealGeneralFieldErrors>((errors, field) => {
            errors[field] = resolveSignalFormFieldError(this.mealSignalForm[field], this.validationErrors, this.translateService);
            return errors;
        }, this.createEmptyGeneralFieldErrors());
    });
    protected readonly mealFormModel = signal<CommittedMealFormValues>(createMealManageFormValue());
    private readonly submitMealFormAsync = async (): Promise<void> => {
        await this.onSubmitAsync();
    };
    protected readonly mealSignalForm = form(
        this.mealFormModel,
        path => {
            required(path.date);
            required(path.time);
            validate(path.comment, ({ value }) =>
                (value()?.trim().length ?? 0) <= MEAL_MANAGE_COMMENT_LIMIT
                    ? undefined
                    : {
                          kind: 'maxLength',
                          maxLength: MEAL_MANAGE_COMMENT_LIMIT,
                      },
            );
            validate(path.time, context => {
                const date = context.valueOf(path.date);
                const time = context.value();
                return date.length > 0 && time.length > 0 && !isExistingMealLocalDateTime(date, time)
                    ? { kind: 'nonexistentLocalTime' }
                    : undefined;
            });
            for (const field of MANUAL_NUTRITION_FIELDS) {
                min(path[field], 0);
                validate(path[field], ({ value }) =>
                    value() === null || Number.isFinite(value()) ? undefined : { kind: 'invalidNumber' },
                );
            }
            max(path.manualCalories, MANUAL_NUTRITION_MAX_CALORIES);
            max(path.manualProteins, MANUAL_NUTRITION_MAX_NUTRIENT);
            max(path.manualFats, MANUAL_NUTRITION_MAX_NUTRIENT);
            max(path.manualCarbs, MANUAL_NUTRITION_MAX_NUTRIENT);
            max(path.manualFiber, MANUAL_NUTRITION_MAX_NUTRIENT);
            max(path.manualAlcohol, MANUAL_NUTRITION_MAX_NUTRIENT);
        },
        {
            submission: {
                action: this.submitMealFormAsync,
                onInvalid: () => {
                    this.handleInvalidSubmit();
                },
            },
        },
    );
    protected readonly manageHeaderState = computed(() => ({
        titleKey: this.meal() !== null ? 'MEAL_MANAGE.EDIT_TITLE' : 'MEAL_MANAGE.ADD_TITLE',
    }));

    protected readonly macroBarState = computed<MacroBarState>(() => {
        const nutrients = this.nutrientChartData();
        return calculateMacroBarState(nutrients.proteins, nutrients.fats, nutrients.carbs);
    });
    protected readonly itemListItems = computed<readonly MealItemsListItemState[]>(() => {
        const touched = this.itemsTouched();

        return this.mealFormModel().items.map(value => {
            const productInvalid = touched && value.sourceType === MealSourceType.Product && value.product === null;
            const recipeInvalid = touched && value.sourceType === MealSourceType.Recipe && value.recipe === null;
            const sourceError = productInvalid || recipeInvalid ? this.translateService.instant('MEAL_MANAGE.ITEM_SOURCE_ERROR') : null;
            const amountError = this.getItemAmountError(value, touched);
            return {
                formItem: value,
                amountError,
                productInvalid,
                recipeInvalid,
                sourceError,
            };
        });
    });
    protected readonly itemsError = this.itemsTouchedState.error;

    protected mealTypeSelectOptions: Array<FdUiSelectOption<string>> = [];

    public constructor() {
        this.connectDevelopmentTools();
        afterNextRender(() => {
            if (this.meal() === null && this.route.snapshot.queryParamMap.get('manual') === '1') {
                this.addMealItem();
            }
        });
        this.buildMealTypeOptions();
        this.watchLanguageChanges();
        this.watchSignalFormModelChanges();

        const presetMealType = this.resolvePresetMealType();
        if (presetMealType !== null) {
            this.patchMealFormModel({ mealType: presetMealType });
        } else {
            this.setAutoMealTypeFromDate();
        }

        this.watchMealInput();

        if (presetMealType === null) {
            this.watchAutoMealTypeChanges();
        }
    }

    private connectDevelopmentTools(): void {
        const developmentTools = inject(MEAL_DEVELOPMENT_TOOLS, { optional: true });
        if (developmentTools === null) {
            return;
        }
        this.destroyRef.onDestroy(
            developmentTools.attach({
                isDirty: () => this.mealSignalForm().dirty(),
                read: () => this.readDevelopmentContext(),
                fillDraft: this.fillDevelopmentDraftAsync.bind(this),
                clearDraft: () => {
                    if (this.meal() !== null || this.isSubmitBusy()) {
                        throw new Error('A ready new meal editor is required.');
                    }
                    this.mealSignalForm().reset(createMealManageFormValue());
                    this.aiSessions.set([]);
                    this.updateSummary();
                },
            }),
        );
    }

    private readDevelopmentContext(): unknown {
        return {
            kind: this.meal() === null ? 'new-meal-editor' : 'edit-meal-editor',
            date: this.mealFormModel().date,
            time: this.mealFormModel().time,
            mealType: this.mealFormModel().mealType,
            comment: this.mealFormModel().comment,
            items: this.items.map(item => this.readDevelopmentItem(item)).filter(item => item !== null),
            calories: this.totalCalories(),
            dirty: this.mealSignalForm().dirty(),
            valid:
                this.mealSignalForm().valid() &&
                this.hasSelectedItems() &&
                this.caloriesError() === null &&
                this.macrosError() === null &&
                this.items.every(item => this.getItemAmountError(item, true) === null),
            error: this.globalError(),
        };
    }

    private readDevelopmentItem(item: CommittedMealItemFormValues): unknown {
        if (item.product !== null) {
            return {
                name: item.product.name,
                productId: item.product.id,
                recipeId: null,
                servings: null,
                amount: item.amount,
                unit: item.product.baseUnit,
            };
        }
        if (item.recipe === null) {
            return null;
        }
        return {
            name: item.recipe.name,
            productId: null,
            recipeId: item.recipe.id,
            servings: recipeServingsFromInput(item.amount, item.recipeMass),
            amount: item.amount,
            unit: item.recipeMass.kind === 'known' ? 'grams' : 'servings',
        };
    }

    private async fillDevelopmentDraftAsync(draft: MealDevelopmentDraft, requireSession: () => void): Promise<void> {
        if (this.meal() !== null || this.mealSignalForm().dirty() || this.isSubmitBusy()) {
            throw new Error('A clean new meal editor is required.');
        }
        const original = this.mealFormModel();
        const resolveItemAsync = async (item: MealDevelopmentDraft['items'][number]): Promise<CommittedMealItemFormValues> =>
            item.recipe === undefined
                ? createMealItemValue(item.product, null, item.amount)
                : this.mealManageFacade.prepareRecipeItemAsync(item.recipe, item.servings);
        const items = await Promise.all(draft.items.map(resolveItemAsync));
        requireSession();
        if (this.destroyRef.destroyed || this.mealFormModel() !== original || this.mealSignalForm().dirty() || this.isSubmitBusy()) {
            throw new Error('Meal editor changed while resolving serving quantities.');
        }
        this.mealSignalForm.mealType().markAsDirty();
        this.patchMealFormModel({ ...draft, items });
        this.selectedMealType.set(draft.mealType);
        this.updateSummary();
    }

    private watchLanguageChanges(): void {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
            this.buildMealTypeOptions();
        });
    }

    private watchSignalFormModelChanges(): void {
        effect(() => {
            const value = this.mealFormModel();
            untracked(() => {
                this.selectedMealType.set(value.mealType);
                this.nutritionMode.set(value.isNutritionAutoCalculated ? 'auto' : 'manual');
                this.preMealSatietyLevel.set(value.preMealSatietyLevel);
                this.postMealSatietyLevel.set(value.postMealSatietyLevel);
                this.updateSummary();
                this.clearGlobalError();
            });
        });
    }

    private watchMealInput(): void {
        effect(onCleanup => {
            const meal = this.meal();
            if (meal === null) {
                this.isLoadingRecipeAmounts.set(false);
                return;
            }

            const subscription = untracked(() => this.populateForm(meal));
            onCleanup(() => {
                subscription.unsubscribe();
            });
        });
    }

    private watchAutoMealTypeChanges(): void {
        effect(() => {
            this.setAutoMealTypeFromDate();
        });
    }

    protected async onCancelAsync(): Promise<void> {
        if (!(await this.canLeaveAsync())) {
            return;
        }
        this.confirmedNavigation = true;
        try {
            await this.navigationService.navigateToMealListAsync();
        } finally {
            this.confirmedNavigation = false;
        }
    }

    // eslint-disable-next-line local/prefer-protected-template-members -- Public editor departure contract used by the router and parent view queries.
    public async canLeaveAsync(): Promise<boolean> {
        if (this.confirmedNavigation || !this.mealSignalForm().dirty()) {
            return true;
        }
        if (this.isBusy()) {
            return false;
        }
        this.leaveDecision ??= this.mealManageFacade
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

    protected startMealManageTour(force = true): void {
        this.tourService.start(this.localizedTour.build(MEAL_MANAGE_TOUR), { force });
    }

    protected get items(): CommittedMealItemFormValues[] {
        return this.mealFormModel().items;
    }

    // --- Item management (delegated from MealItemsListComponent events) ---

    protected addMealItem(): void {
        const reusableIndex = this.findReusableEmptyItemIndex();
        const itemIndex = reusableIndex >= 0 ? reusableIndex : this.items.length;

        if (reusableIndex < 0) {
            this.patchMealFormModel({
                items: [...this.items, this.mealManageFacade.createMealItem()],
            });
        }

        afterNextRender(
            () => {
                this.openManualItemDialog(itemIndex);
            },
            { injector: this.injector },
        );
    }

    protected removeItem(item: MealItemFormValues): void {
        const items = this.items;
        const index = items.findIndex(currentItem => currentItem === item);
        if (index === -1) {
            return;
        }
        this.mealSignalForm.items().markAsDirty();
        this.patchMealFormModel({
            items: items.filter((_, currentIndex) => currentIndex !== index),
        });
    }

    protected onItemSourceClick(index: number): void {
        this.openManualItemDialog(index);
    }

    protected openManualItemDialog(index: number): void {
        const meal = this.meal();
        const item = this.items[index];
        const dialog = this.fdDialogService.open<
            MealManualItemDialogComponent,
            MealManualItemDialogData,
            MealManualItemDialogResult | null
        >(MealManualItemDialogComponent, { preset: 'form', data: { item } });
        const closeDialog = (): void => {
            dialog.close(null);
        };
        const unregisterClose = this.destroyRef.onDestroy(closeDialog);
        const closeOnMealChange = this.closeDialogOnMealChange(meal, closeDialog);
        const cleanup = (): void => {
            closeOnMealChange.destroy();
            unregisterClose();
        };
        void firstValueFrom(dialog.afterClosed().pipe(takeUntilDestroyed(this.destroyRef), finalize(cleanup)), {
            defaultValue: null,
        }).then(selectedItem => {
            if (selectedItem === null || selectedItem === undefined || !this.isCurrentMeal(meal)) {
                return;
            }

            const currentIndex = this.items.indexOf(item);
            if (currentIndex === -1) {
                return;
            }
            this.replaceMealItem(currentIndex, this.mealManageFacade.configureItemType(selectedItem, selectedItem.sourceType));
            this.itemsTouchedState.markTouched();
            this.mealSignalForm.items().markAsDirty();
            this.updateSummary();
        });
    }

    // --- AI session management ---

    protected onAiMealRecognized(result: AiInputBarResult): void {
        this.mealSignalForm().markAsDirty();
        this.aiSessions.update(current =>
            this.mealManageFacade.addAiSession(current, {
                source: result.source,
                imageAssetId: result.imageAssetId,
                imageUrl: result.imageUrl,
                recognizedAtUtc: result.recognizedAtUtc,
                notes: result.notes,
                items: result.items,
            }),
        );
        this.updateSummary();
    }

    protected onDeleteAiSession({ session, onRemoved }: MealAiSessionRemoval): void {
        const current = this.aiSessions();
        const index = current.indexOf(session);
        if (index === -1) {
            return;
        }
        this.mealSignalForm().markAsDirty();
        this.aiSessions.set(this.mealManageFacade.removeAiSession(current, index));
        onRemoved(index);
        this.updateSummary();
    }

    protected onEditAiSession(session: MealAiSessionManageDto): void {
        if (!this.aiSessions().includes(session) || !this.ensurePremiumAccess()) {
            return;
        }
        void this.editAiSessionAsync(session);
    }

    private async editAiSessionAsync(session: MealAiSessionManageDto): Promise<void> {
        const meal = this.meal();
        const reviewScope = new AbortController();
        const closeOnMealChange = this.closeDialogOnMealChange(meal, () => {
            reviewScope.abort();
        });
        try {
            const updated = await this.mealManageFacade.openEditAiPhotoSessionDialogAsync(session, this.destroyRef, reviewScope.signal);
            if (updated === null || !this.isCurrentMeal(meal)) {
                return;
            }
            const currentIndex = this.aiSessions().indexOf(session);
            if (currentIndex === -1) {
                return;
            }
            this.mealSignalForm().markAsDirty();
            this.aiSessions.update(current => this.mealManageFacade.replaceAiSession(current, currentIndex, updated));
            this.updateSummary();
        } finally {
            closeOnMealChange.destroy();
        }
    }

    // --- Nutrition mode (delegated from MealNutritionSidebarComponent events) ---

    protected onNutritionModeChange(nextMode: NutritionMode): void {
        this.mealSignalForm.isNutritionAutoCalculated().markAsDirty();
        this.nutritionMode.set(nextMode);
        const isAuto = nextMode === 'auto';
        this.patchMealFormModel({ isNutritionAutoCalculated: isAuto });
        if (!isAuto) {
            this.populateManualNutritionFromCurrentSummary();
        }
        this.updateSummary();
    }

    protected caloriesError(): string | null {
        if (this.mealFormModel().isNutritionAutoCalculated) {
            return null;
        }

        return checkCaloriesError(this.getSignalNutritionControlState('manualCalories'))
            ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.CALORIES_REQUIRED')
            : null;
    }

    protected macrosError(): string | null {
        if (this.mealFormModel().isNutritionAutoCalculated) {
            return null;
        }

        return checkMacrosError([
            this.getSignalNutritionControlState('manualProteins'),
            this.getSignalNutritionControlState('manualFats'),
            this.getSignalNutritionControlState('manualCarbs'),
            this.getSignalNutritionControlState('manualAlcohol'),
        ])
            ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED')
            : null;
    }

    // --- Satiety ---

    protected onSatietyLevelChange(controlName: MealSatietyControlName, value: number | null): void {
        const normalizedValue = normalizeSatietyLevel(value);
        this.mealSignalForm[controlName]().markAsDirty();
        this.patchMealFormModel({ [controlName]: normalizedValue });

        if (controlName === 'preMealSatietyLevel') {
            this.preMealSatietyLevel.set(normalizedValue);
            return;
        }

        this.postMealSatietyLevel.set(normalizedValue);
    }

    // --- Form control helpers ---

    private createEmptyGeneralFieldErrors(): MealGeneralFieldErrors {
        return {
            date: null,
            time: null,
            mealType: null,
            comment: null,
        };
    }

    // --- Submit ---

    protected async onSubmitAsync(): Promise<void> {
        if (this.isSubmitBusy()) {
            return;
        }

        this.mealSignalForm().markAsTouched();
        this.itemsTouchedState.markTouched();

        if (this.caloriesError() !== null || this.macrosError() !== null) {
            return;
        }

        if (!this.hasSelectedItems()) {
            this.setGlobalError('FORM_ERRORS.NON_EMPTY_ARRAY');
            return;
        }

        if (this.mealSignalForm().invalid() || this.itemListItems().some(item => item.amountError !== null)) {
            this.setGlobalError('FORM_ERRORS.CHECK_FIELDS');
            return;
        }

        const mealData = this.buildMealManageDto();
        const meal = this.meal();
        this.globalError.set(null);
        this.isSubmitting.set(true);
        try {
            await this.saveWithConsentAsync(meal, mealData);
        } catch (error: unknown) {
            if (this.isCurrentMeal(meal)) {
                this.handleSubmitError(error instanceof HttpErrorResponse ? error : new HttpErrorResponse({ error }));
            }
        } finally {
            this.isSubmitting.set(false);
        }
    }

    private handleInvalidSubmit(): void {
        this.itemsTouchedState.markTouched();
        this.setGlobalError('FORM_ERRORS.CHECK_FIELDS');
    }

    private buildMealManageDto(): MealManageDto {
        return buildMealManageDto(this.getMealFormValue(), {
            aiSessions: this.aiSessions(),
            buildDateTime: () => this.buildDateTime(),
            manualTotals: this.mealManageFacade.getManualNutritionTotalsFromValue(this.mealFormModel()),
        });
    }

    // --- Private methods ---

    private resolvePresetMealType(): string | null {
        const navigationState: unknown = this.router.currentNavigation()?.extras.state;
        const stateMealType = getStringProperty(navigationState, 'mealType');
        const queryMealType = this.route.snapshot.queryParamMap.get('mealType');
        return normalizeMealType(stateMealType ?? queryMealType);
    }

    private setAutoMealTypeFromDate(): void {
        if (this.meal() !== null) {
            return;
        }

        if (this.mealSignalForm.mealType().dirty()) {
            return;
        }

        const mealType = resolveMealTypeByTime(this.buildDateTime());
        if (this.mealFormModel().mealType === mealType) {
            return;
        }

        this.patchMealFormModel({ mealType });
        this.selectedMealType.set(mealType);
    }

    private populateForm(meal: Meal): Subscription {
        this.isLoadingRecipeAmounts.set(true);
        const patch = buildMealManageFormPatchValue(meal);
        this.patchMealFormModel({ ...patch, items: [] });
        this.aiSessions.set(meal.aiSessions ?? []);
        return this.mealManageFacade
            .prepareMealItems(meal.items)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(items => {
                this.patchMealFormModel({ items });
                this.isLoadingRecipeAmounts.set(false);
                this.updateSummary();
            });
    }

    private updateSummary(): void {
        const nutritionState = this.mealManageFacade.buildNutritionSummaryStateFromValues(
            this.getMealFormValue(),
            this.aiSessions(),
            this.calorieMismatchThreshold,
        );

        this.applySummary(nutritionState);

        if (this.mealFormModel().isNutritionAutoCalculated) {
            this.patchManualNutritionFromTotals(nutritionState.autoTotals);
        }
    }

    private applySummary(nutritionState: MealNutritionSummaryState): void {
        if (this.totalCalories() !== nutritionState.summaryTotals.calories) {
            this.totalCalories.set(nutritionState.summaryTotals.calories);
        }

        if (this.totalFiber() !== nutritionState.summaryTotals.fiber) {
            this.totalFiber.set(nutritionState.summaryTotals.fiber);
        }

        if (this.totalAlcohol() !== nutritionState.summaryTotals.alcohol) {
            this.totalAlcohol.set(nutritionState.summaryTotals.alcohol);
        }

        const currentNutrientData = this.nutrientChartData();
        if (
            currentNutrientData.proteins !== nutritionState.summaryTotals.proteins ||
            currentNutrientData.fats !== nutritionState.summaryTotals.fats ||
            currentNutrientData.carbs !== nutritionState.summaryTotals.carbs
        ) {
            this.nutrientChartData.set({
                proteins: nutritionState.summaryTotals.proteins,
                fats: nutritionState.summaryTotals.fats,
                carbs: nutritionState.summaryTotals.carbs,
            });
        }

        if (this.nutritionWarning() !== nutritionState.warning) {
            this.nutritionWarning.set(nutritionState.warning);
        }
    }

    private populateManualNutritionFromCurrentSummary(): void {
        const nutritionState = this.mealManageFacade.buildNutritionSummaryStateFromValues(
            this.getMealFormValue(),
            this.aiSessions(),
            this.calorieMismatchThreshold,
        );
        this.patchManualNutritionFromTotals(nutritionState.autoTotals);
    }

    private patchManualNutritionFromTotals(totals: NutritionTotals): void {
        this.patchMealFormModel(this.mealManageFacade.buildManualNutritionPatchFromTotals(totals));
    }

    private async addMealAsync(mealData: MealManageDto): Promise<void> {
        const fingerprint = JSON.stringify(mealData);
        if (this.createIntent?.fingerprint !== fingerprint) {
            this.createIntent = { fingerprint, key: crypto.randomUUID() };
        }
        const response = await this.mealManageFacade.submitMealAsync(null, mealData, this.createIntent.key);
        await this.handleSubmitResponseAsync(null, response);
    }

    private async updateMealAsync(meal: Meal, mealData: MealManageDto): Promise<void> {
        const response = await this.mealManageFacade.submitMealAsync(meal, mealData);
        await this.handleSubmitResponseAsync(meal, response);
    }

    private async handleSubmitResponseAsync(meal: Meal | null, response: Meal | null): Promise<void> {
        if (!this.isCurrentMeal(meal)) {
            return;
        }
        if (response !== null) {
            if (meal === null) {
                this.createIntent = null;
                const resetValue = {
                    date: this.getDateInputValue(new Date()),
                    time: this.getTimeInputValue(new Date()),
                    mealType: resolveMealTypeByTime(new Date()),
                    comment: null,
                    isNutritionAutoCalculated: true,
                    manualCalories: null,
                    manualProteins: null,
                    manualFats: null,
                    manualCarbs: null,
                    manualFiber: null,
                    manualAlcohol: null,
                };
                this.mealFormModel.set({
                    ...createMealManageFormValue(),
                    ...resetValue,
                });
                this.mealSignalForm().reset(this.mealFormModel());
                this.aiSessions.set([]);
                this.itemsTouchedState.reset();
                this.updateSummary();
            }
            this.mealSignalForm().reset(this.mealFormModel());
            await this.mealManageFacade.showSuccessToastAndRedirectAsync(meal !== null);
        } else {
            this.handleSubmitError();
        }
    }

    private closeDialogOnMealChange(meal: Meal | null, close: () => void): EffectRef {
        return effect(
            () => {
                if (!this.isCurrentMeal(meal)) {
                    untracked(close);
                }
            },
            { injector: this.injector },
        );
    }

    private isCurrentMeal(meal: Meal | null): boolean {
        return !this.destroyRef.destroyed && this.meal() === meal;
    }

    private async saveWithConsentAsync(meal: Meal | null, mealData: MealManageDto): Promise<void> {
        const allowed = await this.privacy.ensureHealthConsentAsync(true, { acceptLabelKey: 'PRIVACY_CHOICES.ACCEPT_AND_SAVE' });
        if (!this.isCurrentMeal(meal)) {
            return;
        }
        if (allowed) {
            await (meal !== null ? this.updateMealAsync(meal, mealData) : this.addMealAsync(mealData));
        } else if (this.privacy.failed()) {
            this.setGlobalError(this.privacy.needsReload() ? 'PRIVACY_CHOICES.RELOAD' : 'PRIVACY_CHOICES.ERROR');
        }
    }

    private handleSubmitError(error?: HttpErrorResponse): void {
        if (isHealthConsentRequiredError(error)) {
            if (!Boolean(this.privacy.state()?.healthDeclinedAtUtc)) {
                this.setGlobalError(this.privacy.needsReload() ? 'PRIVACY_CHOICES.RELOAD' : 'PRIVACY_CHOICES.ERROR');
            }
            return;
        }
        if (error?.status === 0) {
            this.setGlobalError('FORM_ERRORS.SAVE_CONNECTION');
            return;
        }
        const message = this.getSubmitErrorMessage(error);
        if (message !== null) {
            this.globalError.set(message);
            return;
        }

        this.setGlobalError('QUICK_MEAL.SAVE_ERROR');
    }

    private getSubmitErrorMessage(error?: HttpErrorResponse): string | null {
        const responseBody = getRecordProperty(error, 'error');
        return getStringProperty(responseBody, 'message') ?? null;
    }

    private setGlobalError(errorKey: string): void {
        this.globalError.set(this.translateService.instant(errorKey));
    }

    private clearGlobalError(): void {
        this.globalError.set(null);
    }

    private ensurePremiumAccess(): boolean {
        return this.mealManageFacade.ensurePremiumAccess();
    }

    private findReusableEmptyItemIndex(): number {
        return findReusableEmptyMealItemIndex(this.items);
    }

    private buildMealTypeOptions(): void {
        this.mealTypeSelectOptions = buildMealTypeSelectOptions(this.translateService);
    }

    private getDateInputValue(date: Date): string {
        return getDateInputValue(date);
    }

    private getTimeInputValue(date: Date): string {
        return getTimeInputValue(date);
    }

    private buildDateTime(): Date {
        const formValue = this.mealFormModel();
        const original = this.meal();
        return buildMealDateTime(formValue.date, formValue.time, new Date(), original !== null ? new Date(original.date) : undefined);
    }

    private getMealFormValue(): MealFormValues {
        return this.mealFormModel();
    }

    private patchMealFormModel(patch: Partial<CommittedMealFormValues>): void {
        patchSignalFormModel(this.mealFormModel, patch);
    }

    private getSignalNutritionControlState(
        field: keyof Pick<MealFormValues, 'manualCalories' | 'manualProteins' | 'manualFats' | 'manualCarbs' | 'manualAlcohol'>,
    ): {
        value: number | null;
        touched: boolean;
        dirty: boolean;
    } {
        const state = this.mealSignalForm[field]();
        return {
            value: state.value(),
            touched: state.touched(),
            dirty: state.dirty(),
        };
    }

    private replaceMealItem(index: number, item: CommittedMealItemFormValues): void {
        this.patchMealFormModel({
            items: this.items.map((currentItem, currentIndex) => (currentIndex === index ? item : currentItem)),
        });
    }

    private hasSelectedItems(): boolean {
        return hasSelectedMealItems(this.items, this.aiSessions());
    }

    private getItemAmountError(item: MealItemFormValues, touched: boolean): string | null {
        if (!touched || (item.product === null && item.recipe === null)) {
            return null;
        }

        if (item.amount === null) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        if (!Number.isFinite(item.amount) || item.amount < MEAL_MANAGE_MIN_ITEM_AMOUNT) {
            return this.translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', {
                min: MEAL_MANAGE_MIN_ITEM_AMOUNT,
            });
        }

        return null;
    }
}

import { HttpErrorResponse } from '@angular/common/http';
import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    inject,
    Injector,
    input,
    signal,
    untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormRoot, max, required } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_VALIDATION_ERRORS, type FdValidationErrors, resolveSignalFormFieldError } from 'fd-ui-kit/form-error/fd-ui-form-error';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { firstValueFrom } from 'rxjs';

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
import { DEFAULT_SATIETY_LEVEL, normalizeSatietyLevel } from '../../../../shared/lib/satiety-level.utils';
import { patchSignalFormModel } from '../../../../shared/lib/signal-form-model.utils';
import { getRecordProperty, getStringProperty } from '../../../../shared/lib/unknown-value.utils';
import type { NutrientData } from '../../../../shared/models/charts.data';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { MEAL_MANAGE_MIN_ITEM_AMOUNT } from '../../lib/manage/meal-manage.config';
import { MealManageFacade } from '../../lib/manage/meal-manage.facade';
import {
    type Consumption,
    type ConsumptionAiSessionManageDto,
    type ConsumptionItem,
    type ConsumptionManageDto,
    ConsumptionSourceType,
} from '../../models/meal.data';
import { type MealGeneralFieldErrors, MealGeneralInfoComponent } from './meal-general-info/meal-general-info';
import type { MealItemsListItemState } from './meal-items-list/meal-items-list';
import { MealItemsSectionComponent } from './meal-items-section/meal-items-section';
import type {
    CalorieMismatchWarning,
    ConsumptionFormValues,
    ConsumptionItemFormValues,
    MacroBarState,
    MealNutritionSummaryState,
    NutritionMode,
    NutritionTotals,
} from './meal-manage-lib/meal-manage.types';
import {
    buildMealManageDto,
    buildMealManageFormPatchValue,
    createMealManageFormValue,
    getConsumptionItemInitialAmount,
    getDateInputValue,
    getTimeInputValue,
} from './meal-manage-lib/meal-manage-form.mapper';
import { buildMealTypeSelectOptions, type MealSatietyControlName } from './meal-manage-lib/meal-manage-options.mapper';
import { MEAL_MANAGE_TOUR } from './meal-manage-tour';
import { MealManualItemDialogComponent, type MealManualItemDialogData } from './meal-manual-item-dialog/meal-manual-item-dialog';
import { MealNutritionSidebarComponent } from './meal-nutrition-sidebar/meal-nutrition-sidebar';
import { MealSatietyCardComponent } from './meal-satiety-card/meal-satiety-card';

const GENERAL_ERROR_FIELDS = ['date', 'time', 'mealType'] as const;
@Component({
    selector: 'fd-meal-manage-form',
    templateUrl: './meal-manage-form.html',
    styleUrls: ['./meal-manage-form.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        PageBodyComponent,
        PageHeaderComponent,
        FdPageContainerDirective,
        FormRoot,
        MealGeneralInfoComponent,
        MealSatietyCardComponent,
        MealItemsSectionComponent,
        MealNutritionSidebarComponent,
    ],
})
export class MealManageFormComponent {
    private readonly translateService = inject(TranslateService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly injector = inject(Injector);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly mealManageFacade = inject(MealManageFacade);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    private readonly calorieMismatchThreshold = DEFAULT_CALORIE_MISMATCH_THRESHOLD;
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    private readonly languageVersion = signal(0);

    public readonly consumption = input<Consumption | null>(null);
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
    protected readonly isSubmitDisabled = computed(() => this.isSubmitting() || !this.hasSelectedItems());
    protected readonly aiSessions = signal<ConsumptionAiSessionManageDto[]>([]);
    private readonly itemsTouchedState = createCollectionTouchedState({
        hasItems: () => this.hasSelectedItems(),
        errorMessage: () => this.translateService.instant('FORM_ERRORS.NON_EMPTY_ARRAY'),
        dependencies: [this.languageVersion],
    });
    protected readonly itemsTouched = this.itemsTouchedState.touched;
    protected readonly nutritionMode = signal<NutritionMode>('auto');
    protected readonly preMealSatietyLevel = signal<number | null>(DEFAULT_SATIETY_LEVEL);
    protected readonly postMealSatietyLevel = signal<number | null>(DEFAULT_SATIETY_LEVEL);
    protected readonly selectedMealType = signal<string | null>(null);
    protected readonly nutritionWarning = signal<CalorieMismatchWarning | null>(null);
    protected readonly generalFieldErrors = computed<MealGeneralFieldErrors>(() => {
        this.languageVersion();

        return GENERAL_ERROR_FIELDS.reduce<MealGeneralFieldErrors>((errors, field) => {
            errors[field] = resolveSignalFormFieldError(this.consumptionSignalForm[field], this.validationErrors, this.translateService);
            return errors;
        }, this.createEmptyGeneralFieldErrors());
    });
    protected readonly consumptionFormModel = signal<ConsumptionFormValues>(createMealManageFormValue());
    private readonly submitConsumptionFormAsync = async (): Promise<void> => {
        await this.onSubmitAsync();
    };
    protected readonly consumptionSignalForm = form(
        this.consumptionFormModel,
        path => {
            required(path.date);
            required(path.time);
            max(path.manualCalories, MANUAL_NUTRITION_MAX_CALORIES);
            max(path.manualProteins, MANUAL_NUTRITION_MAX_NUTRIENT);
            max(path.manualFats, MANUAL_NUTRITION_MAX_NUTRIENT);
            max(path.manualCarbs, MANUAL_NUTRITION_MAX_NUTRIENT);
            max(path.manualFiber, MANUAL_NUTRITION_MAX_NUTRIENT);
            max(path.manualAlcohol, MANUAL_NUTRITION_MAX_NUTRIENT);
        },
        {
            submission: {
                action: this.submitConsumptionFormAsync,
                onInvalid: () => {
                    this.handleInvalidSubmit();
                },
            },
        },
    );
    protected readonly manageHeaderState = computed(() => ({
        titleKey: this.consumption() !== null ? 'CONSUMPTION_MANAGE.EDIT_TITLE' : 'CONSUMPTION_MANAGE.ADD_TITLE',
    }));
    private populatedConsumption: Consumption | null = null;

    protected readonly macroBarState = computed<MacroBarState>(() => {
        const nutrients = this.nutrientChartData();
        return calculateMacroBarState(nutrients.proteins, nutrients.fats, nutrients.carbs);
    });
    protected readonly itemListItems = computed<readonly MealItemsListItemState[]>(() => {
        const touched = this.itemsTouched();

        return this.consumptionFormModel().items.map(value => {
            const productInvalid = touched && value.sourceType === ConsumptionSourceType.Product && value.product === null;
            const recipeInvalid = touched && value.sourceType === ConsumptionSourceType.Recipe && value.recipe === null;
            const sourceError =
                productInvalid || recipeInvalid ? this.translateService.instant('CONSUMPTION_MANAGE.ITEM_SOURCE_ERROR') : null;
            const amountError = this.getItemAmountError(value, touched);
            return {
                ...value,
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
        this.buildMealTypeOptions();
        this.watchLanguageChanges();
        this.watchSignalFormModelChanges();

        const presetMealType = this.resolvePresetMealType();
        if (presetMealType !== null) {
            this.patchConsumptionFormModel({ mealType: presetMealType });
        } else if (this.consumption() === null) {
            this.setAutoMealTypeFromDate();
        }

        this.watchConsumptionInput();

        if (presetMealType === null && this.consumption() === null) {
            this.watchAutoMealTypeChanges();
        }
    }

    private watchLanguageChanges(): void {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
            this.buildMealTypeOptions();
        });
    }

    private watchSignalFormModelChanges(): void {
        effect(() => {
            const value = this.consumptionFormModel();
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

    private watchConsumptionInput(): void {
        effect(() => {
            const consumption = this.consumption();
            untracked(() => {
                if (consumption === null) {
                    this.populatedConsumption = null;
                    return;
                }

                if (this.populatedConsumption === consumption) {
                    return;
                }

                this.populatedConsumption = consumption;
                this.populateForm(consumption);
                this.updateSummary();
            });
        });
    }

    private watchAutoMealTypeChanges(): void {
        effect(() => {
            this.setAutoMealTypeFromDate();
        });
    }

    protected async onCancelAsync(): Promise<void> {
        if (this.consumptionSignalForm().dirty()) {
            const shouldLeave = await this.mealManageFacade.confirmDiscardChangesAsync({
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

        await this.navigationService.navigateToConsumptionListAsync();
    }

    protected startMealManageTour(force = true): void {
        this.tourService.start(this.localizedTour.build(MEAL_MANAGE_TOUR), { force });
    }

    protected get items(): ConsumptionItemFormValues[] {
        return this.consumptionFormModel().items;
    }

    // --- Item management (delegated from MealItemsListComponent events) ---

    protected addConsumptionItem(): void {
        const reusableIndex = this.findReusableEmptyItemIndex();
        const itemIndex = reusableIndex >= 0 ? reusableIndex : this.items.length;

        if (reusableIndex < 0) {
            this.patchConsumptionFormModel({
                items: [...this.items, this.mealManageFacade.createConsumptionItem()],
            });
        }

        afterNextRender(
            () => {
                this.openManualItemDialog(itemIndex);
            },
            { injector: this.injector },
        );
    }

    protected removeItem(index: number): void {
        this.patchConsumptionFormModel({
            items: this.items.filter((_, currentIndex) => currentIndex !== index),
        });
    }

    protected onItemSourceClick(index: number): void {
        this.openManualItemDialog(index);
    }

    protected openManualItemDialog(index: number): void {
        const item = this.items[index];
        void firstValueFrom(
            this.fdDialogService
                .open<MealManualItemDialogComponent, MealManualItemDialogData, ConsumptionItemFormValues | null>(
                    MealManualItemDialogComponent,
                    {
                        preset: 'form',
                        data: { item },
                    },
                )
                .afterClosed(),
        ).then(selectedItem => {
            if (selectedItem === null || selectedItem === undefined) {
                return;
            }

            this.replaceConsumptionItem(index, this.mealManageFacade.configureItemType(selectedItem, selectedItem.sourceType));
            this.itemsTouchedState.markTouched();
            this.updateSummary();
        });
    }

    // --- AI session management ---

    protected onAiMealRecognized(result: AiInputBarResult): void {
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

    protected onDeleteAiSession(index: number): void {
        this.aiSessions.update(current => this.mealManageFacade.removeAiSession(current, index));
        this.updateSummary();
    }

    protected onEditAiSession(index: number): void {
        if (!this.ensurePremiumAccess()) {
            return;
        }

        const session = (this.aiSessions() as Array<ConsumptionAiSessionManageDto | undefined>)[index];
        if (session === undefined) {
            return;
        }

        void this.mealManageFacade.openEditAiPhotoSessionDialogAsync(session).then(updated => {
            if (updated === null) {
                return;
            }
            this.aiSessions.update(current => this.mealManageFacade.replaceAiSession(current, index, updated));
            this.updateSummary();
        });
    }

    // --- Nutrition mode (delegated from MealNutritionSidebarComponent events) ---

    protected onNutritionModeChange(nextMode: string): void {
        const resolvedMode: NutritionMode = nextMode === 'manual' ? 'manual' : 'auto';
        if (this.nutritionMode() === resolvedMode) {
            return;
        }

        this.nutritionMode.set(resolvedMode);
        const isAuto = resolvedMode === 'auto';
        this.patchConsumptionFormModel({ isNutritionAutoCalculated: isAuto });
        if (!isAuto) {
            this.populateManualNutritionFromCurrentSummary();
        }
        this.updateSummary();
    }

    protected caloriesError(): string | null {
        if (this.consumptionFormModel().isNutritionAutoCalculated) {
            return null;
        }

        return checkCaloriesError(this.getSignalNutritionControlState('manualCalories'))
            ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.CALORIES_REQUIRED')
            : null;
    }

    protected macrosError(): string | null {
        if (this.consumptionFormModel().isNutritionAutoCalculated) {
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
        this.patchConsumptionFormModel({ [controlName]: normalizedValue });

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
        };
    }

    // --- Submit ---

    protected async onSubmitAsync(): Promise<void> {
        if (this.isSubmitting()) {
            return;
        }

        this.consumptionSignalForm().markAsTouched();
        this.itemsTouchedState.markTouched();

        if (this.macrosError() !== null) {
            return;
        }

        if (!this.hasSelectedItems()) {
            this.setGlobalError('FORM_ERRORS.NON_EMPTY_ARRAY');
            return;
        }

        if (this.consumptionSignalForm().invalid()) {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
            return;
        }

        const consumptionData = this.buildConsumptionManageDto();
        const consumption = this.consumption();
        this.isSubmitting.set(true);
        try {
            await (consumption !== null ? this.updateConsumptionAsync(consumptionData) : this.addConsumptionAsync(consumptionData));
        } catch (error: unknown) {
            this.handleSubmitError(error instanceof HttpErrorResponse ? error : new HttpErrorResponse({ error }));
        } finally {
            this.isSubmitting.set(false);
        }
    }

    private handleInvalidSubmit(): void {
        this.itemsTouchedState.markTouched();
        this.setGlobalError('FORM_ERRORS.UNKNOWN');
    }

    private buildConsumptionManageDto(): ConsumptionManageDto {
        return buildMealManageDto(this.getConsumptionFormValue(), {
            aiSessions: this.aiSessions(),
            buildDateTime: () => this.buildDateTime(),
            convertRecipeGramsToServings: (recipe, amount) => this.mealManageFacade.convertRecipeGramsToServings(recipe, amount),
            manualTotals: this.mealManageFacade.getManualNutritionTotalsFromValue(this.consumptionFormModel()),
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
        if (this.consumption() !== null) {
            return;
        }

        if (this.consumptionSignalForm.mealType().dirty()) {
            return;
        }

        const date = this.buildDateTime();
        if (Number.isNaN(date.getTime())) {
            return;
        }

        const mealType = resolveMealTypeByTime(date);
        if (this.consumptionFormModel().mealType === mealType) {
            return;
        }

        this.patchConsumptionFormModel({ mealType });
        this.selectedMealType.set(mealType);
    }

    private populateForm(consumption: Consumption): void {
        const patch = buildMealManageFormPatchValue(consumption);
        this.patchConsumptionFormModel(patch);
        this.aiSessions.set(consumption.aiSessions ?? []);

        if (consumption.items.length === 0) {
            this.patchConsumptionFormModel({ items: [this.mealManageFacade.createConsumptionItem()] });
            return;
        }

        this.patchConsumptionFormModel({
            items: consumption.items.map(item => this.createConsumptionItemFromConsumption(item)),
        });
    }

    private createConsumptionItemFromConsumption(item: ConsumptionItem): ConsumptionItemFormValues {
        const sourceType = item.sourceType;
        const initialAmount = getConsumptionItemInitialAmount(item, value =>
            this.mealManageFacade.convertRecipeServingsToGrams(value.recipe ?? null, value.amount),
        );

        return this.mealManageFacade.createConsumptionItem(
            sourceType === ConsumptionSourceType.Product ? (item.product ?? null) : null,
            sourceType === ConsumptionSourceType.Recipe ? (item.recipe ?? null) : null,
            initialAmount,
            sourceType,
        );
    }

    private updateSummary(): void {
        const nutritionState = this.mealManageFacade.buildNutritionSummaryStateFromValues(
            this.getConsumptionFormValue(),
            this.aiSessions(),
            this.calorieMismatchThreshold,
        );

        this.applySummary(nutritionState);

        if (this.consumptionFormModel().isNutritionAutoCalculated) {
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
            this.getConsumptionFormValue(),
            this.aiSessions(),
            this.calorieMismatchThreshold,
        );
        this.patchManualNutritionFromTotals(nutritionState.autoTotals);
    }

    private patchManualNutritionFromTotals(totals: NutritionTotals): void {
        this.patchConsumptionFormModel(this.mealManageFacade.buildManualNutritionPatchFromTotals(totals));
    }

    private async addConsumptionAsync(consumptionData: ConsumptionManageDto): Promise<void> {
        const response = await this.mealManageFacade.submitConsumptionAsync(null, consumptionData);
        await this.handleSubmitResponseAsync(response);
    }

    private async updateConsumptionAsync(consumptionData: ConsumptionManageDto): Promise<void> {
        const response = await this.mealManageFacade.submitConsumptionAsync(this.consumption() ?? null, consumptionData);
        await this.handleSubmitResponseAsync(response);
    }

    private async handleSubmitResponseAsync(response: Consumption | null): Promise<void> {
        if (response !== null) {
            if (this.consumption() === null) {
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
                this.consumptionFormModel.set({
                    ...createMealManageFormValue(),
                    ...resetValue,
                });
                this.aiSessions.set([]);
                this.itemsTouchedState.reset();
                this.updateSummary();
            }
            await this.mealManageFacade.showSuccessToastAndRedirectAsync(Boolean(this.consumption()));
        } else {
            this.handleSubmitError();
        }
    }

    private handleSubmitError(error?: HttpErrorResponse): void {
        const message = this.getSubmitErrorMessage(error);
        if (message !== null) {
            this.globalError.set(message);
            return;
        }

        this.setGlobalError('FORM_ERRORS.UNKNOWN');
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
        return this.items.findIndex(item => item.product === null && item.recipe === null);
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
        const dateValue = this.consumptionFormModel().date;
        const timeValue = this.consumptionFormModel().time;
        const combined = `${dateValue}T${timeValue}`;
        const parsed = new Date(combined);
        return Number.isNaN(parsed.getTime()) ? new Date() : parsed;
    }

    private getConsumptionFormValue(): ConsumptionFormValues {
        return this.consumptionFormModel();
    }

    private patchConsumptionFormModel(patch: Partial<ConsumptionFormValues>): void {
        patchSignalFormModel(this.consumptionFormModel, patch);
    }

    private getSignalNutritionControlState(
        field: keyof Pick<ConsumptionFormValues, 'manualCalories' | 'manualProteins' | 'manualFats' | 'manualCarbs' | 'manualAlcohol'>,
    ): {
        value: number | null;
        touched: boolean;
        dirty: boolean;
    } {
        const state = this.consumptionSignalForm[field]();
        return {
            value: state.value(),
            touched: state.touched(),
            dirty: state.dirty(),
        };
    }

    private replaceConsumptionItem(index: number, item: ConsumptionItemFormValues): void {
        this.patchConsumptionFormModel({
            items: this.items.map((currentItem, currentIndex) => (currentIndex === index ? item : currentItem)),
        });
    }

    private hasSelectedItems(): boolean {
        return this.items.some(item => item.product !== null || item.recipe !== null) || this.aiSessions().length > 0;
    }

    private getItemAmountError(item: ConsumptionItemFormValues, touched: boolean): string | null {
        if (!touched || (item.product === null && item.recipe === null)) {
            return null;
        }

        if (item.amount === null) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        if (item.amount < MEAL_MANAGE_MIN_ITEM_AMOUNT) {
            return this.translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', {
                min: MEAL_MANAGE_MIN_ITEM_AMOUNT,
            });
        }

        return null;
    }
}

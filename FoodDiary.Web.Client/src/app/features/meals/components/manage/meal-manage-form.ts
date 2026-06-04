import { HttpErrorResponse } from '@angular/common/http';
import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    type FactoryProvider,
    inject,
    input,
    signal,
    untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FieldTree, form, required, type ValidationError } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_VALIDATION_ERRORS, type FdValidationErrors, getNumberProperty } from 'fd-ui-kit/form-error/fd-ui-form-error';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { EMPTY, firstValueFrom, merge, type Observable } from 'rxjs';

import type { AiInputBarResult } from '../../../../components/shared/ai-input-bar/ai-input-bar.types';
import { ManageHeaderComponent } from '../../../../components/shared/manage-header/manage-header';
import { NavigationService } from '../../../../services/navigation.service';
import { normalizeMealType, resolveMealTypeByTime } from '../../../../shared/lib/meal-type.util';
import { DEFAULT_CALORIE_MISMATCH_THRESHOLD } from '../../../../shared/lib/nutrition.constants';
import { calculateMacroBarState, checkCaloriesError, checkMacrosError } from '../../../../shared/lib/nutrition-form.utils';
import { DEFAULT_SATIETY_LEVEL, normalizeSatietyLevel } from '../../../../shared/lib/satiety-level.utils';
import { getRecordProperty, getStringProperty } from '../../../../shared/lib/unknown-value.utils';
import type { NutrientData } from '../../../../shared/models/charts.data';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
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
    createMealManageForm,
    createMealManageFormValue,
    getConsumptionItemInitialAmount,
    getDateInputValue,
    getTimeInputValue,
} from './meal-manage-lib/meal-manage-form.mapper';
import { buildMealTypeSelectOptions, type MealSatietyControlName } from './meal-manage-lib/meal-manage-options.mapper';
import { resolveMealManageControlError } from './meal-manage-lib/meal-manage-view.utils';
import { MealManualItemDialogComponent, type MealManualItemDialogData } from './meal-manual-item-dialog/meal-manual-item-dialog';
import { MealNutritionSidebarComponent } from './meal-nutrition-sidebar/meal-nutrition-sidebar';
import { MealSatietyCardComponent } from './meal-satiety-card/meal-satiety-card';

export type { ConsumptionFormData, ConsumptionItemFormData } from './meal-manage-lib/meal-manage.types';

export const VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: FD_VALIDATION_ERRORS,
    useFactory: (): FdValidationErrors => ({
        required: () => 'FORM_ERRORS.REQUIRED',
        nonEmptyArray: () => 'FORM_ERRORS.NON_EMPTY_ARRAY',
        min: (error?: unknown) => ({
            key: 'FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO',
            params: { min: getNumberProperty(error, 'min') },
        }),
    }),
};

const GENERAL_ERROR_FIELDS = ['date', 'time', 'mealType'] as const;
const SIGNAL_MANAGED_FORM_FIELDS = [
    'date',
    'time',
    'mealType',
    'comment',
    'imageUrl',
    'isNutritionAutoCalculated',
    'manualCalories',
    'manualProteins',
    'manualFats',
    'manualCarbs',
    'manualFiber',
    'manualAlcohol',
] as const satisfies ReadonlyArray<keyof ConsumptionFormValues>;

@Component({
    selector: 'fd-meal-manage-form',
    templateUrl: './meal-manage-form.html',
    styleUrls: ['./meal-manage-form.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [VALIDATION_ERRORS_PROVIDER],
    imports: [
        TranslatePipe,
        ManageHeaderComponent,
        FdPageContainerDirective,
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
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly mealManageFacade = inject(MealManageFacade);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly calorieMismatchThreshold = DEFAULT_CALORIE_MISMATCH_THRESHOLD;

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
    protected readonly aiSessions = signal<ConsumptionAiSessionManageDto[]>([]);
    protected readonly itemsRenderVersion = signal(0);
    protected readonly nutritionMode = signal<NutritionMode>('auto');
    protected readonly preMealSatietyLevel = signal<number | null>(DEFAULT_SATIETY_LEVEL);
    protected readonly postMealSatietyLevel = signal<number | null>(DEFAULT_SATIETY_LEVEL);
    protected readonly selectedMealType = signal<string | null>(null);
    protected readonly nutritionWarning = signal<CalorieMismatchWarning | null>(null);
    protected readonly generalFieldErrors = signal<MealGeneralFieldErrors>(this.createEmptyGeneralFieldErrors());
    protected readonly consumptionFormModel = signal<ConsumptionFormValues>(createMealManageFormValue());
    protected readonly consumptionSignalForm = form(this.consumptionFormModel, path => {
        required(path.date);
        required(path.time);
    });
    protected readonly manageHeaderState = computed(() => ({
        titleKey: this.consumption() !== null ? 'CONSUMPTION_MANAGE.EDIT_TITLE' : 'CONSUMPTION_MANAGE.ADD_TITLE',
    }));
    private populatedConsumption: Consumption | null = null;

    protected readonly macroBarState = computed<MacroBarState>(() => {
        const nutrients = this.nutrientChartData();
        return calculateMacroBarState(nutrients.proteins, nutrients.fats, nutrients.carbs);
    });
    protected readonly itemListItems = computed<readonly MealItemsListItemState[]>(() => {
        this.itemsRenderVersion();

        return this.items.controls.map(group => {
            const value = group.getRawValue();
            const productInvalid =
                value.sourceType === ConsumptionSourceType.Product && group.controls.product.invalid && group.controls.product.touched;
            const recipeInvalid =
                value.sourceType === ConsumptionSourceType.Recipe && group.controls.recipe.invalid && group.controls.recipe.touched;
            const sourceError =
                productInvalid || recipeInvalid ? this.translateService.instant('CONSUMPTION_MANAGE.ITEM_SOURCE_ERROR') : null;

            return {
                ...value,
                amountError: resolveMealManageControlError(group.controls.amount, this.translateService),
                productInvalid,
                recipeInvalid,
                sourceError,
            };
        });
    });
    protected readonly itemsError = computed(() => {
        this.itemsRenderVersion();
        return this.items.touched && this.items.errors?.['nonEmptyArray'] === true
            ? this.translateService.instant('FORM_ERRORS.NON_EMPTY_ARRAY')
            : null;
    });

    protected consumptionForm: MealLegacyForm;
    protected mealTypeSelectOptions: Array<FdUiSelectOption<string>> = [];

    public constructor() {
        this.consumptionForm = this.createConsumptionForm();
        this.buildMealTypeOptions();
        this.nutritionMode.set(this.consumptionForm.controls.isNutritionAutoCalculated.value ? 'auto' : 'manual');
        this.watchLanguageChanges();
        this.watchGeneralFieldErrors();
        this.watchSignalFormModelChanges();
        this.updateGeneralFieldErrors();
        this.watchSatietyChanges();
        this.watchMealTypeChanges();
        this.updateManualNutritionValidators(true);
        this.updateItemValidationRules();
        this.watchNutritionModeChanges();

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

        this.watchFormChanges();
    }

    private createConsumptionForm(): MealLegacyForm {
        return createMealManageForm({
            createItem: () => this.mealManageFacade.createConsumptionItem(),
            createItemsRule: () => this.mealManageFacade.createItemsRule(() => this.aiSessions()),
        });
    }

    private watchLanguageChanges(): void {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildMealTypeOptions();
            this.updateGeneralFieldErrors();
            this.bumpItemsRenderVersion();
        });
    }

    private watchGeneralFieldErrors(): void {
        effect(() => {
            this.consumptionSignalForm.date().errors();
            this.consumptionSignalForm.date().touched();
            this.consumptionSignalForm.date().dirty();
            this.consumptionSignalForm.time().errors();
            this.consumptionSignalForm.time().touched();
            this.consumptionSignalForm.time().dirty();
            this.consumptionSignalForm.mealType().errors();
            this.consumptionSignalForm.mealType().touched();
            this.consumptionSignalForm.mealType().dirty();
            this.updateGeneralFieldErrors();
        });

        const formEvents = (this.consumptionForm as { events?: Observable<unknown> }).events ?? EMPTY;
        merge(formEvents, this.consumptionForm.statusChanges, this.consumptionForm.valueChanges)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.updateGeneralFieldErrors();
            });
    }

    private watchSignalFormModelChanges(): void {
        effect(() => {
            const value = this.consumptionFormModel();
            untracked(() => {
                this.syncSignalManagedValuesToLegacyForm(value);
                this.selectedMealType.set(value.mealType);
                this.updateGeneralFieldErrors();
                this.updateSummary();
                this.clearGlobalError();
            });
        });
    }

    private watchSatietyChanges(): void {
        merge(
            this.consumptionForm.controls.preMealSatietyLevel.valueChanges,
            this.consumptionForm.controls.postMealSatietyLevel.valueChanges,
        )
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.preMealSatietyLevel.set(this.consumptionForm.controls.preMealSatietyLevel.value);
                this.postMealSatietyLevel.set(this.consumptionForm.controls.postMealSatietyLevel.value);
            });
    }

    private watchMealTypeChanges(): void {
        this.consumptionForm.controls.mealType.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(value => {
            this.selectedMealType.set(value);
        });
    }

    private watchNutritionModeChanges(): void {
        this.consumptionForm.controls.isNutritionAutoCalculated.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(isAuto => {
            this.nutritionMode.set(isAuto ? 'auto' : 'manual');
            this.patchConsumptionFormModel({ isNutritionAutoCalculated: isAuto });
            this.updateManualNutritionValidators(isAuto);
            if (!isAuto) {
                this.populateManualNutritionFromCurrentSummary();
            }
            this.updateSummary();
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
                this.updateItemValidationRules();
                this.updateSummary();
            });
        });
    }

    private watchAutoMealTypeChanges(): void {
        effect(() => {
            this.consumptionFormModel().date;
            this.consumptionFormModel().time;
            this.setAutoMealTypeFromDate();
        });

        this.consumptionForm.controls.date.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.setAutoMealTypeFromDate();
        });
        this.consumptionForm.controls.time.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.setAutoMealTypeFromDate();
        });
    }

    private watchFormChanges(): void {
        this.consumptionForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.updateItemValidationRules();
            this.updateSummary();
            this.clearGlobalError();
        });
    }

    protected async onCancelAsync(): Promise<void> {
        await this.navigationService.navigateToConsumptionListAsync();
    }

    protected get items(): MealLegacyItemsControl {
        return this.consumptionForm.controls.items;
    }

    // --- Item management (delegated from MealItemsListComponent events) ---

    protected addConsumptionItem(): void {
        const reusableIndex = this.findReusableEmptyItemIndex();
        const itemIndex = reusableIndex >= 0 ? reusableIndex : this.items.length;

        if (reusableIndex < 0) {
            this.items.push(this.mealManageFacade.createConsumptionItem());
        }

        queueMicrotask(() => {
            this.openManualItemDialog(itemIndex);
        });
    }

    protected removeItem(index: number): void {
        this.items.removeAt(index);
        this.bumpItemsRenderVersion();
    }

    protected onItemSourceClick(index: number): void {
        this.openManualItemDialog(index);
    }

    protected openManualItemDialog(index: number): void {
        const group = this.items.at(index);
        void firstValueFrom(
            this.fdDialogService
                .open<MealManualItemDialogComponent, MealManualItemDialogData, ConsumptionItemFormValues | null>(
                    MealManualItemDialogComponent,
                    {
                        preset: 'form',
                        data: { item: group.getRawValue() },
                    },
                )
                .afterClosed(),
        ).then(item => {
            if (item !== null && item !== undefined) {
                group.patchValue(item);
                this.mealManageFacade.configureItemType(group, item.sourceType);
                this.bumpItemsRenderVersion();
                this.updateItemValidationRules();
                this.updateSummary();
            }
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
        this.items.updateValueAndValidity({ emitEvent: false });
        this.updateItemValidationRules();
        this.updateSummary();
    }

    protected onDeleteAiSession(index: number): void {
        this.aiSessions.update(current => this.mealManageFacade.removeAiSession(current, index));
        this.items.updateValueAndValidity({ emitEvent: false });
        this.updateItemValidationRules();
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
            this.items.updateValueAndValidity({ emitEvent: false });
            this.updateItemValidationRules();
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
        this.syncSignalManagedValuesToLegacyForm(this.consumptionFormModel());
        this.updateManualNutritionValidators(isAuto);
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
        const control = this.consumptionForm.controls[controlName];
        control.setValue(normalizeSatietyLevel(value));
        control.markAsDirty();
        control.markAsTouched();
    }

    // --- Form control helpers ---

    private updateGeneralFieldErrors(): void {
        this.generalFieldErrors.set(
            GENERAL_ERROR_FIELDS.reduce<MealGeneralFieldErrors>((errors, field) => {
                errors[field] = this.getSignalFieldError(this.consumptionSignalForm[field]);
                return errors;
            }, this.createEmptyGeneralFieldErrors()),
        );
    }

    private createEmptyGeneralFieldErrors(): MealGeneralFieldErrors {
        return {
            date: null,
            time: null,
            mealType: null,
        };
    }

    // --- Submit ---

    protected onSubmit(): void {
        this.syncSignalManagedValuesToLegacyForm(this.consumptionFormModel());
        this.consumptionSignalForm().markAsTouched();
        this.updateGeneralFieldErrors();
        this.markControlTreeTouched(this.consumptionForm);
        this.bumpItemsRenderVersion();

        if (this.macrosError() !== null) {
            return;
        }

        if (this.consumptionForm.invalid) {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
            return;
        }

        const consumptionData = this.buildConsumptionManageDto();
        const consumption = this.consumption();
        void (consumption !== null ? this.updateConsumptionAsync(consumptionData) : this.addConsumptionAsync(consumptionData)).catch(
            (error: unknown) => {
                this.handleSubmitError(error instanceof HttpErrorResponse ? error : new HttpErrorResponse({ error }));
            },
        );
    }

    private buildConsumptionManageDto(): ConsumptionManageDto {
        return buildMealManageDto(this.getConsumptionFormValue(), {
            aiSessions: this.aiSessions(),
            buildDateTime: () => this.buildDateTime(),
            convertRecipeGramsToServings: (recipe, amount) => this.mealManageFacade.convertRecipeGramsToServings(recipe, amount),
            manualTotals: this.mealManageFacade.getManualNutritionTotals(this.consumptionForm),
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

        const mealTypeControl = this.consumptionForm.controls.mealType;
        if (mealTypeControl.dirty || this.consumptionSignalForm.mealType().dirty()) {
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

    private markControlTreeTouched(control: MealLegacyMarkableControl): void {
        const children = this.getControlChildren(control);
        if (children.length > 0) {
            children.forEach(child => {
                this.markControlTreeTouched(child);
            });
            control.updateValueAndValidity();
            control.markAllAsTouched();
            return;
        }

        control.markAllAsTouched();
        control.updateValueAndValidity();
    }

    private getControlChildren(control: MealLegacyMarkableControl): MealLegacyMarkableControl[] {
        if (!this.hasChildControls(control)) {
            return [];
        }

        return Array.isArray(control.controls) ? control.controls : Object.values(control.controls);
    }

    private hasChildControls(control: MealLegacyMarkableControl): control is MealLegacyControlContainer {
        return 'controls' in control;
    }

    private populateForm(consumption: Consumption): void {
        const patch = buildMealManageFormPatchValue(consumption);
        this.consumptionForm.patchValue(patch);
        this.patchConsumptionFormModel(patch);
        this.aiSessions.set(consumption.aiSessions ?? []);

        const itemsArray = this.items;
        itemsArray.clear();

        if (consumption.items.length === 0) {
            itemsArray.push(this.mealManageFacade.createConsumptionItem());
            return;
        }

        consumption.items.forEach(item => {
            this.appendConsumptionItemForm(item);
        });

        this.items.updateValueAndValidity({ emitEvent: false });
        this.updateItemValidationRules();
    }

    private appendConsumptionItemForm(item: ConsumptionItem): void {
        const sourceType = item.sourceType;
        const initialAmount = getConsumptionItemInitialAmount(item, value =>
            this.mealManageFacade.convertRecipeServingsToGrams(value.recipe ?? null, value.amount),
        );
        this.items.push(
            this.mealManageFacade.createConsumptionItem(
                sourceType === ConsumptionSourceType.Product ? (item.product ?? null) : null,
                sourceType === ConsumptionSourceType.Recipe ? (item.recipe ?? null) : null,
                initialAmount,
                sourceType,
            ),
        );

        if (sourceType === ConsumptionSourceType.Recipe) {
            const currentIndex = this.items.length - 1;
            this.mealManageFacade.ensureRecipeWeightForExistingItem(this.items.at(currentIndex), item.amount, item.recipe ?? null);
        }
    }

    private updateSummary(): void {
        const nutritionState = this.mealManageFacade.buildNutritionSummaryState(
            this.consumptionForm,
            this.items,
            this.aiSessions(),
            this.calorieMismatchThreshold,
        );

        this.applySummary(nutritionState);

        if (this.consumptionForm.controls.isNutritionAutoCalculated.value) {
            this.mealManageFacade.syncManualNutritionFromTotals(this.consumptionForm, nutritionState.autoTotals);
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
        const nutritionState = this.mealManageFacade.buildNutritionSummaryState(
            this.consumptionForm,
            this.items,
            this.aiSessions(),
            this.calorieMismatchThreshold,
        );
        this.mealManageFacade.syncManualNutritionFromTotals(this.consumptionForm, nutritionState.autoTotals);
        this.patchManualNutritionFromTotals(nutritionState.autoTotals);
    }

    private updateManualNutritionValidators(isAuto: boolean): void {
        this.mealManageFacade.updateManualNutritionRules(this.consumptionForm, isAuto);
    }

    private updateItemValidationRules(): void {
        this.mealManageFacade.updateItemRules(this.consumptionForm.controls.items);
    }

    private patchManualNutritionFromTotals(totals: NutritionTotals): void {
        this.patchConsumptionFormModel({
            manualCalories: totals.calories,
            manualProteins: totals.proteins,
            manualFats: totals.fats,
            manualCarbs: totals.carbs,
            manualFiber: totals.fiber,
            manualAlcohol: totals.alcohol,
        });
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
                    preMealSatietyLevel: DEFAULT_SATIETY_LEVEL,
                    postMealSatietyLevel: DEFAULT_SATIETY_LEVEL,
                };
                this.consumptionForm.reset(resetValue);
                this.consumptionFormModel.set({
                    ...createMealManageFormValue(),
                    ...resetValue,
                });
                this.items.clear();
                this.items.push(this.mealManageFacade.createConsumptionItem());
                this.aiSessions.set([]);
                this.updateSummary();
            }
            await this.mealManageFacade.showSuccessRedirectAsync(Boolean(this.consumption()));
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
        return this.items.controls.findIndex(group => group.controls.product.value === null && group.controls.recipe.value === null);
    }

    private bumpItemsRenderVersion(): void {
        this.itemsRenderVersion.update(version => version + 1);
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
        return {
            ...this.consumptionForm.getRawValue(),
            ...this.pickSignalManagedFormValue(this.consumptionFormModel()),
        };
    }

    private patchConsumptionFormModel(patch: Partial<ConsumptionFormValues>): void {
        const pickedPatch = this.pickSignalManagedFormValue(patch);
        this.consumptionFormModel.update(value => {
            const nextValue = {
                ...value,
                ...pickedPatch,
            };

            return this.hasSignalManagedValueChanges(value, nextValue) ? nextValue : value;
        });
    }

    private syncSignalManagedValuesToLegacyForm(value: ConsumptionFormValues): void {
        this.consumptionForm.patchValue(
            {
                date: value.date,
                time: value.time,
                mealType: value.mealType,
                comment: value.comment,
                imageUrl: value.imageUrl,
                isNutritionAutoCalculated: value.isNutritionAutoCalculated,
                manualCalories: value.manualCalories,
                manualProteins: value.manualProteins,
                manualFats: value.manualFats,
                manualCarbs: value.manualCarbs,
                manualFiber: value.manualFiber,
                manualAlcohol: value.manualAlcohol,
            },
            { emitEvent: false },
        );
    }

    private pickSignalManagedFormValue(value: Partial<ConsumptionFormValues>): Partial<ConsumptionFormValues> {
        return Object.fromEntries(
            SIGNAL_MANAGED_FORM_FIELDS.filter(field => value[field] !== undefined).map(field => [field, value[field]]),
        );
    }

    private hasSignalManagedValueChanges(currentValue: ConsumptionFormValues, nextValue: ConsumptionFormValues): boolean {
        return SIGNAL_MANAGED_FORM_FIELDS.some(field => currentValue[field] !== nextValue[field]);
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

    private getSignalFieldError(field: FieldTree<unknown>): string | null {
        const state = field();
        if (!state.invalid() || (!state.touched() && !state.dirty())) {
            return null;
        }

        return this.translateSignalValidationError(state.errors()[0]);
    }

    private translateSignalValidationError(error: ValidationError | undefined): string | null {
        if (error === undefined) {
            return null;
        }

        if (error.kind === 'required') {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        return this.translateService.instant('FORM_ERRORS.UNKNOWN');
    }
}

type MealLegacyForm = ReturnType<typeof createMealManageForm>;
type MealLegacyItemsControl = MealLegacyForm['controls']['items'];
type MealLegacyMarkableControl = {
    markAllAsTouched: () => void;
    updateValueAndValidity: () => void;
};
type MealLegacyControlContainer = MealLegacyMarkableControl & {
    controls: MealLegacyMarkableControl[] | Record<string, MealLegacyMarkableControl>;
};

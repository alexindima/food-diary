import { HttpErrorResponse } from '@angular/common/http';
import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    FactoryProvider,
    inject,
    input,
    signal,
    untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_VALIDATION_ERRORS, FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { DEFAULT_SATIETY_LEVELS } from 'fd-ui-kit/satiety-scale/fd-ui-satiety-scale.component';
import { FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';
import { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { FdUiTimeInputComponent } from 'fd-ui-kit/time-input/fd-ui-time-input.component';

import { ImageUploadFieldComponent } from '../../../../components/shared/image-upload-field/image-upload-field.component';
import { ManageHeaderComponent } from '../../../../components/shared/manage-header/manage-header.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { NavigationService } from '../../../../services/navigation.service';
import { MEAL_TYPE_OPTIONS, normalizeMealType, resolveMealTypeByTime } from '../../../../shared/lib/meal-type.util';
import { checkCaloriesError, checkMacrosError } from '../../../../shared/lib/nutrition-form.utils';
import { UserAiUsageResponse } from '../../../../shared/models/ai.data';
import { NutrientData } from '../../../../shared/models/charts.data';
import { ImageSelection } from '../../../../shared/models/image-upload.data';
import {
    MealSatietyLevelDialogComponent,
    SatietyLevelDialogData,
} from '../../dialogs/satiety-level-dialog/meal-satiety-level-dialog.component';
import { MealManageFacade } from '../../lib/meal-manage.facade';
import { QuickMealItem } from '../../lib/quick-meal.service';
import {
    Consumption,
    ConsumptionAiSessionManageDto,
    ConsumptionItemManageDto,
    ConsumptionManageDto,
    ConsumptionSourceType,
} from '../../models/meal.data';
import {
    CalorieMismatchWarning,
    ConsumptionFormData,
    ConsumptionItemFormData,
    MacroBarState,
    MacroKey,
    MealNutritionSummaryState,
    NutritionMode,
} from './base-meal-manage.types';
import { MealAiSessionsComponent } from './meal-ai-sessions/meal-ai-sessions.component';
import { MealItemsListComponent } from './meal-items-list/meal-items-list.component';
import { MealNutritionSidebarComponent } from './meal-nutrition-sidebar/meal-nutrition-sidebar.component';

export type { ConsumptionFormData, ConsumptionItemFormData } from './base-meal-manage.types';

export const VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: FD_VALIDATION_ERRORS,
    useFactory: (): FdValidationErrors => ({
        required: () => 'FORM_ERRORS.REQUIRED',
        nonEmptyArray: () => 'FORM_ERRORS.NON_EMPTY_ARRAY',
        min: (error?: unknown) => ({
            key: 'FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO',
            params: { min: (error as { min?: number } | undefined)?.min },
        }),
    }),
};

@Component({
    selector: 'fd-base-meal-manage',
    templateUrl: './base-meal-manage.component.html',
    styleUrls: ['./base-meal-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [VALIDATION_ERRORS_PROVIDER],
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiCardComponent,
        FdUiIconComponent,
        FdUiDateInputComponent,
        FdUiTimeInputComponent,
        FdUiSelectComponent,
        FdUiTextareaComponent,
        ManageHeaderComponent,
        FdPageContainerDirective,
        ImageUploadFieldComponent,
        MealItemsListComponent,
        MealAiSessionsComponent,
        MealNutritionSidebarComponent,
    ],
})
export class BaseMealManageComponent {
    private readonly translateService = inject(TranslateService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly mealManageFacade = inject(MealManageFacade);
    private readonly calorieMismatchThreshold = 0.2;

    public readonly nutritionControlNames = {
        calories: 'manualCalories',
        proteins: 'manualProteins',
        fats: 'manualFats',
        carbs: 'manualCarbs',
        fiber: 'manualFiber',
        alcohol: 'manualAlcohol',
    };

    public readonly consumption = input<Consumption | null>();
    public readonly totalCalories = signal<number>(0);
    public readonly totalFiber = signal<number>(0);
    public readonly totalAlcohol = signal<number>(0);
    public readonly nutrientChartData = signal<NutrientData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });
    public readonly globalError = signal<string | null>(null);
    public readonly aiSessions = signal<ConsumptionAiSessionManageDto[]>([]);
    public readonly aiUsage = signal<UserAiUsageResponse | null>(null);
    public nutritionMode: NutritionMode = 'auto';
    public nutritionModeOptions: FdUiSegmentedToggleOption[] = [];
    public readonly nutritionWarning = signal<CalorieMismatchWarning | null>(null);
    private populatedConsumptionId: string | null = null;

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

    public readonly aiQuotaExceeded = computed(() => {
        const usage = this.aiUsage();
        if (!usage) {
            return false;
        }
        return usage.inputUsed >= usage.inputLimit || usage.outputUsed >= usage.outputLimit;
    });

    public consumptionForm: FormGroup<ConsumptionFormData>;
    public readonly mealTypeOptions = MEAL_TYPE_OPTIONS;
    public mealTypeSelectOptions: FdUiSelectOption<string>[] = [];

    public constructor() {
        this.consumptionForm = new FormGroup<ConsumptionFormData>({
            date: new FormControl<string>(this.getDateInputValue(new Date()), {
                nonNullable: true,
                validators: Validators.required,
            }),
            time: new FormControl<string>(this.getTimeInputValue(new Date()), {
                nonNullable: true,
                validators: Validators.required,
            }),
            mealType: new FormControl<string | null>(null),
            items: new FormArray<FormGroup<ConsumptionItemFormData>>(
                [this.mealManageFacade.createConsumptionItem()],
                this.mealManageFacade.createItemsValidator(() => this.aiSessions()),
            ),
            comment: new FormControl<string | null>(null),
            imageUrl: new FormControl<ImageSelection | null>(null),
            isNutritionAutoCalculated: new FormControl<boolean>(true, { nonNullable: true }),
            manualCalories: new FormControl<number | null>(null),
            manualProteins: new FormControl<number | null>(null),
            manualFats: new FormControl<number | null>(null),
            manualCarbs: new FormControl<number | null>(null),
            manualFiber: new FormControl<number | null>(null),
            manualAlcohol: new FormControl<number | null>(null, [Validators.min(0)]),
            preMealSatietyLevel: new FormControl<number | null>(null),
            postMealSatietyLevel: new FormControl<number | null>(null),
        });

        this.buildMealTypeOptions();
        this.buildNutritionModeOptions();
        this.nutritionMode = this.consumptionForm.controls.isNutritionAutoCalculated.value ? 'auto' : 'manual';
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildMealTypeOptions();
            this.buildNutritionModeOptions();
        });

        this.updateManualNutritionValidators(true);
        this.updateItemValidationRules();
        this.consumptionForm.controls.isNutritionAutoCalculated.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(isAuto => {
            this.nutritionMode = isAuto ? 'auto' : 'manual';
            this.updateManualNutritionValidators(isAuto);
            if (!isAuto) {
                this.populateManualNutritionFromCurrentSummary();
            }
            this.updateSummary();
        });

        this.loadAiUsage();
        const presetMealType = this.resolvePresetMealType();
        if (presetMealType) {
            this.consumptionForm.controls.mealType.setValue(presetMealType);
        } else if (!this.consumption()) {
            this.setAutoMealTypeFromDate();
        }

        this.prefillFromNavigationState();

        effect(() => {
            const consumption = this.consumption();
            untracked(() => {
                if (!consumption) {
                    this.populatedConsumptionId = null;
                    return;
                }

                if (this.populatedConsumptionId === consumption.id) {
                    return;
                }

                this.populatedConsumptionId = consumption.id;
                this.populateForm(consumption);
                this.updateItemValidationRules();
                this.updateSummary();
            });
        });

        if (!presetMealType && !this.consumption()) {
            this.consumptionForm.controls.date.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
                this.setAutoMealTypeFromDate();
            });
            this.consumptionForm.controls.time.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
                this.setAutoMealTypeFromDate();
            });
        }

        this.consumptionForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.updateItemValidationRules();
            this.updateSummary();
            this.clearGlobalError();
        });
    }

    public async onCancel(): Promise<void> {
        await this.navigationService.navigateToConsumptionList();
    }

    public get items(): FormArray<FormGroup<ConsumptionItemFormData>> {
        return this.consumptionForm.controls.items;
    }

    // --- Item management (delegated from MealItemsListComponent events) ---

    public addConsumptionItem(): void {
        this.items.push(this.mealManageFacade.createConsumptionItem());
        const newIndex = this.items.length - 1;
        queueMicrotask(() => this.onItemSourceClick(newIndex));
    }

    public removeItem(index: number): void {
        this.items.removeAt(index);
    }

    public onItemSourceClick(index: number): void {
        const group = this.items.at(index);
        const initialType = group.controls.sourceType.value ?? ConsumptionSourceType.Product;
        void this.mealManageFacade
            .openItemSelectionDialog(group, initialType === ConsumptionSourceType.Recipe ? 'Recipe' : 'Product')
            .then(() => {
                this.updateItemValidationRules();
                this.updateSummary();
            });
    }

    // --- AI session management (delegated from MealAiSessionsComponent events) ---

    public onAddConsumptionFromPhoto(): void {
        if (!this.ensurePremiumAccess()) {
            return;
        }

        if (this.aiQuotaExceeded()) {
            return;
        }

        void this.mealManageFacade.openAiPhotoSessionDialog().then(session => {
            if (!session) {
                return;
            }
            this.aiSessions.update(current => this.mealManageFacade.addAiSession(current, session));
            this.items.updateValueAndValidity({ emitEvent: false });
            this.updateItemValidationRules();
            this.updateSummary();
        });
    }

    public onDeleteAiSession(index: number): void {
        this.aiSessions.update(current => this.mealManageFacade.removeAiSession(current, index));
        this.items.updateValueAndValidity({ emitEvent: false });
        this.updateItemValidationRules();
        this.updateSummary();
    }

    public onEditAiSession(index: number): void {
        if (!this.ensurePremiumAccess()) {
            return;
        }

        const session = this.aiSessions()[index];
        if (!session) {
            return;
        }

        void this.mealManageFacade.openEditAiPhotoSessionDialog(session).then(updated => {
            if (!updated) {
                return;
            }
            this.aiSessions.update(current => this.mealManageFacade.replaceAiSession(current, index, updated));
            this.items.updateValueAndValidity({ emitEvent: false });
            this.updateItemValidationRules();
            this.updateSummary();
        });
    }

    // --- Nutrition mode (delegated from MealNutritionSidebarComponent events) ---

    public onNutritionModeChange(nextMode: string): void {
        const resolvedMode: NutritionMode = nextMode === 'manual' ? 'manual' : 'auto';
        if (this.nutritionMode === resolvedMode) {
            return;
        }

        this.nutritionMode = resolvedMode;
        this.consumptionForm.controls.isNutritionAutoCalculated.setValue(resolvedMode === 'auto');
    }

    public caloriesError(): string | null {
        if (this.consumptionForm.controls.isNutritionAutoCalculated.value) {
            return null;
        }

        return checkCaloriesError(this.consumptionForm.controls.manualCalories)
            ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.CALORIES_REQUIRED')
            : null;
    }

    public macrosError(): string | null {
        if (this.consumptionForm.controls.isNutritionAutoCalculated.value) {
            return null;
        }

        return checkMacrosError([
            this.consumptionForm.controls.manualProteins,
            this.consumptionForm.controls.manualFats,
            this.consumptionForm.controls.manualCarbs,
            this.consumptionForm.controls.manualAlcohol,
        ])
            ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED')
            : null;
    }

    // --- Satiety ---

    public getSatietyLevelMeta(value: number | null): { label: string; description: string; gradient: string } {
        if (!value) {
            return {
                label: this.translateService.instant('CONSUMPTION_MANAGE.SATIETY_PLACEHOLDER_TITLE'),
                description: this.translateService.instant('CONSUMPTION_MANAGE.SATIETY_PLACEHOLDER_DESCRIPTION'),
                gradient:
                    'linear-gradient(135deg, var(--fd-color-slate-200), color-mix(in srgb, var(--fd-color-primary-200) 55%, var(--fd-color-white)))',
            };
        }

        const config = DEFAULT_SATIETY_LEVELS.find(level => level.value === value);
        return {
            label: `${value} - ${this.translateService.instant(config?.titleKey ?? '')}`,
            description: this.translateService.instant(config?.descriptionKey ?? ''),
            gradient:
                config?.gradient ??
                'linear-gradient(135deg, var(--fd-color-slate-200), color-mix(in srgb, var(--fd-color-primary-200) 55%, var(--fd-color-white)))',
        };
    }

    public getSatietyButtonAriaLabel(controlName: 'preMealSatietyLevel' | 'postMealSatietyLevel'): string {
        const labelKey =
            controlName === 'preMealSatietyLevel' ? 'CONSUMPTION_MANAGE.HUNGER_BEFORE_LABEL' : 'CONSUMPTION_MANAGE.HUNGER_AFTER_LABEL';
        const meta = this.getSatietyLevelMeta(this.consumptionForm.controls[controlName].value);
        const sectionLabel = this.translateService.instant(labelKey);

        return `${sectionLabel}. ${meta.label}. ${meta.description}`;
    }

    public openSatietyDialog(controlName: 'preMealSatietyLevel' | 'postMealSatietyLevel'): void {
        const control = this.consumptionForm.controls[controlName];
        if (!control) {
            return;
        }

        const titleKey =
            controlName === 'preMealSatietyLevel'
                ? 'CONSUMPTION_MANAGE.HUNGER_BEFORE_DIALOG_TITLE'
                : 'CONSUMPTION_MANAGE.HUNGER_AFTER_DIALOG_TITLE';

        const dialogRef = this.fdDialogService.open<MealSatietyLevelDialogComponent, SatietyLevelDialogData, number>(
            MealSatietyLevelDialogComponent,
            {
                size: 'lg',
                data: {
                    titleKey,
                    subtitleKey: 'CONSUMPTION_MANAGE.SATIETY_DIALOG_HINT',
                    value: control.value ?? null,
                },
            },
        );

        dialogRef
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(value => {
                if (typeof value === 'number') {
                    control.setValue(value);
                    control.markAsDirty();
                    control.markAsTouched();
                }
            });
    }

    // --- Form control helpers ---

    public getControlError(controlName: keyof ConsumptionFormData): string | null {
        return this.resolveControlError(this.consumptionForm.controls[controlName]);
    }

    // --- Submit ---

    public onSubmit(): void {
        this.markFormGroupTouched(this.consumptionForm);

        if (this.macrosError()) {
            return;
        }

        if (this.consumptionForm.invalid) {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
            return;
        }

        const mealType = this.consumptionForm.controls.mealType.value;
        const comment = this.consumptionForm.controls.comment.value;
        const formItems = this.consumptionForm.controls.items.value;
        const consumptionDate = this.buildDateTime();

        const mappedItems: ConsumptionItemManageDto[] = [];

        formItems.forEach(item => {
            const amountValue = Number(item.amount) || 0;
            const sourceType = item.sourceType ?? (item.recipe ? ConsumptionSourceType.Recipe : ConsumptionSourceType.Product);

            if (sourceType === ConsumptionSourceType.Product && item.product) {
                mappedItems.push({
                    productId: item.product.id,
                    recipeId: null,
                    amount: amountValue,
                });
                return;
            }

            if (sourceType === ConsumptionSourceType.Recipe && item.recipe) {
                const servingsAmount = this.mealManageFacade.convertRecipeGramsToServings(item.recipe, amountValue);
                mappedItems.push({
                    recipeId: item.recipe.id,
                    productId: null,
                    amount: servingsAmount,
                });
            }
        });

        const isNutritionAutoCalculated = this.consumptionForm.controls.isNutritionAutoCalculated.value;
        const manualTotals = this.mealManageFacade.getManualNutritionTotals(this.consumptionForm);
        const preMealSatietyLevel = this.consumptionForm.controls.preMealSatietyLevel.value;
        const postMealSatietyLevel = this.consumptionForm.controls.postMealSatietyLevel.value;
        const image = this.consumptionForm.controls.imageUrl.value;

        const consumptionData: ConsumptionManageDto = {
            date: consumptionDate,
            mealType: mealType ?? undefined,
            comment: comment ?? undefined,
            imageUrl: image?.url ?? undefined,
            imageAssetId: image?.assetId ?? undefined,
            items: mappedItems,
            aiSessions: this.aiSessions(),
            isNutritionAutoCalculated,
            manualCalories: isNutritionAutoCalculated ? undefined : manualTotals.calories,
            manualProteins: isNutritionAutoCalculated ? undefined : manualTotals.proteins,
            manualFats: isNutritionAutoCalculated ? undefined : manualTotals.fats,
            manualCarbs: isNutritionAutoCalculated ? undefined : manualTotals.carbs,
            manualFiber: isNutritionAutoCalculated ? undefined : manualTotals.fiber,
            manualAlcohol: isNutritionAutoCalculated ? undefined : manualTotals.alcohol,
            preMealSatietyLevel: preMealSatietyLevel ?? undefined,
            postMealSatietyLevel: postMealSatietyLevel ?? undefined,
        };

        const consumption = this.consumption();
        void (consumption ? this.updateConsumption(consumptionData) : this.addConsumption(consumptionData)).catch(error =>
            this.handleSubmitError(error as HttpErrorResponse),
        );
    }

    // --- Private methods ---

    private resolvePresetMealType(): string | null {
        const stateMealType = (this.router.getCurrentNavigation()?.extras.state as { mealType?: string } | undefined)?.mealType;
        const queryMealType = this.route.snapshot.queryParamMap.get('mealType');
        return normalizeMealType(stateMealType ?? queryMealType);
    }

    private setAutoMealTypeFromDate(): void {
        if (this.consumption()) {
            return;
        }

        const mealTypeControl = this.consumptionForm.controls.mealType;
        if (mealTypeControl.dirty) {
            return;
        }

        const date = this.buildDateTime();
        if (Number.isNaN(date.getTime())) {
            return;
        }

        mealTypeControl.setValue(resolveMealTypeByTime(date), { emitEvent: false });
    }

    private prefillFromNavigationState(): void {
        if (this.consumption()) {
            return;
        }

        const navigationState = (
            this.router.getCurrentNavigation()?.extras.state as { quickConsumptionItems?: QuickMealItem[] } | undefined
        )?.quickConsumptionItems;
        const historyState = (window.history.state as { quickConsumptionItems?: QuickMealItem[] } | undefined)?.quickConsumptionItems;
        const draftItems = navigationState ?? historyState;

        if (!draftItems?.length) {
            return;
        }

        this.items.clear();
        draftItems.forEach(item => {
            const sourceType = item.type === 'recipe' ? ConsumptionSourceType.Recipe : ConsumptionSourceType.Product;
            const amount = item.amount ?? 0;
            this.items.push(
                this.mealManageFacade.createConsumptionItem(
                    sourceType === ConsumptionSourceType.Product ? (item.product ?? null) : null,
                    sourceType === ConsumptionSourceType.Recipe ? (item.recipe ?? null) : null,
                    amount,
                    sourceType,
                ),
            );

            if (sourceType === ConsumptionSourceType.Recipe) {
                const currentIndex = this.items.length - 1;
                this.mealManageFacade.ensureRecipeWeightForExistingItem(this.items.at(currentIndex), amount, item.recipe ?? null);
            }
        });

        if (!this.items.length) {
            this.items.push(this.mealManageFacade.createConsumptionItem());
        }

        this.updateItemValidationRules();
        this.updateSummary();
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

        formGroup.markAllAsTouched();
    }

    private populateForm(consumption: Consumption): void {
        this.consumptionForm.patchValue({
            date: this.getDateInputValue(new Date(consumption.date)),
            time: this.getTimeInputValue(new Date(consumption.date)),
            mealType: normalizeMealType(consumption.mealType),
            comment: consumption.comment || null,
            imageUrl: {
                url: consumption.imageUrl ?? null,
                assetId: consumption.imageAssetId ?? null,
            },
            isNutritionAutoCalculated: consumption.isNutritionAutoCalculated,
            manualCalories: consumption.manualCalories ?? consumption.totalCalories,
            manualProteins: consumption.manualProteins ?? consumption.totalProteins,
            manualFats: consumption.manualFats ?? consumption.totalFats,
            manualCarbs: consumption.manualCarbs ?? consumption.totalCarbs,
            manualFiber: consumption.manualFiber ?? consumption.totalFiber,
            manualAlcohol: consumption.manualAlcohol ?? consumption.totalAlcohol,
            preMealSatietyLevel: consumption.preMealSatietyLevel ?? null,
            postMealSatietyLevel: consumption.postMealSatietyLevel ?? null,
        });
        this.aiSessions.set(consumption.aiSessions ?? []);

        const itemsArray = this.items;
        itemsArray.clear();

        if (consumption.items.length === 0) {
            itemsArray.push(this.mealManageFacade.createConsumptionItem());
            return;
        }

        consumption.items.forEach(item => {
            const sourceType = item.sourceType ?? (item.recipe ? ConsumptionSourceType.Recipe : ConsumptionSourceType.Product);
            const initialAmount =
                sourceType === ConsumptionSourceType.Recipe
                    ? this.mealManageFacade.convertRecipeServingsToGrams(item.recipe ?? null, item.amount ?? 0)
                    : item.amount;

            itemsArray.push(
                this.mealManageFacade.createConsumptionItem(
                    sourceType === ConsumptionSourceType.Product ? (item.product ?? null) : null,
                    sourceType === ConsumptionSourceType.Recipe ? (item.recipe ?? null) : null,
                    initialAmount,
                    sourceType,
                ),
            );

            if (sourceType === ConsumptionSourceType.Recipe) {
                const currentIndex = itemsArray.length - 1;
                this.mealManageFacade.ensureRecipeWeightForExistingItem(itemsArray.at(currentIndex), item.amount ?? 0, item.recipe ?? null);
            }
        });

        this.items.updateValueAndValidity({ emitEvent: false });
        this.updateItemValidationRules();
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
    }

    private updateManualNutritionValidators(isAuto: boolean): void {
        this.mealManageFacade.updateManualNutritionValidators(this.consumptionForm, isAuto);
    }

    private updateItemValidationRules(): void {
        this.mealManageFacade.updateItemValidationRules(this.consumptionForm.controls.items);
    }

    private buildNutritionModeOptions(): void {
        this.nutritionModeOptions = [
            {
                value: 'auto',
                label: this.translateService.instant('CONSUMPTION_MANAGE.NUTRITION_MODE.AUTO'),
            },
            {
                value: 'manual',
                label: this.translateService.instant('CONSUMPTION_MANAGE.NUTRITION_MODE.MANUAL'),
            },
        ];
    }

    private async addConsumption(consumptionData: ConsumptionManageDto): Promise<void> {
        const response = await this.mealManageFacade.submitConsumption(null, consumptionData);
        await this.handleSubmitResponse(response);
    }

    private async updateConsumption(consumptionData: ConsumptionManageDto): Promise<void> {
        const response = await this.mealManageFacade.submitConsumption(this.consumption() ?? null, consumptionData);
        await this.handleSubmitResponse(response);
    }

    private async handleSubmitResponse(response: Consumption | null): Promise<void> {
        if (response) {
            if (!this.consumption()) {
                this.consumptionForm.reset({
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
                });
                this.items.clear();
                this.items.push(this.mealManageFacade.createConsumptionItem());
                this.aiSessions.set([]);
                this.updateSummary();
            }
            await this.mealManageFacade.showSuccessRedirect(Boolean(this.consumption()));
        } else {
            this.handleSubmitError();
        }
    }

    private handleSubmitError(_error?: HttpErrorResponse): void {
        this.setGlobalError('FORM_ERRORS.UNKNOWN');
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

    private buildMealTypeOptions(): void {
        this.mealTypeSelectOptions = this.mealTypeOptions.map(option => ({
            value: option,
            label: this.translateService.instant('MEAL_TYPES.' + option),
        }));
    }

    private getDateInputValue(date: Date): string {
        const year = date.getFullYear();
        const month = this.padNumber(date.getMonth() + 1);
        const day = this.padNumber(date.getDate());
        return `${year}-${month}-${day}`;
    }

    private getTimeInputValue(date: Date): string {
        const hours = this.padNumber(date.getHours());
        const minutes = this.padNumber(date.getMinutes());
        return `${hours}:${minutes}`;
    }

    private buildDateTime(): Date {
        const dateValue = this.consumptionForm.controls.date.value;
        const timeValue = this.consumptionForm.controls.time.value;
        const datePart = dateValue ?? this.getDateInputValue(new Date());
        const timePart = timeValue ?? this.getTimeInputValue(new Date());
        const combined = `${datePart}T${timePart}`;
        const parsed = new Date(combined);
        return Number.isNaN(parsed.getTime()) ? new Date() : parsed;
    }

    private padNumber(value: number): string {
        return value.toString().padStart(2, '0');
    }

    private resolveControlError(control: AbstractControl | null): string | null {
        if (!control || !control.invalid || !control.touched) {
            return null;
        }

        if (control.errors?.['required']) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        if (control.errors?.['min']) {
            const min = control.errors['min'].min ?? 0;
            return this.translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', { min });
        }

        return this.translateService.instant('FORM_ERRORS.UNKNOWN');
    }

    private loadAiUsage(): void {
        void this.mealManageFacade.loadAiUsage().then(usage => this.aiUsage.set(usage));
    }
}

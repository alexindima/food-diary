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
import { type AbstractControl, FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import {
    FdUiEmojiPickerComponent,
    type FdUiEmojiPickerOption,
    type FdUiEmojiPickerValue,
} from 'fd-ui-kit/emoji-picker/fd-ui-emoji-picker.component';
import { FD_VALIDATION_ERRORS, type FdValidationErrors, getNumberProperty } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { DEFAULT_HUNGER_LEVELS, DEFAULT_SATIETY_LEVELS } from 'fd-ui-kit/satiety-scale/fd-ui-satiety-scale.component';
import type { FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { FdUiTimeInputComponent } from 'fd-ui-kit/time-input/fd-ui-time-input.component';
import { EMPTY, firstValueFrom, merge, type Observable } from 'rxjs';

import { AiInputBarComponent } from '../../../../components/shared/ai-input-bar/ai-input-bar.component';
import type { AiInputBarResult } from '../../../../components/shared/ai-input-bar/ai-input-bar.types';
import { ImageUploadFieldComponent } from '../../../../components/shared/image-upload-field/image-upload-field.component';
import { ManageHeaderComponent } from '../../../../components/shared/manage-header/manage-header.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { NavigationService } from '../../../../services/navigation.service';
import { MEAL_TYPE_OPTIONS, normalizeMealType, resolveMealTypeByTime } from '../../../../shared/lib/meal-type.util';
import { DEFAULT_CALORIE_MISMATCH_THRESHOLD, PERCENT_MULTIPLIER } from '../../../../shared/lib/nutrition.constants';
import { checkCaloriesError, checkMacrosError } from '../../../../shared/lib/nutrition-form.utils';
import { DEFAULT_SATIETY_LEVEL, normalizeSatietyLevel } from '../../../../shared/lib/satiety-level.utils';
import { getStringProperty } from '../../../../shared/lib/unknown-value.utils';
import type { UserAiUsageResponse } from '../../../../shared/models/ai.data';
import type { NutrientData } from '../../../../shared/models/charts.data';
import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import { MealManageFacade } from '../../lib/meal-manage.facade';
import {
    type Consumption,
    type ConsumptionAiSessionManageDto,
    type ConsumptionItem,
    type ConsumptionItemManageDto,
    type ConsumptionManageDto,
    ConsumptionSourceType,
} from '../../models/meal.data';
import type {
    CalorieMismatchWarning,
    ConsumptionFormData,
    ConsumptionFormValues,
    ConsumptionItemFormData,
    ConsumptionItemFormValues,
    MacroBarState,
    MacroKey,
    MealNutritionSummaryState,
    NutritionMode,
    NutritionTotals,
} from './base-meal-manage.types';
import { MealAiSessionsComponent } from './meal-ai-sessions/meal-ai-sessions.component';
import { MealItemsListComponent } from './meal-items-list/meal-items-list.component';
import { MealManualItemDialogComponent, type MealManualItemDialogData } from './meal-manual-item-dialog/meal-manual-item-dialog.component';
import { MealNutritionSidebarComponent } from './meal-nutrition-sidebar/meal-nutrition-sidebar.component';

export type { ConsumptionFormData, ConsumptionItemFormData } from './base-meal-manage.types';

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
const TIME_PAD_LENGTH = 2;

type GeneralErrorField = (typeof GENERAL_ERROR_FIELDS)[number];
type GeneralFieldErrors = Record<GeneralErrorField, string | null>;

@Component({
    selector: 'fd-base-meal-manage',
    templateUrl: './base-meal-manage.component.html',
    styleUrls: ['./base-meal-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [VALIDATION_ERRORS_PROVIDER],
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiDateInputComponent,
        FdUiTimeInputComponent,
        FdUiSelectComponent,
        FdUiTextareaComponent,
        FdUiEmojiPickerComponent,
        ManageHeaderComponent,
        FdPageContainerDirective,
        AiInputBarComponent,
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
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly mealManageFacade = inject(MealManageFacade);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly calorieMismatchThreshold = DEFAULT_CALORIE_MISMATCH_THRESHOLD;

    public readonly nutritionControlNames = {
        calories: 'manualCalories',
        proteins: 'manualProteins',
        fats: 'manualFats',
        carbs: 'manualCarbs',
        fiber: 'manualFiber',
        alcohol: 'manualAlcohol',
    };

    public readonly consumption = input<Consumption | null>(null);
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
    public readonly itemsRenderVersion = signal(0);
    public readonly nutritionMode = signal<NutritionMode>('auto');
    public readonly preMealSatietyLevel = signal<number | null>(DEFAULT_SATIETY_LEVEL);
    public readonly postMealSatietyLevel = signal<number | null>(DEFAULT_SATIETY_LEVEL);
    public readonly selectedMealType = signal<string | null>(null);
    public nutritionModeOptions: FdUiSegmentedToggleOption[] = [];
    public hungerEmojiOptions: Array<FdUiEmojiPickerOption<number>> = [];
    public satietyEmojiOptions: Array<FdUiEmojiPickerOption<number>> = [];
    public readonly nutritionWarning = signal<CalorieMismatchWarning | null>(null);
    public readonly preMealSatietyAriaLabel = signal('');
    public readonly postMealSatietyAriaLabel = signal('');
    public readonly generalFieldErrors = signal<GeneralFieldErrors>(this.createEmptyGeneralFieldErrors());
    public readonly manageHeaderState = computed(() => ({
        titleKey: this.consumption() !== null ? 'CONSUMPTION_MANAGE.EDIT_TITLE' : 'CONSUMPTION_MANAGE.ADD_TITLE',
    }));
    private populatedConsumptionId: string | null = null;

    public readonly macroBarState = computed<MacroBarState>(() => {
        const nutrients = this.nutrientChartData();
        const entries: Array<{ key: MacroKey; value: number }> = [
            { key: 'proteins', value: nutrients.proteins },
            { key: 'fats', value: nutrients.fats },
            { key: 'carbs', value: nutrients.carbs },
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
                percent: (entry.value / total) * PERCENT_MULTIPLIER,
            })),
        };
    });

    public readonly aiQuotaExceeded = computed(() => {
        const usage = this.aiUsage();
        if (usage === null) {
            return false;
        }
        return usage.inputUsed >= usage.inputLimit || usage.outputUsed >= usage.outputLimit;
    });

    public consumptionForm: FormGroup<ConsumptionFormData>;
    public readonly mealTypeOptions = MEAL_TYPE_OPTIONS;
    public mealTypeSelectOptions: Array<FdUiSelectOption<string>> = [];

    public constructor() {
        this.consumptionForm = this.createConsumptionForm();
        this.buildMealTypeOptions();
        this.buildNutritionModeOptions();
        this.buildSatietyEmojiOptions();
        this.nutritionMode.set(this.consumptionForm.controls.isNutritionAutoCalculated.value ? 'auto' : 'manual');
        this.watchLanguageChanges();
        this.watchGeneralFieldErrors();
        this.updateGeneralFieldErrors();
        this.watchSatietyChanges();
        this.updateSatietyAriaLabels();
        this.watchMealTypeChanges();
        this.updateManualNutritionValidators(true);
        this.updateItemValidationRules();
        this.watchNutritionModeChanges();

        this.loadAiUsage();
        const presetMealType = this.resolvePresetMealType();
        if (presetMealType !== null) {
            this.consumptionForm.controls.mealType.setValue(presetMealType);
        } else if (this.consumption() === null) {
            this.setAutoMealTypeFromDate();
        }

        this.watchConsumptionInput();

        if (presetMealType === null && this.consumption() === null) {
            this.watchAutoMealTypeChanges();
        }

        this.watchFormChanges();
    }

    private createConsumptionForm(): FormGroup<ConsumptionFormData> {
        return new FormGroup<ConsumptionFormData>({
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
            preMealSatietyLevel: new FormControl<number | null>(DEFAULT_SATIETY_LEVEL),
            postMealSatietyLevel: new FormControl<number | null>(DEFAULT_SATIETY_LEVEL),
        });
    }

    private watchLanguageChanges(): void {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildMealTypeOptions();
            this.buildNutritionModeOptions();
            this.buildSatietyEmojiOptions();
            this.updateSatietyAriaLabels();
            this.updateGeneralFieldErrors();
        });
    }

    private watchGeneralFieldErrors(): void {
        const formEvents = (this.consumptionForm as { events?: Observable<unknown> }).events ?? EMPTY;
        merge(formEvents, this.consumptionForm.statusChanges, this.consumptionForm.valueChanges)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.updateGeneralFieldErrors();
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
                this.updateSatietyAriaLabels();
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
    }

    private watchAutoMealTypeChanges(): void {
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

    public async onCancelAsync(): Promise<void> {
        await this.navigationService.navigateToConsumptionListAsync();
    }

    public get items(): FormArray<FormGroup<ConsumptionItemFormData>> {
        return this.consumptionForm.controls.items;
    }

    // --- Item management (delegated from MealItemsListComponent events) ---

    public addConsumptionItem(): void {
        const reusableIndex = this.findReusableEmptyItemIndex();
        const itemIndex = reusableIndex >= 0 ? reusableIndex : this.items.length;

        if (reusableIndex < 0) {
            this.items.push(this.mealManageFacade.createConsumptionItem());
        }

        queueMicrotask(() => {
            this.openManualItemDialog(itemIndex);
        });
    }

    public removeItem(index: number): void {
        this.items.removeAt(index);
        this.bumpItemsRenderVersion();
    }

    public onItemSourceClick(index: number): void {
        this.openManualItemDialog(index);
    }

    public openManualItemDialog(index: number): void {
        const group = this.items.at(index);
        void firstValueFrom(
            this.fdDialogService
                .open<MealManualItemDialogComponent, MealManualItemDialogData, boolean>(MealManualItemDialogComponent, {
                    preset: 'form',
                    data: { group },
                })
                .afterClosed(),
        ).then(saved => {
            if (saved === true) {
                this.bumpItemsRenderVersion();
                this.updateItemValidationRules();
                this.updateSummary();
            }
        });
    }

    // --- AI session management ---

    public onAiMealRecognized(result: AiInputBarResult): void {
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

    public onNutritionModeChange(nextMode: string): void {
        const resolvedMode: NutritionMode = nextMode === 'manual' ? 'manual' : 'auto';
        if (this.nutritionMode() === resolvedMode) {
            return;
        }

        this.nutritionMode.set(resolvedMode);
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

    public getSatietyLevelMeta(
        controlName: 'preMealSatietyLevel' | 'postMealSatietyLevel',
        value: number | null,
    ): { emoji: string; label: string; description: string; gradient: string } {
        const normalizedValue = normalizeSatietyLevel(value);
        const levels = controlName === 'preMealSatietyLevel' ? DEFAULT_HUNGER_LEVELS : DEFAULT_SATIETY_LEVELS;
        const config = levels.find(level => level.value === normalizedValue);
        return {
            emoji: config?.emoji ?? '😐',
            label: this.translateService.instant(config?.titleKey ?? ''),
            description: this.translateService.instant(config?.descriptionKey ?? ''),
            gradient: config?.gradient ?? 'linear-gradient(135deg, var(--fd-color-orange-500), var(--fd-color-yellow-300))',
        };
    }

    private getSatietyButtonAriaLabel(controlName: 'preMealSatietyLevel' | 'postMealSatietyLevel'): string {
        const labelKey =
            controlName === 'preMealSatietyLevel' ? 'CONSUMPTION_MANAGE.HUNGER_BEFORE_LABEL' : 'CONSUMPTION_MANAGE.HUNGER_AFTER_LABEL';
        const meta = this.getSatietyLevelMeta(controlName, this.consumptionForm.controls[controlName].value);
        const sectionLabel = this.translateService.instant(labelKey);

        return `${sectionLabel}. ${meta.label}. ${meta.description}`;
    }

    private updateSatietyAriaLabels(): void {
        this.preMealSatietyAriaLabel.set(this.getSatietyButtonAriaLabel('preMealSatietyLevel'));
        this.postMealSatietyAriaLabel.set(this.getSatietyButtonAriaLabel('postMealSatietyLevel'));
    }

    public onSatietyLevelChange(controlName: 'preMealSatietyLevel' | 'postMealSatietyLevel', value: FdUiEmojiPickerValue | null): void {
        if (typeof value !== 'number') {
            return;
        }

        const control = this.consumptionForm.controls[controlName];
        control.setValue(normalizeSatietyLevel(value));
        control.markAsDirty();
        control.markAsTouched();
    }

    // --- Form control helpers ---

    private getControlError(controlName: keyof ConsumptionFormData): string | null {
        return this.resolveControlError(this.consumptionForm.controls[controlName]);
    }

    private updateGeneralFieldErrors(): void {
        this.generalFieldErrors.set(
            GENERAL_ERROR_FIELDS.reduce<GeneralFieldErrors>((errors, field) => {
                errors[field] = this.getControlError(field);
                return errors;
            }, this.createEmptyGeneralFieldErrors()),
        );
    }

    private createEmptyGeneralFieldErrors(): GeneralFieldErrors {
        return {
            date: null,
            time: null,
            mealType: null,
        };
    }

    // --- Submit ---

    public onSubmit(): void {
        this.markFormGroupTouched(this.consumptionForm);

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
        const isNutritionAutoCalculated = this.consumptionForm.controls.isNutritionAutoCalculated.value;
        const manualTotals = this.mealManageFacade.getManualNutritionTotals(this.consumptionForm);
        const image = this.consumptionForm.controls.imageUrl.value;

        return {
            date: this.buildDateTime(),
            mealType: this.consumptionForm.controls.mealType.value ?? undefined,
            comment: this.consumptionForm.controls.comment.value ?? undefined,
            imageUrl: image?.url ?? undefined,
            imageAssetId: image?.assetId ?? undefined,
            items: this.mapConsumptionItems(),
            aiSessions: this.aiSessions(),
            isNutritionAutoCalculated,
            ...this.buildManualNutritionPayload(isNutritionAutoCalculated, manualTotals),
            preMealSatietyLevel: normalizeSatietyLevel(this.consumptionForm.controls.preMealSatietyLevel.value),
            postMealSatietyLevel: normalizeSatietyLevel(this.consumptionForm.controls.postMealSatietyLevel.value),
        };
    }

    private mapConsumptionItems(): ConsumptionItemManageDto[] {
        return this.consumptionForm.controls.items.value.flatMap(item => this.mapConsumptionItem(item));
    }

    private mapConsumptionItem(item: Partial<ConsumptionItemFormValues>): ConsumptionItemManageDto[] {
        const amount = this.normalizeItemAmount(item.amount);
        const sourceType =
            item.sourceType ??
            (item.recipe !== null && item.recipe !== undefined ? ConsumptionSourceType.Recipe : ConsumptionSourceType.Product);

        if (sourceType === ConsumptionSourceType.Product && item.product !== null && item.product !== undefined) {
            return [{ productId: item.product.id, recipeId: null, amount }];
        }

        if (sourceType === ConsumptionSourceType.Recipe && item.recipe !== null && item.recipe !== undefined) {
            return [
                {
                    recipeId: item.recipe.id,
                    productId: null,
                    amount: this.mealManageFacade.convertRecipeGramsToServings(item.recipe, amount),
                },
            ];
        }

        return [];
    }

    private normalizeItemAmount(value: unknown): number {
        const parsedAmount = Number(value);
        return Number.isNaN(parsedAmount) || parsedAmount === 0 ? 0 : parsedAmount;
    }

    private buildManualNutritionPayload(isNutritionAutoCalculated: boolean, manualTotals: NutritionTotals): Partial<ConsumptionManageDto> {
        return {
            manualCalories: isNutritionAutoCalculated ? undefined : manualTotals.calories,
            manualProteins: isNutritionAutoCalculated ? undefined : manualTotals.proteins,
            manualFats: isNutritionAutoCalculated ? undefined : manualTotals.fats,
            manualCarbs: isNutritionAutoCalculated ? undefined : manualTotals.carbs,
            manualFiber: isNutritionAutoCalculated ? undefined : manualTotals.fiber,
            manualAlcohol: isNutritionAutoCalculated ? undefined : manualTotals.alcohol,
        };
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
        if (mealTypeControl.dirty) {
            return;
        }

        const date = this.buildDateTime();
        if (Number.isNaN(date.getTime())) {
            return;
        }

        const mealType = resolveMealTypeByTime(date);
        mealTypeControl.setValue(mealType, { emitEvent: false });
        this.selectedMealType.set(mealType);
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
        this.consumptionForm.patchValue(this.buildConsumptionFormPatchValue(consumption));
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

    private buildConsumptionFormPatchValue(consumption: Consumption): Partial<ConsumptionFormValues> {
        return {
            date: this.getDateInputValue(new Date(consumption.date)),
            time: this.getTimeInputValue(new Date(consumption.date)),
            mealType: normalizeMealType(consumption.mealType),
            comment: this.toNullable(consumption.comment),
            imageUrl: {
                url: this.toNullable(consumption.imageUrl),
                assetId: this.toNullable(consumption.imageAssetId),
            },
            isNutritionAutoCalculated: consumption.isNutritionAutoCalculated,
            ...this.buildConsumptionManualNutritionPatchValue(consumption),
            preMealSatietyLevel: normalizeSatietyLevel(this.toNullable(consumption.preMealSatietyLevel)),
            postMealSatietyLevel: normalizeSatietyLevel(this.toNullable(consumption.postMealSatietyLevel)),
        };
    }

    private buildConsumptionManualNutritionPatchValue(consumption: Consumption): Partial<ConsumptionFormValues> {
        return {
            manualCalories: consumption.manualCalories ?? consumption.totalCalories,
            manualProteins: consumption.manualProteins ?? consumption.totalProteins,
            manualFats: consumption.manualFats ?? consumption.totalFats,
            manualCarbs: consumption.manualCarbs ?? consumption.totalCarbs,
            manualFiber: consumption.manualFiber ?? consumption.totalFiber,
            manualAlcohol: consumption.manualAlcohol ?? consumption.totalAlcohol,
        };
    }

    private toNullable<T>(value: T | null | undefined): T | null {
        return value ?? null;
    }

    private appendConsumptionItemForm(item: ConsumptionItem): void {
        const sourceType = item.sourceType;
        const initialAmount = this.getConsumptionItemInitialAmount(item);
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

    private getConsumptionItemInitialAmount(item: ConsumptionItem): number {
        return item.sourceType === ConsumptionSourceType.Recipe
            ? this.mealManageFacade.convertRecipeServingsToGrams(item.recipe ?? null, item.amount)
            : item.amount;
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

    private buildSatietyEmojiOptions(): void {
        this.hungerEmojiOptions = this.buildEmojiOptions(DEFAULT_HUNGER_LEVELS);
        this.satietyEmojiOptions = this.buildEmojiOptions(DEFAULT_SATIETY_LEVELS);
    }

    private buildEmojiOptions(levels: typeof DEFAULT_SATIETY_LEVELS): Array<FdUiEmojiPickerOption<number>> {
        return levels.map(level => {
            const label = this.translateService.instant(level.titleKey);
            const description = this.translateService.instant(level.descriptionKey);
            return {
                value: level.value,
                emoji: level.emoji,
                label,
                description,
                ariaLabel: `${label}. ${description}`,
                hint: `${label}. ${description}`,
            };
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
                    preMealSatietyLevel: DEFAULT_SATIETY_LEVEL,
                    postMealSatietyLevel: DEFAULT_SATIETY_LEVEL,
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

    private findReusableEmptyItemIndex(): number {
        return this.items.controls.findIndex(group => group.controls.product.value === null && group.controls.recipe.value === null);
    }

    private bumpItemsRenderVersion(): void {
        this.itemsRenderVersion.update(version => version + 1);
    }

    private buildMealTypeOptions(): void {
        this.mealTypeSelectOptions = this.mealTypeOptions.map(option => ({
            value: option,
            label: this.translateService.instant(`MEAL_TYPES.${option}`),
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
        const datePart = dateValue;
        const timePart = timeValue;
        const combined = `${datePart}T${timePart}`;
        const parsed = new Date(combined);
        return Number.isNaN(parsed.getTime()) ? new Date() : parsed;
    }

    private padNumber(value: number): string {
        return value.toString().padStart(TIME_PAD_LENGTH, '0');
    }

    private resolveControlError(control: AbstractControl | null): string | null {
        if (control === null || !control.invalid || !control.touched) {
            return null;
        }

        if (control.errors?.['required'] === true) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        const minError: unknown = control.getError('min');
        if (minError !== null) {
            const min = getNumberProperty(minError, 'min') ?? 0;
            return this.translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', { min });
        }

        return this.translateService.instant('FORM_ERRORS.UNKNOWN');
    }

    private loadAiUsage(): void {
        void this.mealManageFacade.loadAiUsageAsync().then(usage => {
            this.aiUsage.set(usage);
        });
    }
}

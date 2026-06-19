import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { FieldTree } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FD_VALIDATION_ERRORS, type FdValidationErrors, resolveSignalFormFieldError } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';
import { EMPTY, type Observable } from 'rxjs';

import {
    NutritionEditorComponent,
    type NutritionEditorFieldErrors,
    type NutritionEditorSignalForm,
    type NutritionEditorWarning,
    type NutritionMacroState,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor';
import { DEFAULT_CALORIE_MISMATCH_THRESHOLD, DEFAULT_NUTRITION_BASE_AMOUNT } from '../../../../../shared/lib/nutrition.constants';
import {
    calculateCalorieMismatchWarning,
    calculateMacroBarState,
    checkCaloriesError,
    checkMacrosError,
} from '../../../../../shared/lib/nutrition-form.utils';
import {
    getProductMaxCaloriesPerBaseForUnit,
    getProductMaxNutrientPerBaseForUnit,
    getProductMaxNutritionDisplayForUnit,
} from '../../../lib/product-manage.constants';
import { MeasurementUnit } from '../../../models/product.data';
import type { NutritionMode, ProductFormValues } from '../product-manage-lib/product-manage-form.types';

@Component({
    selector: 'fd-product-nutrition-editor',
    templateUrl: './product-nutrition-editor.html',
    styleUrls: ['./product-nutrition-editor.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiSegmentedToggleComponent,
        NutritionEditorComponent,
    ],
})
export class ProductNutritionEditorComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });

    public readonly form = input.required<FieldTree<ProductFormValues>>();
    public readonly nutritionMode = input.required<NutritionMode>();
    protected readonly maxCalories = computed(() =>
        this.getAllowedNutritionMax(getProductMaxCaloriesPerBaseForUnit(this.form().baseUnit().value())),
    );
    protected readonly maxNutrient = computed(() =>
        this.getAllowedNutritionMax(getProductMaxNutrientPerBaseForUnit(this.form().baseUnit().value())),
    );
    protected readonly macroBarState = signal<NutritionMacroState>({ isEmpty: true, segments: [] });
    protected readonly nutritionWarning = signal<NutritionEditorWarning | null>(null);
    protected readonly nutritionModeOptions = signal<FdUiSegmentedToggleOption[]>([]);
    protected readonly nutritionForm = computed<NutritionEditorSignalForm>(() => {
        const form = this.form();
        return {
            calories: form.caloriesPerBase,
            proteins: form.proteinsPerBase,
            fats: form.fatsPerBase,
            carbs: form.carbsPerBase,
            fiber: form.fiberPerBase,
            alcohol: form.alcoholPerBase,
        };
    });
    protected readonly nutritionFieldErrors = computed<NutritionEditorFieldErrors>(() => {
        const form = this.form();
        return {
            calories: this.getFieldError(form.caloriesPerBase),
            proteins: this.getFieldError(form.proteinsPerBase),
            fats: this.getFieldError(form.fatsPerBase),
            carbs: this.getFieldError(form.carbsPerBase),
            fiber: this.getFieldError(form.fiberPerBase),
            alcohol: this.getFieldError(form.alcoholPerBase),
        };
    });

    public readonly nutritionModeChange = output<string>();
    public readonly openAiRecognition = output();

    public constructor() {
        const languageChanges = (this.translateService as { onLangChange?: Observable<unknown> }).onLangChange ?? EMPTY;
        languageChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildNutritionModeOptions();
        });

        effect(() => {
            this.form().baseUnit().value();
            this.buildNutritionModeOptions();
        });

        effect(() => {
            this.updateCalorieWarning();
            this.updateMacroDistribution();
        });
    }

    protected caloriesError(): string | null {
        const fieldError = this.nutritionFieldErrors().calories;
        if (fieldError !== null && fieldError !== undefined) {
            return null;
        }

        const control = this.getControlState('caloriesPerBase');
        return checkCaloriesError(control) ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.CALORIES_REQUIRED') : null;
    }

    protected macrosError(): string | null {
        const controls = [
            this.getControlState('proteinsPerBase'),
            this.getControlState('fatsPerBase'),
            this.getControlState('carbsPerBase'),
            this.getControlState('alcoholPerBase'),
        ];

        return checkMacrosError(controls) ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED') : null;
    }

    private buildNutritionModeOptions(): void {
        const baseUnit = this.form().baseUnit().value();
        const amount = this.getDefaultBaseAmount(baseUnit);
        const unitLabel = this.getUnitLabel(baseUnit);

        this.nutritionModeOptions.set([
            {
                value: 'base',
                label: this.translateService.instant('PRODUCT_MANAGE.NUTRITION_MODE.BASE', {
                    amount,
                    unit: unitLabel,
                }),
            },
            {
                value: 'portion',
                label: this.translateService.instant('PRODUCT_MANAGE.NUTRITION_MODE.PORTION'),
            },
        ]);
    }

    private getUnitLabel(baseUnit: MeasurementUnit | null): string {
        if (baseUnit === null) {
            return '';
        }
        return this.translateService.instant(`GENERAL.UNITS.${baseUnit}`);
    }

    private getDefaultBaseAmount(unit: MeasurementUnit): number {
        return unit === MeasurementUnit.PCS ? 1 : DEFAULT_NUTRITION_BASE_AMOUNT;
    }

    private getAllowedNutritionMax(maxPerBase: number): number {
        if (this.nutritionMode() !== 'portion') {
            return maxPerBase;
        }

        return getProductMaxNutritionDisplayForUnit(maxPerBase, this.form().baseUnit().value());
    }

    private getCurrentPortionNutritionMax(maxPerBase: number): number {
        const form = this.form();
        const baseUnit = form.baseUnit().value();
        const baseAmount = this.getDefaultBaseAmount(baseUnit);
        const portionAmount = this.getNumberValue(form.defaultPortionAmount().value());
        return portionAmount > 0 ? maxPerBase * (portionAmount / baseAmount) : maxPerBase;
    }

    private updateCalorieWarning(): void {
        const form = this.form();
        const calories = this.getNumberValue(form.caloriesPerBase().value());
        const proteins = this.getNumberValue(form.proteinsPerBase().value());
        const fats = this.getNumberValue(form.fatsPerBase().value());
        const carbs = this.getNumberValue(form.carbsPerBase().value());
        const fiber = this.getNumberValue(form.fiberPerBase().value());
        const alcohol = this.getNumberValue(form.alcoholPerBase().value());
        if (this.hasCurrentPortionNutritionWarning({ calories, proteins, fats, carbs, fiber, alcohol })) {
            this.nutritionWarning.set({ kind: 'text', messageKey: 'PRODUCT_MANAGE.NUTRITION_WARNINGS.PORTION_LIMIT' });
            return;
        }

        const warning = calculateCalorieMismatchWarning({
            calories,
            proteins,
            fats,
            carbs,
            alcohol,
            threshold: DEFAULT_CALORIE_MISMATCH_THRESHOLD,
        });
        this.nutritionWarning.set(warning === null ? null : { kind: 'caloriesMismatch', ...warning });
    }

    private hasCurrentPortionNutritionWarning(values: ProductNutritionValues): boolean {
        if (this.nutritionMode() !== 'portion') {
            return false;
        }

        const baseUnit = this.form().baseUnit().value();
        const maxCalories = this.getCurrentPortionNutritionMax(getProductMaxCaloriesPerBaseForUnit(baseUnit));
        const maxNutrient = this.getCurrentPortionNutritionMax(getProductMaxNutrientPerBaseForUnit(baseUnit));
        return (
            values.calories > maxCalories ||
            values.proteins > maxNutrient ||
            values.fats > maxNutrient ||
            values.carbs > maxNutrient ||
            values.fiber > maxNutrient ||
            values.alcohol > maxNutrient
        );
    }

    private updateMacroDistribution(): void {
        const form = this.form();
        const proteins = this.getNumberValue(form.proteinsPerBase().value());
        const fats = this.getNumberValue(form.fatsPerBase().value());
        const carbs = this.getNumberValue(form.carbsPerBase().value());
        this.macroBarState.set(calculateMacroBarState(proteins, fats, carbs));
    }

    private getNumberValue(value: number | string | null): number {
        if (value === null || value === '') {
            return 0;
        }

        const normalized = typeof value === 'string' ? Number(value.replace(',', '.').replaceAll(/[^\d.-]/g, '')) : Number(value);
        return Number.isFinite(normalized) ? Math.max(0, normalized) : 0;
    }

    private getControlState(
        field: keyof Pick<ProductFormValues, 'caloriesPerBase' | 'proteinsPerBase' | 'fatsPerBase' | 'carbsPerBase' | 'alcoholPerBase'>,
    ): {
        value: number | null;
        touched: boolean;
        dirty: boolean;
    } {
        const state = this.form()[field]();
        return {
            value: state.value(),
            touched: state.touched(),
            dirty: state.dirty(),
        };
    }

    private getFieldError(field: NutritionEditorSignalForm[keyof NutritionEditorSignalForm]): string | null {
        if (field().errors()[0]?.kind === 'required') {
            return null;
        }

        return resolveSignalFormFieldError(field, this.validationErrors, this.translateService);
    }
}

type ProductNutritionValues = {
    alcohol: number;
    calories: number;
    carbs: number;
    fats: number;
    fiber: number;
    proteins: number;
};

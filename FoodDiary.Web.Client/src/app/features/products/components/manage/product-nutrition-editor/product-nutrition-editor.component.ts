import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal } from '@angular/core';
import { type FormControl, type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';
import { EMPTY, merge, type Observable } from 'rxjs';

import type {
    NutritionMacroState,
    NutritionMismatchWarning,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor.component';
import {
    type NutritionControlNames,
    NutritionEditorComponent,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor.component';
import { DEFAULT_CALORIE_MISMATCH_THRESHOLD, DEFAULT_NUTRITION_BASE_AMOUNT } from '../../../../../shared/lib/nutrition.constants';
import {
    calculateCalorieMismatchWarning,
    calculateMacroBarState,
    checkCaloriesError,
    checkMacrosError,
    getControlNumericValue,
} from '../../../../../shared/lib/nutrition-form.utils';
import { MeasurementUnit } from '../../../models/product.data';
import type { NutritionMode, ProductFormData } from '../product-manage-lib/product-manage-form.types';

@Component({
    selector: 'fd-product-nutrition-editor',
    templateUrl: './product-nutrition-editor.component.html',
    styleUrls: ['./product-nutrition-editor.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
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

    public readonly formGroup = input.required<FormGroup<ProductFormData>>();
    public readonly nutritionMode = input.required<NutritionMode>();
    public readonly macroBarState = signal<NutritionMacroState>({ isEmpty: true, segments: [] });
    public readonly nutritionWarning = signal<NutritionMismatchWarning | null>(null);
    public readonly nutritionModeOptions = signal<FdUiSegmentedToggleOption[]>([]);
    public readonly nutritionControlNames: NutritionControlNames = {
        calories: 'caloriesPerBase',
        proteins: 'proteinsPerBase',
        fats: 'fatsPerBase',
        carbs: 'carbsPerBase',
        fiber: 'fiberPerBase',
        alcohol: 'alcoholPerBase',
    };

    public readonly nutritionModeChange = output<string>();
    public readonly openAiRecognition = output();

    public constructor() {
        effect(onCleanup => {
            const form = this.formGroup();
            const languageChanges = (this.translateService as { onLangChange?: Observable<unknown> }).onLangChange ?? EMPTY;
            const refresh = (): void => {
                this.buildNutritionModeOptions();
            };

            refresh();
            const subscription = merge(form.controls.baseUnit.valueChanges, languageChanges).subscribe(() => {
                refresh();
            });
            onCleanup(() => {
                subscription.unsubscribe();
            });
        });

        effect(onCleanup => {
            const form = this.formGroup();
            const refresh = (): void => {
                this.updateCalorieWarning();
                this.updateMacroDistribution();
            };

            refresh();
            const subscription = form.valueChanges.subscribe(() => {
                refresh();
            });
            onCleanup(() => {
                subscription.unsubscribe();
            });
        });
    }

    public caloriesError(): string | null {
        const control = this.formGroup().controls.caloriesPerBase;
        return checkCaloriesError(control) ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.CALORIES_REQUIRED') : null;
    }

    public macrosError(): string | null {
        const controls = [
            this.formGroup().controls.proteinsPerBase,
            this.formGroup().controls.fatsPerBase,
            this.formGroup().controls.carbsPerBase,
            this.formGroup().controls.alcoholPerBase,
        ];

        return checkMacrosError(controls) ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED') : null;
    }

    private buildNutritionModeOptions(): void {
        const baseUnit = this.formGroup().controls.baseUnit.value;
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

    private updateCalorieWarning(): void {
        const form = this.formGroup();
        const calories = this.getNumberValue(form.controls.caloriesPerBase);
        const proteins = this.getNumberValue(form.controls.proteinsPerBase);
        const fats = this.getNumberValue(form.controls.fatsPerBase);
        const carbs = this.getNumberValue(form.controls.carbsPerBase);
        const alcohol = this.getNumberValue(form.controls.alcoholPerBase);
        this.nutritionWarning.set(
            calculateCalorieMismatchWarning({
                calories,
                proteins,
                fats,
                carbs,
                alcohol,
                threshold: DEFAULT_CALORIE_MISMATCH_THRESHOLD,
            }),
        );
    }

    private updateMacroDistribution(): void {
        const form = this.formGroup();
        const proteins = this.getNumberValue(form.controls.proteinsPerBase);
        const fats = this.getNumberValue(form.controls.fatsPerBase);
        const carbs = this.getNumberValue(form.controls.carbsPerBase);
        this.macroBarState.set(calculateMacroBarState(proteins, fats, carbs));
    }

    private getNumberValue(control: FormControl<number | string | null>): number {
        return getControlNumericValue(control);
    }
}

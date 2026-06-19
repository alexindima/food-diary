import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { FieldTree } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';
import { EMPTY, type Observable } from 'rxjs';

import {
    NutritionEditorComponent,
    type NutritionEditorSignalForm,
    type NutritionEditorWarning,
    type NutritionMacroState,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor';
import { DEFAULT_CALORIE_MISMATCH_THRESHOLD } from '../../../../../shared/lib/nutrition.constants';
import {
    calculateCalorieMismatchWarning,
    calculateMacroBarState,
    checkCaloriesError,
    checkMacrosError,
} from '../../../../../shared/lib/nutrition-form.utils';
import type { NutritionMode, NutritionScaleMode, RecipeFormValues } from '../recipe-manage-lib/recipe-manage.types';

@Component({
    selector: 'fd-recipe-nutrition-editor',
    templateUrl: './recipe-nutrition-editor.html',
    styleUrls: ['./recipe-nutrition-editor.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiHintDirective, FdUiCardComponent, FdUiSegmentedToggleComponent, NutritionEditorComponent],
})
export class RecipeNutritionEditorComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly form = input.required<FieldTree<RecipeFormValues>>();
    public readonly nutritionMode = input.required<NutritionMode>();
    public readonly nutritionScaleMode = input.required<NutritionScaleMode>();
    protected readonly nutritionWarning = signal<NutritionEditorWarning | null>(null);
    protected readonly nutritionModeOptions = signal<FdUiSegmentedToggleOption[]>([]);
    protected readonly nutritionScaleModeOptions = signal<FdUiSegmentedToggleOption[]>([]);
    protected readonly isNutritionReadonly = computed(() => this.nutritionMode() === 'auto');
    protected readonly showManualNutritionHint = computed(() => !this.isNutritionReadonly());
    protected readonly macroBarState = computed<NutritionMacroState>(() => {
        const form = this.form();
        return calculateMacroBarState(
            this.getNumberValue(form.manualProteins().value()),
            this.getNumberValue(form.manualFats().value()),
            this.getNumberValue(form.manualCarbs().value()),
        );
    });
    protected readonly nutritionForm = computed<NutritionEditorSignalForm>(() => {
        const form = this.form();
        return {
            calories: form.manualCalories,
            proteins: form.manualProteins,
            fats: form.manualFats,
            carbs: form.manualCarbs,
            fiber: form.manualFiber,
            alcohol: form.manualAlcohol,
        };
    });

    public readonly nutritionModeChange = output<string>();
    public readonly nutritionScaleModeChange = output<string>();

    public constructor() {
        const languageChanges = (this.translateService as { onLangChange?: Observable<unknown> }).onLangChange ?? EMPTY;
        languageChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.refreshTranslatedState();
        });

        effect(() => {
            this.refreshTranslatedState();
        });

        effect(() => {
            this.updateCalorieWarning();
        });
    }

    protected caloriesError(): string | null {
        if (this.nutritionMode() === 'auto') {
            return null;
        }

        return checkCaloriesError(this.getControlState('manualCalories'))
            ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.CALORIES_REQUIRED')
            : null;
    }

    protected macrosError(): string | null {
        if (this.nutritionMode() === 'auto') {
            return null;
        }

        return checkMacrosError([
            this.getControlState('manualProteins'),
            this.getControlState('manualFats'),
            this.getControlState('manualCarbs'),
            this.getControlState('manualAlcohol'),
        ])
            ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED')
            : null;
    }

    private buildNutritionModeOptions(): void {
        this.nutritionModeOptions.set([
            {
                value: 'auto',
                label: this.translateService.instant('RECIPE_MANAGE.NUTRITION_MODE.AUTO'),
            },
            {
                value: 'manual',
                label: this.translateService.instant('RECIPE_MANAGE.NUTRITION_MODE.MANUAL'),
            },
        ]);
    }

    private buildNutritionScaleModeOptions(): void {
        this.nutritionScaleModeOptions.set([
            {
                value: 'recipe',
                label: this.translateService.instant('RECIPE_MANAGE.NUTRITION_SCALE_MODE.RECIPE'),
            },
            {
                value: 'portion',
                label: this.translateService.instant('RECIPE_MANAGE.NUTRITION_SCALE_MODE.PORTION'),
            },
        ]);
    }

    private refreshTranslatedState(): void {
        this.buildNutritionModeOptions();
        this.buildNutritionScaleModeOptions();
    }

    private updateCalorieWarning(): void {
        if (this.nutritionMode() === 'auto') {
            this.nutritionWarning.set(null);
            return;
        }

        const form = this.form();
        const calories = this.getNumberValue(form.manualCalories().value());
        const proteins = this.getNumberValue(form.manualProteins().value());
        const fats = this.getNumberValue(form.manualFats().value());
        const carbs = this.getNumberValue(form.manualCarbs().value());
        const alcohol = this.getNumberValue(form.manualAlcohol().value());
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

    private getNumberValue(value: number | string | null): number {
        if (value === null || value === '') {
            return 0;
        }

        const normalized = typeof value === 'string' ? Number(value.replace(',', '.').replaceAll(/[^\d.-]/g, '')) : Number(value);
        return Number.isFinite(normalized) ? Math.max(0, normalized) : 0;
    }

    private getControlState(
        field: keyof Pick<RecipeFormValues, 'manualCalories' | 'manualProteins' | 'manualFats' | 'manualCarbs' | 'manualAlcohol'>,
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
}

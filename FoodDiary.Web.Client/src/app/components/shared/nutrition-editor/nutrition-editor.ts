import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiNutrientInputComponent } from 'fd-ui-kit/nutrient-input/fd-ui-nutrient-input';

import { NutritionEditorMessagesComponent } from './nutrition-editor-messages';

export type NutritionMacroSegment = {
    key: 'proteins' | 'fats' | 'carbs';
    percent: number;
};

export type NutritionMacroState = {
    isEmpty: boolean;
    segments: NutritionMacroSegment[];
};

export type NutritionMismatchWarning = {
    kind: 'caloriesMismatch';
    expectedCalories: number;
    actualCalories: number;
};

export type NutritionTextWarning = {
    kind: 'text';
    messageKey: string;
    params?: Record<string, number | string>;
};

export type NutritionEditorWarning = NutritionMismatchWarning | NutritionTextWarning;

export type NutritionFormModel = {
    calories: number | null;
    proteins: number | null;
    fats: number | null;
    carbs: number | null;
    fiber: number | null;
    alcohol: number | null;
};

export type NutritionEditorSignalForm = Pick<
    FieldTree<NutritionFormModel>,
    'calories' | 'proteins' | 'fats' | 'carbs' | 'fiber' | 'alcohol'
>;

export type NutritionEditorFieldErrors = Partial<Record<keyof NutritionFormModel, string | null>>;

@Component({
    selector: 'fd-nutrition-editor',
    imports: [CommonModule, FormField, TranslatePipe, FdUiNutrientInputComponent, NutritionEditorMessagesComponent],
    templateUrl: './nutrition-editor.html',
    styleUrls: ['./nutrition-editor.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NutritionEditorComponent {
    public readonly form = input<NutritionEditorSignalForm | null>(null);
    public readonly macroState = input.required<NutritionMacroState>();
    public readonly readonly = input(false);
    public readonly caloriesError = input<string | null>(null);
    public readonly fieldErrors = input<NutritionEditorFieldErrors>({});
    public readonly macrosError = input<string | null>(null);
    public readonly maxCalories = input<number>();
    public readonly maxNutrient = input<number>();
    public readonly showManualHint = input(false);
    public readonly manualHintKey = input('');
    public readonly warning = input<NutritionEditorWarning | null>(null);
    protected readonly hasCaloriesError = computed(() => this.hasText(this.caloriesError()));

    protected readonly nutrientFillColors = {
        calories: 'var(--fd-color-nutrition-calories-fill)',
        proteins: 'var(--fd-color-nutrition-proteins-fill)',
        fats: 'var(--fd-color-nutrition-fats-fill)',
        carbs: 'var(--fd-color-nutrition-carbs-fill)',
        fiber: 'var(--fd-color-nutrition-fiber-fill)',
        alcohol: 'var(--fd-color-nutrition-alcohol-fill)',
    };
    protected readonly nutrientTextColors = {
        calories: 'var(--fd-color-nutrition-calories-text)',
        proteins: 'var(--fd-color-nutrition-proteins-text)',
        fats: 'var(--fd-color-nutrition-fats-text)',
        carbs: 'var(--fd-color-nutrition-carbs-text)',
        fiber: 'var(--fd-color-nutrition-fiber-text)',
        alcohol: 'var(--fd-color-nutrition-alcohol-text)',
    };
    protected readonly macroBarColors = {
        proteins: 'var(--fd-color-nutrition-proteins)',
        fats: 'var(--fd-color-nutrition-fats)',
        carbs: 'var(--fd-color-nutrition-carbs)',
    };
    protected readonly macroBarState = computed<NutritionMacroBarViewModel>(() => {
        const state = this.macroState();

        return {
            isEmpty: state.isEmpty,
            segments: state.segments.map(segment => ({
                ...segment,
                color: this.macroBarColors[segment.key],
            })),
        };
    });

    private hasText(value: string | null): boolean {
        return value !== null && value.trim().length > 0;
    }
}

type NutritionMacroBarViewModel = {
    isEmpty: boolean;
    segments: NutritionMacroSegmentViewModel[];
};

type NutritionMacroSegmentViewModel = {
    color: string;
} & NutritionMacroSegment;

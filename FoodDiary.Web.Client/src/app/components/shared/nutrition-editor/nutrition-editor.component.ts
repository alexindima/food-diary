import { CommonModule } from '@angular/common';
import { Component, input } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiNutrientInputComponent } from 'fd-ui-kit/nutrient-input/fd-ui-nutrient-input.component';

export interface NutritionControlNames {
    calories: string;
    proteins: string;
    fats: string;
    carbs: string;
    fiber: string;
    alcohol: string;
}

export interface NutritionMacroSegment {
    key: 'proteins' | 'fats' | 'carbs';
    percent: number;
}

export interface NutritionMacroState {
    isEmpty: boolean;
    segments: NutritionMacroSegment[];
}

export interface NutritionMismatchWarning {
    expectedCalories: number;
    actualCalories: number;
}

@Component({
    selector: 'fd-nutrition-editor',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, TranslatePipe, FdUiNutrientInputComponent],
    templateUrl: './nutrition-editor.component.html',
    styleUrls: ['./nutrition-editor.component.scss'],
})
export class NutritionEditorComponent {
    public formGroup = input.required<FormGroup>();
    public controlNames = input.required<NutritionControlNames>();
    public macroState = input.required<NutritionMacroState>();
    public readonly = input(false);
    public caloriesError = input<string | null>(null);
    public macrosError = input<string | null>(null);
    public showManualHint = input(false);
    public manualHintKey = input('');
    public warning = input<NutritionMismatchWarning | null>(null);

    public readonly nutrientFillColors = {
        calories: 'var(--fd-color-nutrition-calories-fill)',
        proteins: 'var(--fd-color-nutrition-proteins-fill)',
        fats: 'var(--fd-color-nutrition-fats-fill)',
        carbs: 'var(--fd-color-nutrition-carbs-fill)',
        fiber: 'var(--fd-color-nutrition-fiber-fill)',
        alcohol: 'var(--fd-color-nutrition-alcohol-fill)',
    };
    public readonly nutrientTextColors = {
        calories: 'var(--fd-color-nutrition-calories)',
        proteins: 'var(--fd-color-nutrition-proteins)',
        fats: 'var(--fd-color-nutrition-fats)',
        carbs: 'var(--fd-color-nutrition-carbs)',
        fiber: 'var(--fd-color-nutrition-fiber)',
    };

    public getMacroColor(key: NutritionMacroSegment['key']): string {
        return this.nutrientTextColors[key];
    }
}

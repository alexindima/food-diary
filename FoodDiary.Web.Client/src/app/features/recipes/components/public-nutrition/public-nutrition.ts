import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { PublicRecipe } from '../../models/public-recipe.data';

@Component({
    selector: 'fd-public-nutrition',
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './public-nutrition.html',
    styleUrl: './public-nutrition.scss',
    imports: [DecimalPipe, TranslatePipe],
})
export class PublicNutritionComponent {
    public readonly recipe = input.required<PublicRecipe>();
    protected readonly nutrients = computed(() => {
        const recipe = this.recipe();
        return [
            { key: 'CALORIES', value: recipe.totalCalories, unit: 'KCAL' },
            { key: 'PROTEINS', value: recipe.totalProteins, unit: 'GRAMS' },
            { key: 'FATS', value: recipe.totalFats, unit: 'GRAMS' },
            { key: 'CARBS', value: recipe.totalCarbs, unit: 'GRAMS' },
            { key: 'FIBER', value: recipe.totalFiber, unit: 'GRAMS' },
            { key: 'ALCOHOL', value: recipe.totalAlcohol, unit: 'GRAMS' },
        ].map(item => ({ ...item, value: item.value === null ? null : item.value / Math.max(1, recipe.servings) }));
    });
}

import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent } from 'fd-ui-kit';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import type { PublicRecipe } from '../../models/public-recipe.data';

@Component({
    selector: 'fd-public-nutrition',
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './public-nutrition.html',
    styleUrl: './public-nutrition.scss',
    imports: [LocalizedNumberPipe, TranslatePipe, FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent],
})
export class PublicNutritionComponent {
    public readonly recipe = input.required<PublicRecipe>();
    protected readonly language = injectCurrentLanguage();
    protected readonly partialHint = computed(() => {
        const recipe = this.recipe();
        const names = recipe.missingIngredientNames ?? [];
        const total = recipe.steps.reduce((count, step) => count + step.ingredients.length, 0);
        const complete = names.length === recipe.missingIngredientCount && total >= names.length;
        const key = complete ? 'PARTIAL_HINT_NAMES' : names.length > 0 ? 'PARTIAL_HINT_SOME_NAMES' : 'PARTIAL_HINT';
        return { key: `PUBLIC_RECIPES.${key}`, params: { count: recipe.missingIngredientCount, total, names: names.join(', ') } };
    });
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
    protected readonly hasNutrition = computed(() => this.nutrients().some(nutrient => nutrient.value !== null));
    protected readonly isPartial = computed(() => this.hasNutrition() && this.recipe().missingIngredientCount > 0);
}

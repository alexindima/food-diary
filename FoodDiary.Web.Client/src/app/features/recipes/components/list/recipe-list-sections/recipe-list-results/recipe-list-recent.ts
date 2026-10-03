import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';

import { injectCurrentLanguage } from '../../../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../../../shared/i18n/localized-number.pipe';
import type { Recipe } from '../../../../../../shared/models/recipe.data';
import type { RecipeCardViewModel } from '../../../../lib/recipe-list.types';

@Component({
    selector: 'fd-recipe-list-recent',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiIconComponent, LocalizedNumberPipe],
    templateUrl: './recipe-list-recent.html',
    styleUrl: './recipe-list-results.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeListRecentComponent {
    protected readonly locale = injectCurrentLanguage();
    public readonly recentRecipeItems = input.required<readonly RecipeCardViewModel[]>();
    public readonly recipeOpen = output<Recipe>();
    public readonly recipeAddToMeal = output<Recipe>();

    protected caloriesPerServing(recipe: Recipe): number {
        return (recipe.totalCalories ?? 0) / Math.max(1, recipe.servings);
    }
}

import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';

import { RecipeCardComponent, type RecipeFavoriteChange } from '../../../../components/shared/recipe-card/recipe-card.component';
import type { Recipe } from '../../models/recipe.data';
import type { RecipeCardViewModel } from './recipe-list.component';

export type RecipeFavoriteChangeRequest = {
    recipe: Recipe;
    change: RecipeFavoriteChange;
};

@Component({
    selector: 'fd-recipe-list-results',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiIconComponent, RecipeCardComponent],
    templateUrl: './recipe-list-results.component.html',
    styleUrl: './recipe-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeListResultsComponent {
    public readonly hasVisibleRecipes = input.required<boolean>();
    public readonly showRecentSection = input.required<boolean>();
    public readonly recentRecipeItems = input.required<readonly RecipeCardViewModel[]>();
    public readonly allRecipeItems = input.required<readonly RecipeCardViewModel[]>();
    public readonly allRecipesSectionLabelKey = input.required<string>();
    public readonly isEmptyState = input.required<boolean>();

    public readonly recipeOpen = output<Recipe>();
    public readonly recipeAddToMeal = output<Recipe>();
    public readonly recipeFavoriteChanged = output<RecipeFavoriteChangeRequest>();
    public readonly recipeAdd = output<void>();
}

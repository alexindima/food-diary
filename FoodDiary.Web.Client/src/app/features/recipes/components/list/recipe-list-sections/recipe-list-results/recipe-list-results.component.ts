import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';

import { RecipeCardComponent } from '../../../../../../components/shared/recipe-card/recipe-card.component';
import type { Recipe } from '../../../../models/recipe.data';
import type { RecipeCardViewModel } from '../../../../pages/list/recipe-list.types';

@Component({
    selector: 'fd-recipe-list-results',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiIconComponent, RecipeCardComponent],
    templateUrl: './recipe-list-results.component.html',
    styleUrl: '../../../../pages/list/recipe-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeListResultsComponent {
    public readonly recentRecipeItems = input.required<readonly RecipeCardViewModel[]>();
    public readonly allRecipeItems = input.required<readonly RecipeCardViewModel[]>();
    public readonly allRecipesSectionLabelKey = input.required<string>();
    public readonly emptyState = input.required<RecipeListEmptyState>();
    public readonly favoriteLoadingIds = input.required<ReadonlySet<string>>();
    public readonly showRecentSection = computed(() => this.recentRecipeItems().length > 0);
    public readonly hasVisibleRecipes = computed(() => this.showRecentSection() || this.allRecipeItems().length > 0);

    public readonly recipeOpen = output<Recipe>();
    public readonly recipeAddToMeal = output<Recipe>();
    public readonly recipeFavoriteToggle = output<Recipe>();
    public readonly recipeAdd = output();
}

export type RecipeListEmptyState = 'empty' | 'no-results';

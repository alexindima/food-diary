import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { FavoritesSectionComponent } from '../../../../../../components/shared/favorites-section/favorites-section';
import type { RecipeCardItem } from '../../../../../../components/shared/recipe-card/recipe-card';
import { RecipeCardComponent } from '../../../../../../components/shared/recipe-card/recipe-card';
import type { FavoriteRecipe } from '../../../../models/recipe.data';

@Component({
    selector: 'fd-recipe-list-favorites',
    imports: [TranslatePipe, FavoritesSectionComponent, RecipeCardComponent],
    templateUrl: './recipe-list-favorites.html',
    styleUrl: '../../../../pages/list/recipe-list.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeListFavoritesComponent {
    public readonly favorites = input.required<readonly FavoriteRecipe[]>();
    public readonly favoriteTotalCount = input.required<number>();
    public readonly isFavoritesOpen = input.required<boolean>();
    public readonly isFavoritesLoadingMore = input.required<boolean>();
    public readonly favoriteLoadingIds = input.required<ReadonlySet<string>>();
    protected readonly hasMoreFavorites = computed(() => this.favoriteTotalCount() > this.favorites().length);

    public readonly favoritesToggle = output();
    public readonly favoritesLoadMore = output();
    public readonly favoriteOpen = output<FavoriteRecipe>();
    public readonly favoriteAddToMeal = output<FavoriteRecipe>();
    public readonly favoriteRemove = output<FavoriteRecipe>();

    protected toRecipeCardItem(favorite: FavoriteRecipe): RecipeCardItem {
        return {
            id: favorite.recipeId,
            name: this.resolveFavoriteName(favorite),
            imageUrl: favorite.imageUrl ?? null,
            isOwnedByCurrentUser: true,
            prepTime: 0,
            cookTime: favorite.totalTimeMinutes ?? 0,
            totalCalories: favorite.totalCalories ?? 0,
            totalProteins: 0,
            totalFats: 0,
            totalCarbs: 0,
            totalFiber: 0,
            totalAlcohol: 0,
            steps: [{ ingredients: Array.from({ length: favorite.ingredientCount }, () => ({})) }],
            isFavorite: true,
            favoriteRecipeId: favorite.id,
        };
    }

    private resolveFavoriteName(favorite: FavoriteRecipe): string {
        const name = favorite.name?.trim();
        return name !== undefined && name.length > 0 ? name : favorite.recipeName;
    }
}

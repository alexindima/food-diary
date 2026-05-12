import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { FavoritesSectionComponent } from '../../../../components/shared/favorites-section/favorites-section.component';
import type { FavoriteRecipe } from '../../models/recipe.data';

@Component({
    selector: 'fd-recipe-list-favorites',
    imports: [TranslatePipe, FdUiHintDirective, FdUiButtonComponent, FavoritesSectionComponent],
    templateUrl: './recipe-list-favorites.component.html',
    styleUrl: './recipe-list.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeListFavoritesComponent {
    public readonly favorites = input.required<readonly FavoriteRecipe[]>();
    public readonly favoriteTotalCount = input.required<number>();
    public readonly isFavoritesOpen = input.required<boolean>();
    public readonly hasMoreFavorites = input.required<boolean>();
    public readonly isFavoritesLoadingMore = input.required<boolean>();

    public readonly favoritesToggle = output();
    public readonly favoritesLoadMore = output();
    public readonly favoriteOpen = output<FavoriteRecipe>();
    public readonly favoriteAddToMeal = output<FavoriteRecipe>();
    public readonly favoriteRemove = output<FavoriteRecipe>();
}

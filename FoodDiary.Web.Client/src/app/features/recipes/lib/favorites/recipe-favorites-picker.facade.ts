import { DestroyRef, inject, Injectable } from '@angular/core';

import type { FavoriteRecipe } from '../../../../shared/models/recipe.data';
import type { FavoriteRecipeId } from '../../../../shared/models/semantics/entity-id';
import { FavoritesPageState } from '../../../../shared/ui/favorites/favorites-page-state';
import { FavoriteRecipeService } from '../../api/favorite-recipe.service';

@Injectable()
export class RecipeFavoritesPickerFacade {
    private readonly api = inject(FavoriteRecipeService);
    private readonly state = new FavoritesPageState<FavoriteRecipe>(
        (page, size, search) => this.api.getPage(page, size, search),
        inject(DestroyRef),
    );
    public readonly items = this.state.items;
    public readonly loading = this.state.loading;
    public readonly failed = this.state.failed;
    public readonly page = this.state.page;
    public readonly total = this.state.total;
    public readonly search = this.state.search;
    public readonly pageSize = this.state.pageSize;
    public readonly revision = this.state.revision;
    public readonly removedIds = this.state.removedIds;
    public readonly paginationTotal = this.state.paginationTotal;

    public markRemoved(id: FavoriteRecipeId): void {
        this.state.markRemoved(id);
    }

    public markRestored(id: FavoriteRecipeId): void {
        this.state.markRestored(id);
    }

    public load(page = 1, search = this.search()): void {
        this.state.load(page, search);
    }
}

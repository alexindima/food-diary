import { DestroyRef, inject, Injectable } from '@angular/core';

import type { FavoriteMeal } from '../../../../shared/models/meal.data';
import type { FavoriteMealId } from '../../../../shared/models/semantics/entity-id';
import { FavoritesPageState } from '../../../../shared/ui/favorites/favorites-page-state';
import { FavoriteMealService } from '../../api/favorite-meal.service';

@Injectable()
export class MealFavoritesPickerFacade {
    private readonly api = inject(FavoriteMealService);
    private readonly state = new FavoritesPageState<FavoriteMeal>(
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

    public markRemoved(id: FavoriteMealId): void {
        this.state.markRemoved(id);
    }

    public markRestored(id: FavoriteMealId): void {
        this.state.markRestored(id);
    }

    public load(page = 1, search = this.search()): void {
        this.state.load(page, search);
    }
}

import { DestroyRef, inject, Injectable } from '@angular/core';

import type { FavoriteProduct } from '../../../../shared/models/product.data';
import type { FavoriteProductId } from '../../../../shared/models/semantics/entity-id';
import { FavoritesPageState } from '../../../../shared/ui/favorites/favorites-page-state';
import { FavoriteProductService } from '../../api/favorite-product.service';

@Injectable()
export class ProductFavoritesPickerFacade {
    private readonly api = inject(FavoriteProductService);
    private readonly state = new FavoritesPageState<FavoriteProduct>(
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

    public markRemoved(id: FavoriteProductId): void {
        this.state.markRemoved(id);
    }

    public markRestored(id: FavoriteProductId): void {
        this.state.markRestored(id);
    }

    public load(page = 1, search = this.search()): void {
        this.state.load(page, search);
    }
}

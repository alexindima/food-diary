import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { FavoritesSectionComponent } from '../../../../../../components/shared/favorites-section/favorites-section';
import { ProductCardComponent, type ProductCardItem } from '../../../../../../components/shared/product-card/product-card';
import type { FavoriteProduct } from '../../../../models/product.data';

@Component({
    selector: 'fd-product-list-favorites',
    imports: [TranslatePipe, ProductCardComponent, FavoritesSectionComponent],
    templateUrl: './product-list-favorites.html',
    styleUrl: '../../product-list-base/product-list-base.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class ProductListFavoritesComponent {
    public readonly favorites = input.required<FavoriteProduct[]>();
    public readonly totalCount = input.required<number>();
    public readonly isOpen = input.required<boolean>();
    public readonly isLoadingMore = input.required<boolean>();
    public readonly favoriteLoadingIds = input<ReadonlySet<string>>(new Set<string>());

    protected readonly count = computed(() => this.totalCount());
    protected readonly showLoadMore = computed(() => this.totalCount() > this.favorites().length);

    public readonly favoritesToggle = output();
    public readonly loadMore = output();
    public readonly openProduct = output<FavoriteProduct>();
    public readonly addToMeal = output<FavoriteProduct>();
    public readonly favoriteRemove = output<FavoriteProduct>();

    protected toProductCardItem(favorite: FavoriteProduct): ProductCardItem {
        return {
            id: favorite.productId,
            name: this.resolveFavoriteName(favorite),
            brand: favorite.brand,
            barcode: favorite.barcode,
            comment: favorite.comment,
            isOwnedByCurrentUser: favorite.isOwnedByCurrentUser,
            proteinsPerBase: favorite.proteinsPerBase,
            fatsPerBase: favorite.fatsPerBase,
            carbsPerBase: favorite.carbsPerBase,
            fiberPerBase: favorite.fiberPerBase,
            alcoholPerBase: favorite.alcoholPerBase,
            caloriesPerBase: favorite.caloriesPerBase,
            qualityScore: favorite.qualityScore,
            qualityGrade: favorite.qualityGrade,
            isFavorite: true,
            favoriteProductId: favorite.id,
        };
    }

    private resolveFavoriteName(favorite: FavoriteProduct): string {
        const name = favorite.name?.trim();
        return name !== undefined && name.length > 0 ? name : favorite.productName;
    }
}

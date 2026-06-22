import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { FavoritesSectionComponent } from '../../../../../../components/shared/favorites-section/favorites-section';
import { ProductCardComponent, type ProductCardItem } from '../../../../../../components/shared/product-card/product-card';
import type { FavoriteProduct } from '../../../../models/product.data';

@Component({
    selector: 'fd-product-list-favorites',
    imports: [TranslatePipe, FdUiHintDirective, FdUiButtonComponent, ProductCardComponent, FavoritesSectionComponent],
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
    public readonly portionSavingIds = input<ReadonlySet<string>>(new Set<string>());

    protected readonly count = computed(() => this.totalCount());
    protected readonly showLoadMore = computed(() => this.totalCount() > this.favorites().length);
    protected readonly portionDrafts = signal<Record<string, string>>({});

    public readonly favoritesToggle = output();
    public readonly loadMore = output();
    public readonly openProduct = output<FavoriteProduct>();
    public readonly addToMeal = output<FavoriteProduct>();
    public readonly favoriteRemove = output<FavoriteProduct>();
    public readonly preferredPortionSave = output<{ favorite: FavoriteProduct; preferredPortionAmount: number }>();

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

    protected portionDraftValue(favorite: FavoriteProduct): string {
        return this.portionDrafts()[favorite.id] ?? String(favorite.preferredPortionAmount);
    }

    protected onPortionInput(favorite: FavoriteProduct, event: Event): void {
        if (!(event.target instanceof HTMLInputElement)) {
            return;
        }

        const value = event.target.value;
        this.portionDrafts.update(current => ({
            ...current,
            [favorite.id]: value,
        }));
    }

    protected onPortionSave(favorite: FavoriteProduct): void {
        const parsed = Number(this.portionDraftValue(favorite));
        if (!Number.isFinite(parsed) || parsed <= 0) {
            this.resetPortionDraft(favorite);
            return;
        }

        if (parsed === favorite.preferredPortionAmount) {
            return;
        }

        this.preferredPortionSave.emit({ favorite, preferredPortionAmount: parsed });
    }

    protected onPortionBlur(favorite: FavoriteProduct): void {
        this.onPortionSave(favorite);
    }

    protected onPortionKeydown(favorite: FavoriteProduct, event: KeyboardEvent): void {
        if (event.key !== 'Enter') {
            return;
        }

        event.preventDefault();
        this.onPortionSave(favorite);
    }

    private resetPortionDraft(favorite: FavoriteProduct): void {
        this.portionDrafts.update(current => ({
            ...current,
            [favorite.id]: String(favorite.preferredPortionAmount),
        }));
    }

    private resolveFavoriteName(favorite: FavoriteProduct): string {
        const name = favorite.name?.trim();
        return name !== undefined && name.length > 0 ? name : favorite.productName;
    }
}

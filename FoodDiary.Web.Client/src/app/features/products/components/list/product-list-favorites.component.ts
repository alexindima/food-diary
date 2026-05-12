import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { FavoritesSectionComponent } from '../../../../components/shared/favorites-section/favorites-section.component';
import type { FavoriteProduct } from '../../models/product.data';

@Component({
    selector: 'fd-product-list-favorites',
    imports: [DecimalPipe, TranslatePipe, FdUiHintDirective, FdUiButtonComponent, FavoritesSectionComponent],
    templateUrl: './product-list-favorites.component.html',
    styleUrl: './product-list-base.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class ProductListFavoritesComponent {
    public readonly favorites = input.required<FavoriteProduct[]>();
    public readonly count = input.required<number>();
    public readonly isOpen = input.required<boolean>();
    public readonly showLoadMore = input.required<boolean>();
    public readonly isLoadingMore = input.required<boolean>();

    public readonly favoritesToggle = output();
    public readonly loadMore = output();
    public readonly openProduct = output<FavoriteProduct>();
    public readonly addToMeal = output<FavoriteProduct>();
    public readonly favoriteRemove = output<FavoriteProduct>();
}

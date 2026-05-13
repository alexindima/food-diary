import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { ProductCardComponent } from '../../../../../../components/shared/product-card/product-card.component';
import type { Product } from '../../../../models/product.data';
import type { ProductCardViewModel } from '../../product-list.types';

@Component({
    selector: 'fd-product-list-groups',
    imports: [TranslatePipe, ProductCardComponent],
    templateUrl: './product-list-groups.component.html',
    styleUrl: '../../product-list-base/product-list-base.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class ProductListGroupsComponent {
    public readonly recentItems = input.required<ProductCardViewModel[]>();
    public readonly allItems = input.required<ProductCardViewModel[]>();
    public readonly allProductsSectionLabelKey = input.required<string>();
    public readonly favoriteLoadingIds = input.required<ReadonlySet<string>>();

    protected readonly showRecentSection = computed(() => this.recentItems().length > 0);

    public readonly openProduct = output<Product>();
    public readonly addToMeal = output<Product>();
    public readonly favoriteToggle = output<Product>();
}

import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';

import { ProductCardComponent } from '../../../../../../components/shared/product-card/product-card';
import { injectCurrentLanguage } from '../../../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../../../shared/i18n/localized-number.pipe';
import type { Product } from '../../../../models/product.data';
import type { ProductCardViewModel } from '../../product-list.types';

@Component({
    selector: 'fd-product-list-groups',
    imports: [TranslatePipe, ProductCardComponent, FdUiButtonComponent, FdUiIconComponent, LocalizedNumberPipe],
    templateUrl: './product-list-groups.html',
    styleUrls: ['../../product-list-base/product-list-base.scss', './product-list-groups.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class ProductListGroupsComponent {
    protected readonly locale = injectCurrentLanguage();
    public readonly recentItems = input.required<ProductCardViewModel[]>();
    public readonly allItems = input.required<ProductCardViewModel[]>();
    public readonly allProductsSectionLabelKey = input.required<string>();
    public readonly favoriteLoadingIds = input.required<ReadonlySet<string>>();

    protected readonly showRecentSection = computed(() => this.recentItems().length > 0);

    public readonly openProduct = output<Product>();
    public readonly addToMeal = output<Product>();
    public readonly favoriteToggle = output<Product>();
}

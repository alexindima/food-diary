import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiInlineAlertComponent } from 'fd-ui-kit/inline-alert/fd-ui-inline-alert';

import { SkeletonCardComponent } from '../../../../../../components/shared/skeleton-card/skeleton-card';
import type { OpenFoodFactsProduct } from '../../../../models/open-food-facts.data';

@Component({
    selector: 'fd-product-list-off-section',
    imports: [DecimalPipe, TranslatePipe, FdUiIconComponent, FdUiInlineAlertComponent, SkeletonCardComponent],
    templateUrl: './product-list-off-section.html',
    styleUrl: '../../product-list-base/product-list-base.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class ProductListOffSectionComponent {
    public readonly products = input.required<OpenFoodFactsProduct[]>();
    public readonly isLoading = input.required<boolean>();
    public readonly hasError = input(false);

    public readonly productOpen = output<OpenFoodFactsProduct>();
    public readonly retry = output();
}

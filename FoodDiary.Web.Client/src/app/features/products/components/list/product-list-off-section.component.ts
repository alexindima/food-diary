import { DecimalPipe, NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';

import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card.component';
import type { OpenFoodFactsProduct } from '../../api/open-food-facts.service';

@Component({
    selector: 'fd-product-list-off-section',
    imports: [DecimalPipe, NgOptimizedImage, TranslatePipe, FdUiIconComponent, SkeletonCardComponent],
    templateUrl: './product-list-off-section.component.html',
    styleUrl: './product-list-base.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class ProductListOffSectionComponent {
    public readonly products = input.required<OpenFoodFactsProduct[]>();
    public readonly isLoading = input.required<boolean>();

    public readonly productOpen = output<OpenFoodFactsProduct>();
}

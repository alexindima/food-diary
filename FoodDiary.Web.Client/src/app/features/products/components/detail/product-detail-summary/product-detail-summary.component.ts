import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';

import type { Product } from '../../../models/product.data';
import type { ProductDetailMacroBlock } from '../product-detail-lib/product-detail-nutrition.mapper';

@Component({
    selector: 'fd-product-detail-summary',
    imports: [TranslatePipe, FdUiHintDirective, FdUiAccentSurfaceComponent],
    templateUrl: './product-detail-summary.component.html',
    styleUrl: '../product-detail/product-detail.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductDetailSummaryComponent {
    public readonly product = input.required<Product>();
    public readonly calories = input.required<number>();
    public readonly baseUnitKey = input.required<string>();
    public readonly productTypeKey = input.required<string>();
    public readonly qualityScore = input.required<number>();
    public readonly qualityGrade = input.required<string>();
    public readonly macroSummaryBlocks = input.required<ProductDetailMacroBlock[]>();

    protected readonly qualityHintKey = computed(() => `QUALITY.${this.qualityGrade().toUpperCase()}`);
}

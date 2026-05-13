import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';

export type ProductListEmptyState = 'empty' | 'no-results';

@Component({
    selector: 'fd-product-list-empty-state',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiIconComponent],
    templateUrl: './product-list-empty-state.component.html',
    styleUrl: '../../product-list-base/product-list-base.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class ProductListEmptyStateComponent {
    public readonly state = input.required<ProductListEmptyState>();

    public readonly addProduct = output();
}

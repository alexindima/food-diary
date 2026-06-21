import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

@Component({
    selector: 'fd-product-list-active-filters',
    imports: [TranslatePipe, FdUiHintDirective, FdUiButtonComponent],
    templateUrl: './product-list-active-filters.html',
    styleUrl: '../../product-list-base/product-list-base.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductListActiveFiltersComponent {
    public readonly filterKeys = input.required<readonly string[]>();
    public readonly filtersEdit = output();
}

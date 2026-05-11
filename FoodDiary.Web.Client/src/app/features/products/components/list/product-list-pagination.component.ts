import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';

@Component({
    selector: 'fd-product-list-pagination',
    imports: [FdUiPaginationComponent],
    templateUrl: './product-list-pagination.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class ProductListPaginationComponent {
    public readonly totalPages = input.required<number>();
    public readonly length = input.required<number>();
    public readonly pageSize = input.required<number>();
    public readonly pageIndex = input.required<number>();

    public readonly pageChange = output<number>();
}

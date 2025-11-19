
import { Component, input, output } from '@angular/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';

@Component({
    selector: 'fd-ui-pagination',
    standalone: true,
    imports: [MatPaginatorModule],
    template: `
        <mat-paginator class="fd-ui-pagination"
            [length]="length()"
            [pageSize]="pageSize()"
            [hidePageSize]="true"
            [pageIndex]="pageIndex()"
            (page)="onPage($event)"
         />
    `,
    styleUrls: ['./fd-ui-pagination.component.scss'],
})
export class FdUiPaginationComponent {
    public readonly length = input(0);
    public readonly pageSize = input(10);
    public readonly pageIndex = input(0);
    public readonly pageIndexChange = output<number>();

    public onPage(event: PageEvent): void {
        this.pageIndexChange.emit(event.pageIndex);
    }
}

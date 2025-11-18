import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';

@Component({
    selector: 'fd-ui-pagination',
    standalone: true,
    imports: [CommonModule, MatPaginatorModule],
    template: `
        <mat-paginator
            class="fd-ui-pagination"
            [length]="length"
            [pageSize]="pageSize"
            [hidePageSize]="true"
            [pageIndex]="pageIndex"
            (page)="onPage($event)"
        ></mat-paginator>
    `,
    styleUrls: ['./fd-ui-pagination.component.scss'],
})
export class FdUiPaginationComponent {
    @Input() public length = 0;
    @Input() public pageSize = 10;
    @Input() public pageIndex = 0;
    @Output() public pageIndexChange = new EventEmitter<number>();

    public onPage(event: PageEvent): void {
        this.pageIndexChange.emit(event.pageIndex);
    }
}

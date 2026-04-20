import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

@Component({
    selector: 'fd-ui-pagination',
    standalone: true,
    template: `
        @if (pageCount() > 1) {
            <nav class="fd-ui-pagination" aria-label="Pagination">
                <button class="fd-ui-pagination__button" type="button" [disabled]="pageIndex() <= 0" (click)="goToPage(pageIndex() - 1)">
                    Prev
                </button>

                @for (page of visiblePages(); track page) {
                    <button
                        class="fd-ui-pagination__button"
                        type="button"
                        [class.fd-ui-pagination__button--active]="page === pageIndex()"
                        [attr.aria-current]="page === pageIndex() ? 'page' : null"
                        (click)="goToPage(page)"
                    >
                        {{ page + 1 }}
                    </button>
                }

                <button
                    class="fd-ui-pagination__button"
                    type="button"
                    [disabled]="pageIndex() >= pageCount() - 1"
                    (click)="goToPage(pageIndex() + 1)"
                >
                    Next
                </button>
            </nav>
        }
    `,
    styleUrls: ['./fd-ui-pagination.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiPaginationComponent {
    public readonly length = input(0);
    public readonly pageSize = input(10);
    public readonly pageIndex = input(0);
    public readonly pageIndexChange = output<number>();

    protected readonly pageCount = computed(() => {
        const size = Math.max(this.pageSize(), 1);
        return Math.max(1, Math.ceil(this.length() / size));
    });

    protected readonly visiblePages = computed(() => {
        const count = this.pageCount();
        const current = Math.min(Math.max(this.pageIndex(), 0), count - 1);
        const start = Math.max(0, current - 2);
        const end = Math.min(count, start + 5);
        const adjustedStart = Math.max(0, end - 5);

        return Array.from({ length: end - adjustedStart }, (_, index) => adjustedStart + index);
    });

    public goToPage(index: number): void {
        const nextIndex = Math.min(Math.max(index, 0), this.pageCount() - 1);
        if (nextIndex === this.pageIndex()) {
            return;
        }

        this.pageIndexChange.emit(nextIndex);
    }
}

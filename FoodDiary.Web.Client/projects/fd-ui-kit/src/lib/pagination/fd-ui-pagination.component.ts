import { ChangeDetectionStrategy, Component, computed, input, model } from '@angular/core';

const DEFAULT_PAGE_SIZE = 10;
const VISIBLE_PAGE_RADIUS = 2;
const MAX_VISIBLE_PAGES = 5;

@Component({
    selector: 'fd-ui-pagination',
    standalone: true,
    templateUrl: './fd-ui-pagination.component.html',
    styleUrls: ['./fd-ui-pagination.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiPaginationComponent {
    public readonly length = input(0);
    public readonly pageSize = input(DEFAULT_PAGE_SIZE);
    public readonly pageIndex = model(0);

    protected readonly pageCount = computed(() => {
        const size = Math.max(this.pageSize(), 1);
        return Math.max(1, Math.ceil(this.length() / size));
    });

    protected readonly visiblePages = computed(() => {
        const count = this.pageCount();
        const current = Math.min(Math.max(this.pageIndex(), 0), count - 1);
        const start = Math.max(0, current - VISIBLE_PAGE_RADIUS);
        const end = Math.min(count, start + MAX_VISIBLE_PAGES);
        const adjustedStart = Math.max(0, end - MAX_VISIBLE_PAGES);

        return Array.from({ length: end - adjustedStart }, (_, index) => adjustedStart + index);
    });

    public goToPage(index: number): void {
        const nextIndex = Math.min(Math.max(index, 0), this.pageCount() - 1);
        if (nextIndex === this.pageIndex()) {
            return;
        }

        this.pageIndex.set(nextIndex);
    }
}

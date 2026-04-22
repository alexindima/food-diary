import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

@Component({
    selector: 'fd-ui-pagination',
    standalone: true,
    templateUrl: './fd-ui-pagination.component.html',
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

import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';

@Component({
    selector: 'fd-favorites-section',
    templateUrl: './favorites-section.html',
    styleUrl: './favorites-section.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiIconComponent, FdUiButtonComponent],
})
export class FavoritesSectionComponent {
    public readonly title = input.required<string>();
    public readonly count = input<number>(0);
    public readonly icon = input('star');
    public readonly isOpen = input(false);
    public readonly showLoadMore = input(false);
    public readonly isLoadingMore = input(false);
    public readonly loadMoreLabel = input<string | null>(null);

    public readonly toggleRequested = output();
    public readonly loadMore = output();

    protected onToggle(): void {
        this.toggleRequested.emit();
    }

    protected onLoadMore(): void {
        if (!this.showLoadMore() || this.isLoadingMore()) {
            return;
        }

        this.loadMore.emit();
    }
}

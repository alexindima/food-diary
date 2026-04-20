import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';

@Component({
    selector: 'fd-favorites-section',
    standalone: true,
    templateUrl: './favorites-section.component.html',
    styleUrl: './favorites-section.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiIconComponent, FdUiButtonComponent],
})
export class FavoritesSectionComponent {
    public readonly title = input.required<string>();
    public readonly count = input<number>(0);
    public readonly isOpen = input(false);
    public readonly showLoadMore = input(false);
    public readonly isLoadingMore = input(false);
    public readonly loadMoreLabel = input<string | null>(null);

    public readonly toggle = output<void>();
    public readonly loadMore = output<void>();

    public onToggle(): void {
        this.toggle.emit();
    }

    public onLoadMore(): void {
        if (!this.showLoadMore() || this.isLoadingMore()) {
            return;
        }

        this.loadMore.emit();
    }
}

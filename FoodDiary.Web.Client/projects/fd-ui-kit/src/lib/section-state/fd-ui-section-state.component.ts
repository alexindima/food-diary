import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { FdUiButtonComponent } from '../button/fd-ui-button.component';
import { type FdUiEmptyStateAppearance, FdUiEmptyStateComponent } from '../empty-state/fd-ui-empty-state.component';
import { FdUiIconComponent } from '../icon/fd-ui-icon.component';
import { FdUiLoaderComponent } from '../loader/fd-ui-loader.component';

export type FdUiSectionState = 'content' | 'loading' | 'empty' | 'error';
export type FdUiSectionStateAppearance = 'default' | 'compact';

@Component({
    selector: 'fd-ui-section-state',
    standalone: true,
    imports: [FdUiButtonComponent, FdUiEmptyStateComponent, FdUiIconComponent, FdUiLoaderComponent],
    templateUrl: './fd-ui-section-state.component.html',
    styleUrl: './fd-ui-section-state.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'fd-ui-section-state',
        '[class.fd-ui-section-state--compact]': 'appearance() === "compact"',
    },
})
export class FdUiSectionStateComponent {
    public readonly state = input<FdUiSectionState>('content');
    public readonly appearance = input<FdUiSectionStateAppearance>('default');

    public readonly loadingLabel = input<string | null>('Loading');

    public readonly emptyTitle = input<string | null>(null);
    public readonly emptyMessage = input<string>('');
    public readonly emptyIcon = input<string>('inventory_2');

    public readonly errorTitle = input<string>('Unable to load this section');
    public readonly errorMessage = input<string>('Try again in a moment.');
    public readonly errorIcon = input<string>('error_outline');
    public readonly retryLabel = input<string | null>(null);

    public readonly retry = output<void>();

    protected readonly emptyAppearance = computed<FdUiEmptyStateAppearance>(() =>
        this.appearance() === 'compact' ? 'compact' : 'default',
    );

    public onRetry(): void {
        this.retry.emit();
    }
}

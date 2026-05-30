import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { FdUiIconComponent } from '../icon/fd-ui-icon';

export type FdUiEmptyStateAppearance = 'default' | 'compact';

@Component({
    selector: 'fd-ui-empty-state',
    imports: [FdUiIconComponent],
    templateUrl: './fd-ui-empty-state.html',
    styleUrl: './fd-ui-empty-state.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'fd-ui-empty-state',
        '[class.fd-ui-empty-state--compact]': 'appearance() === "compact"',
    },
})
export class FdUiEmptyStateComponent {
    public readonly title = input<string | null>(null);
    public readonly message = input<string>('');
    public readonly icon = input<string>('inventory_2');
    public readonly appearance = input<FdUiEmptyStateAppearance>('default');
}

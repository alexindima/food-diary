import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type FdUiStatusBadgeTone = 'muted' | 'success' | 'warning' | 'danger';

@Component({
    selector: 'fd-ui-status-badge',
    standalone: true,
    templateUrl: './fd-ui-status-badge.component.html',
    styleUrl: './fd-ui-status-badge.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'fd-ui-status-badge',
        '[class.fd-ui-status-badge--muted]': 'tone() === "muted"',
        '[class.fd-ui-status-badge--success]': 'tone() === "success"',
        '[class.fd-ui-status-badge--warning]': 'tone() === "warning"',
        '[class.fd-ui-status-badge--danger]': 'tone() === "danger"',
        role: 'status',
    },
})
export class FdUiStatusBadgeComponent {
    public readonly tone = input<FdUiStatusBadgeTone>('muted');
}

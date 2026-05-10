import { CommonModule } from '@angular/common';
import { booleanAttribute, ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { FdUiIconComponent } from '../icon/fd-ui-icon.component';

export type FdUiInlineAlertSeverity = 'info' | 'warning' | 'success' | 'danger';
export type FdUiInlineAlertAppearance = 'alert' | 'notice';

@Component({
    selector: 'fd-ui-inline-alert',
    standalone: true,
    imports: [CommonModule, FdUiIconComponent],
    templateUrl: './fd-ui-inline-alert.component.html',
    styleUrls: ['./fd-ui-inline-alert.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'fd-ui-inline-alert',
        '[class.fd-ui-inline-alert--alert]': 'appearance() === "alert"',
        '[class.fd-ui-inline-alert--notice]': 'appearance() === "notice"',
        '[class.fd-ui-inline-alert--info]': 'severity() === "info"',
        '[class.fd-ui-inline-alert--warning]': 'severity() === "warning"',
        '[class.fd-ui-inline-alert--success]': 'severity() === "success"',
        '[class.fd-ui-inline-alert--danger]': 'severity() === "danger"',
    },
})
export class FdUiInlineAlertComponent {
    public readonly appearance = input<FdUiInlineAlertAppearance>('alert');
    public readonly severity = input<FdUiInlineAlertSeverity>('info');
    public readonly title = input('');
    public readonly message = input('');
    public readonly primaryActionLabel = input<string | null>(null);
    public readonly secondaryActionLabel = input<string | null>(null);
    public readonly dismissible = input(false, { transform: booleanAttribute });

    public readonly primaryAction = output<void>();
    public readonly secondaryAction = output<void>();
    public readonly dismiss = output<void>();

    public readonly iconName = computed(() => {
        switch (this.severity()) {
            case 'warning':
                return 'warning_amber';
            case 'success':
                return 'task_alt';
            case 'danger':
                return 'error';
            case 'info':
                return 'info';
        }
    });

    public readonly hasActions = computed(() => !!this.primaryActionLabel() || !!this.secondaryActionLabel());
}

import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { DashboardWidgetHeaderComponent } from '../dashboard-widget-header/dashboard-widget-header.component';

@Component({
    selector: 'fd-dashboard-widget-frame',
    imports: [DashboardWidgetHeaderComponent],
    templateUrl: './dashboard-widget-frame.component.html',
    styleUrl: './dashboard-widget-frame.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardWidgetFrameComponent {
    public readonly title = input.required<string>();
    public readonly description = input<string | null>(null);
    public readonly iconName = input<string | null>(null);
    public readonly iconLabel = input<string | null>(null);
    public readonly iconTone = input<'neutral' | 'info' | 'success' | 'energy' | 'accent'>('neutral');
}

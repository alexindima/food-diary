import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { FdUiIconComponent } from 'fd-ui-kit';

@Component({
    selector: 'fd-dashboard-widget-header',
    standalone: true,
    imports: [FdUiIconComponent],
    templateUrl: './dashboard-widget-header.component.html',
    styleUrl: './dashboard-widget-header.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardWidgetHeaderComponent {
    public readonly title = input.required<string>();
    public readonly description = input<string | null>(null);
    public readonly iconName = input<string | null>(null);
    public readonly iconLabel = input<string | null>(null);
    public readonly iconTone = input<'neutral' | 'info' | 'success' | 'energy' | 'accent'>('neutral');

    public readonly hasIcon = computed(() => !!this.iconName() || !!this.iconLabel());
}

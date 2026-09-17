import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { FdUiButtonComponent, FdUiHintDirective,FdUiIconComponent } from 'fd-ui-kit';

@Component({
    selector: 'fd-dashboard-widget-header',
    imports: [FdUiIconComponent, FdUiButtonComponent, FdUiHintDirective],
    templateUrl: './dashboard-widget-header.html',
    styleUrl: './dashboard-widget-header.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardWidgetHeaderComponent {
    public readonly title = input.required<string>();
    public readonly helpText = input<string | null>(null);
    public readonly description = input<string | null>(null);
    public readonly iconName = input<string | null>(null);
    public readonly iconLabel = input<string | null>(null);
    public readonly iconTone = input<'neutral' | 'info' | 'success' | 'energy' | 'accent'>('neutral');

    protected readonly hasIcon = computed(() => {
        const iconName = this.iconName();
        const iconLabel = this.iconLabel();
        return (iconName !== null && iconName.length > 0) || (iconLabel !== null && iconLabel.length > 0);
    });
}

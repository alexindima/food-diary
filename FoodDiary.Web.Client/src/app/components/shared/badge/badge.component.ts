import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { FdUiIconComponent } from 'fd-ui-kit';

type BadgeVariant = 'primary' | 'success' | 'warning' | 'danger' | 'neutral';

@Component({
    selector: 'fd-badge',
    standalone: true,
    imports: [FdUiIconComponent],
    templateUrl: './badge.component.html',
    styleUrls: ['./badge.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BadgeComponent {
    public readonly label = input.required<string>();
    public readonly icon = input<string>();
    public readonly variant = input<BadgeVariant>('neutral');
    protected readonly variantClass = computed(() => `fd-badge--${this.variant()}`);
}

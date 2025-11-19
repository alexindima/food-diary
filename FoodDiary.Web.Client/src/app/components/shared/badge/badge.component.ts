import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NgClass } from '@angular/common';
import { FdUiIconModule } from 'fd-ui-kit/material';

type BadgeVariant = 'primary' | 'success' | 'warning' | 'danger' | 'neutral';

@Component({
    selector: 'fd-badge',
    standalone: true,
    imports: [FdUiIconModule, NgClass],
    templateUrl: './badge.component.html',
    styleUrls: ['./badge.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BadgeComponent {
    public readonly label = input.required<string>();
    public readonly icon = input<string>();
    public readonly variant = input<BadgeVariant>('neutral');

    public get variantClass(): string {
        return `fd-badge--${this.variant()}`;
    }
}

import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
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
    @Input({ required: true }) public label!: string;
    @Input() public icon?: string;
    @Input() public variant: BadgeVariant = 'neutral';

    public get variantClass(): string {
        return `fd-badge--${this.variant}`;
    }
}

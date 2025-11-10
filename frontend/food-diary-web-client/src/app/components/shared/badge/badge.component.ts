import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { NgClass } from '@angular/common';
import { TuiIcon } from '@taiga-ui/core';

type BadgeVariant = 'primary' | 'success' | 'warning' | 'danger' | 'neutral';

@Component({
    selector: 'fd-badge',
    standalone: true,
    imports: [TuiIcon, NgClass],
    templateUrl: './badge.component.html',
    styleUrls: ['./badge.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BadgeComponent {
    @Input({ required: true }) public label!: string;
    @Input() public icon?: string;
    @Input() public variant: BadgeVariant = 'neutral';
}

import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

type FdUiButtonVariant = 'primary' | 'secondary' | 'danger';
type FdUiButtonFill = 'solid' | 'outline' | 'text';

@Component({
    selector: 'fd-ui-button',
    standalone: true,
    imports: [CommonModule, MatIconModule],
    templateUrl: './fd-ui-button.component.html',
    styleUrls: ['./fd-ui-button.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiButtonComponent {
    @Input() public type: 'button' | 'submit' | 'reset' = 'button';
    @Input() public variant: FdUiButtonVariant = 'primary';
    @Input() public fill: FdUiButtonFill = 'solid';
    @Input() public icon?: string;
    @Input() public disabled = false;
    @Input() public fullWidth = false;

    public get classes(): string[] {
        return [
            'fd-ui-button',
            `fd-ui-button--${this.variant}`,
            `fd-ui-button--${this.fill}`,
            this.fullWidth ? 'fd-ui-button--full-width' : '',
        ].filter(Boolean);
    }
}

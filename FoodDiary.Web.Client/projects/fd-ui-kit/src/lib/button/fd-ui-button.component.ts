import { CommonModule } from '@angular/common';
import {
    booleanAttribute,
    ChangeDetectionStrategy,
    Component,
    computed,
    input,
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
    selector: 'fd-ui-button',
    standalone: true,
    imports: [CommonModule, MatIconModule],
    templateUrl: './fd-ui-button.component.html',
    styleUrls: ['./fd-ui-button.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiButtonComponent {
    public readonly type = input<FdUiButtonType>('button');
    public readonly variant = input<FdUiButtonVariant>('primary');
    public readonly fill = input<FdUiButtonFill>('solid');
    public readonly size = input<FdUiButtonSize>('md');
    public readonly icon = input<string | undefined>(undefined);
    public readonly disabled = input(false, { transform: booleanAttribute });
    public readonly fullWidth = input(false, { transform: booleanAttribute });
    public readonly ariaLabel = input<string | undefined>(undefined);

    private readonly normalizedFill = computed<FdUiButtonFill>(() => {
        const variant = this.variant();
        const fill = this.fill();

        if (variant === 'ghost') {
            return 'text';
        }

        if (variant === 'outline') {
            return 'outline';
        }

        return fill === 'ghost' ? 'text' : fill;
    });

    public readonly classes = computed(() =>
        [
            'fd-ui-button',
            `fd-ui-button--${this.variant()}`,
            `fd-ui-button--${this.normalizedFill()}`,
            `fd-ui-button--size-${this.size()}`,
            this.fullWidth() ? 'fd-ui-button--full-width' : '',
        ].filter((className): className is string => Boolean(className)),
    );
}

export type FdUiButtonType = 'button' | 'submit' | 'reset';
export type FdUiButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost' | 'outline';
export type FdUiButtonFill = 'solid' | 'outline' | 'text' | 'ghost';
export type FdUiButtonSize = 'sm' | 'md' | 'lg';

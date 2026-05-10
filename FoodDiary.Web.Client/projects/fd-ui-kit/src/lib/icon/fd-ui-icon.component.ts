import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type FdUiIconSize = 'sm' | 'md' | 'lg' | 'xl';

@Component({
    selector: 'fd-ui-icon',
    standalone: true,
    templateUrl: './fd-ui-icon.component.html',
    styleUrl: './fd-ui-icon.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'fd-ui-icon',
        '[style.--fd-icon-size]': 'resolvedSize()',
        '[attr.aria-hidden]': 'decorative() ? "true" : null',
        '[attr.role]': 'decorative() ? null : "img"',
        '[attr.aria-label]': 'decorative() ? null : (ariaLabel() ?? name())',
    },
})
export class FdUiIconComponent {
    public readonly name = input.required<string>();
    public readonly size = input<FdUiIconSize | number | null>(null);
    public readonly decorative = input(true);
    public readonly ariaLabel = input<string | null>(null);
    public readonly fontSet = input<string>();
    protected readonly glyphClass = computed(() => this.fontSet()?.trim() ?? 'material-icons');

    protected readonly resolvedSize = computed(() => {
        const size = this.size();
        if (size === null) {
            return null;
        }

        if (typeof size === 'number') {
            return `${size}px`;
        }

        switch (size) {
            case 'sm':
                return 'var(--fd-size-icon-sm)';
            case 'lg':
                return 'var(--fd-size-icon-lg)';
            case 'xl':
                return 'var(--fd-size-icon-xl)';
            case 'md':
            default:
                return 'var(--fd-size-icon-md)';
        }
    });
}

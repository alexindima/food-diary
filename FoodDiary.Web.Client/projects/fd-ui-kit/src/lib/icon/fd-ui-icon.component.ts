import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type FdUiIconSize = 'sm' | 'md' | 'lg' | 'xl';

const ICON_SIZE_TOKENS: Record<FdUiIconSize, string> = {
    sm: 'var(--fd-size-icon-sm)',
    md: 'var(--fd-size-icon-md)',
    lg: 'var(--fd-size-icon-lg)',
    xl: 'var(--fd-size-icon-xl)',
};

@Component({
    selector: 'fd-ui-icon',
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

        return ICON_SIZE_TOKENS[size];
    });
}

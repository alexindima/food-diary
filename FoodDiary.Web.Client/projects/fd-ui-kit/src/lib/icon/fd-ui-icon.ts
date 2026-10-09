import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import subsetIconNames from './material-icons-subset.json';

const subsetIcons = new Set<string>(subsetIconNames);

export type FdUiIconSize = 'sm' | 'md' | 'lg' | 'xl';

const ICON_SIZE_TOKENS: Record<FdUiIconSize, string> = {
    sm: 'var(--fd-size-icon-sm)',
    md: 'var(--fd-size-icon-md)',
    lg: 'var(--fd-size-icon-lg)',
    xl: 'var(--fd-size-icon-xl)',
};

@Component({
    selector: 'fd-ui-icon',
    templateUrl: './fd-ui-icon.html',
    styleUrl: './fd-ui-icon.scss',
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
    protected readonly glyphClass = computed(() => {
        const fontSet = this.fontSet();
        if (fontSet !== undefined) {
            return fontSet.trim();
        }
        return subsetIcons.has(this.name()) ? 'fd-material-icons' : 'material-icons';
    });

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

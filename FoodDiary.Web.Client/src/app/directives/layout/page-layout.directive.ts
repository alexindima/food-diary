import { computed, Directive, input } from '@angular/core';

const DEFAULT_BACKGROUND = 'var(--fd-layout-page-background)';
const DEFAULT_PADDING = 'var(--fd-layout-page-vertical-padding) var(--fd-layout-page-horizontal-padding)';

@Directive({
    selector: '[fdLayoutPage]',
    standalone: true,
    host: {
        class: 'fd-layout-page',
        '[style.background]': 'backgroundStyle()',
        '[style.padding]': 'paddingStyle()',
    },
})
export class FdLayoutPageDirective {
    /**
     * Override the page background if needed; falls back to design token.
     */
    public readonly fdLayoutPageBackground = input<string | undefined | null>();

    /**
     * Allow custom padding while keeping layout token defaults.
     */
    public readonly fdLayoutPagePadding = input<string | undefined | null>();

    protected readonly backgroundStyle = computed(() => this.fdLayoutPageBackground() ?? DEFAULT_BACKGROUND);
    protected readonly paddingStyle = computed(() => this.fdLayoutPagePadding() ?? DEFAULT_PADDING);
}

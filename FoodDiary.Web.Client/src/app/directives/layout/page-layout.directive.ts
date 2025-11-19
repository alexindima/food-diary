import { Directive, HostBinding, Input } from '@angular/core';

const DEFAULT_BACKGROUND = 'var(--fd-layout-page-background)';
const DEFAULT_PADDING =
    'var(--fd-layout-page-vertical-padding) var(--fd-layout-page-horizontal-padding)';

@Directive({
    selector: '[fdLayoutPage]',
    standalone: true,
})
export class FdLayoutPageDirective {
    @HostBinding('class.fd-layout-page')
    protected readonly layoutClass = true;

    @HostBinding('style.background')
    protected backgroundStyle = DEFAULT_BACKGROUND;

    @HostBinding('style.padding')
    protected paddingStyle = DEFAULT_PADDING;

    /**
     * Override the page background if needed; falls back to design token.
     */
    @Input()
    public set fdLayoutPageBackground(value: string | undefined | null) {
        this.backgroundStyle = value ?? DEFAULT_BACKGROUND;
    }

    /**
     * Allow custom padding while keeping layout token defaults.
     */
    @Input()
    public set fdLayoutPagePadding(value: string | undefined | null) {
        this.paddingStyle = value ?? DEFAULT_PADDING;
    }
}

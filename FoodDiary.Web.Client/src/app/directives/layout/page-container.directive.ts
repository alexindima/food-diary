import { Directive, HostBinding } from '@angular/core';

@Directive({
    selector: '[fdPageContainer]',
    standalone: true,
})
export class FdPageContainerDirective {
    @HostBinding('class.fd-page-container')
    protected readonly hostClass = true;

    @HostBinding('style.display')
    protected readonly display = 'flex';

    @HostBinding('style.flex-direction')
    protected readonly direction = 'column';

    @HostBinding('style.flex')
    protected readonly flex = '1 1 auto';

    @HostBinding('style.min-height')
    protected readonly minHeight = '100%';

    @HostBinding('style.gap')
    protected readonly gap = 'var(--fd-page-body-gap, 16px)';

    @HostBinding('style.width')
    protected readonly width = '100%';

    @HostBinding('style.max-width')
    protected readonly maxWidth = 'var(--fd-layout-page-content-max-width)';

    @HostBinding('style.margin')
    protected readonly margin = '0 auto';

    @HostBinding('style.padding')
    protected readonly padding =
        'var(--fd-layout-page-top-padding, 10px) var(--fd-layout-page-horizontal-padding) var(--fd-layout-page-vertical-padding)';

}

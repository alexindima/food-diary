import { Directive } from '@angular/core';

@Directive({
    selector: '[fdPageContainer]',
    host: {
        class: 'fd-page-container',
        '[style.display]': '"flex"',
        '[style.flex-direction]': '"column"',
        '[style.flex]': '"1 1 auto"',
        '[style.min-height]': '"100%"',
        '[style.gap]': '"var(--fd-page-body-gap)"',
        '[style.width]': '"100%"',
        '[style.max-width]': '"var(--fd-layout-page-content-max-width)"',
        '[style.margin]': '"0 auto"',
        '[style.padding]':
            '"var(--fd-layout-page-top-padding) var(--fd-layout-page-horizontal-padding) var(--fd-layout-page-vertical-padding)"',
    },
})
export class FdPageContainerDirective {}

import { Directive, input } from '@angular/core';

@Directive({
    selector: '[fdPageContainer]',
    host: {
        class: 'fd-page-container',
        '[style.display]': '"flex"',
        '[style.flex-direction]': '"column"',
        '[style.flex]': '"1 1 auto"',
        '[style.min-height]': 'fullHeight() ? "100%" : "auto"',
        '[style.gap]': '"var(--fd-page-body-gap)"',
        '[style.width]': '"100%"',
        '[style.max-width]': '"var(--fd-layout-page-content-max-width)"',
        '[style.margin]': '"0 auto"',
        '[style.padding]': '"var(--fd-page-container-padding)"',
    },
})
export class FdPageContainerDirective {
    public readonly fullHeight = input(true);
}

import { Directive, TemplateRef } from '@angular/core';

@Directive({
    selector: '[fdUiEntityCardHeader]',
    standalone: true,
})
export class FdUiEntityCardHeaderDirective {
    public constructor(public readonly templateRef: TemplateRef<unknown>) {}
}


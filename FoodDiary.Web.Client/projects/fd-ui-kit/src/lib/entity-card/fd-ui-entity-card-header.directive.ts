import { Directive, TemplateRef, inject } from '@angular/core';

@Directive({
    selector: '[fdUiEntityCardHeader]',
    standalone: true,
})
export class FdUiEntityCardHeaderDirective {
    readonly templateRef = inject<TemplateRef<unknown>>(TemplateRef);
}


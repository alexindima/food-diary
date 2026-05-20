import { Directive, inject, TemplateRef } from '@angular/core';

@Directive({
    selector: '[fdUiEntityCardHeader]',
})
export class FdUiEntityCardHeaderDirective {
    public readonly templateRef = inject<TemplateRef<unknown>>(TemplateRef);
}

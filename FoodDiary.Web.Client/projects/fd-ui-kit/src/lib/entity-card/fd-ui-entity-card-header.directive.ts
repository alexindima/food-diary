import { Directive, inject, TemplateRef } from '@angular/core';

@Directive({
    selector: '[fdUiEntityCardHeader]',
    standalone: true,
})
export class FdUiEntityCardHeaderDirective {
    public readonly templateRef = inject<TemplateRef<unknown>>(TemplateRef);
}

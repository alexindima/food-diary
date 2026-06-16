import { Directive, input } from '@angular/core';

@Directive({
    selector: '[fdTourTarget]',
    host: {
        '[attr.data-tour-id]': 'fdTourTarget()',
    },
})
export class FdTourTargetDirective {
    public readonly fdTourTarget = input.required<string>();
}

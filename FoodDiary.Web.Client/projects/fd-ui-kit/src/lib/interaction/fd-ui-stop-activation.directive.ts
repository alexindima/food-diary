import { booleanAttribute, Directive, input } from '@angular/core';

/** Isolates a nested action without cancelling the native control's default action. */
@Directive({
    selector: '[fdUiStopActivation]',
    host: {
        '(click)': 'stopClick($event)',
        '(keydown.enter)': '$event.stopPropagation()',
        '(keydown.space)': '$event.stopPropagation()',
    },
})
export class FdUiStopActivationDirective {
    public readonly fdUiStopActivationKeyboardOnly = input(false, { transform: booleanAttribute });

    protected stopClick(event: Event): void {
        if (!this.fdUiStopActivationKeyboardOnly()) {
            event.stopPropagation();
        }
    }
}

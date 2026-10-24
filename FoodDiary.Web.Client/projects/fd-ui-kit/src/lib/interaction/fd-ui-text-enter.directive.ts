import { Directive, output } from '@angular/core';

const IME_KEY_CODE = 229;

/** keyCode covers composition boundary events whose isComposing flag is already false. */
export function fdUiIsComposing(event: KeyboardEvent): boolean {
    // eslint-disable-next-line @typescript-eslint/no-deprecated -- Documented IME boundary fallback: https://developer.mozilla.org/en-US/docs/Web/API/Element/keydown_event#keydown_events_with_ime
    return event.isComposing || event.keyCode === IME_KEY_CODE;
}

/** Emits only intentional text submission; composing keys retain the browser's default behavior. */
@Directive({
    selector: '[fdUiTextEnter]',
    host: { '(keydown)': 'onKeydown($event)' },
})
export class FdUiTextEnterDirective {
    public readonly fdUiTextEnter = output<KeyboardEvent>();

    protected onKeydown(event: KeyboardEvent): void {
        if (event.key !== 'Enter' || fdUiIsComposing(event)) {
            return;
        }
        event.preventDefault();
        this.fdUiTextEnter.emit(event);
    }
}

import { ElementRef, inject } from '@angular/core';

/** Lets Signal Forms focus the actual control inside a composite field. */
export function injectFdUiControlFocus(): () => void {
    const host = inject<ElementRef<HTMLElement>>(ElementRef);
    return (): void => {
        host.nativeElement
            .querySelector<HTMLElement>('input:not(:disabled), textarea:not(:disabled), button:not(:disabled), [tabindex="0"]')
            ?.focus();
    };
}

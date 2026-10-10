import { booleanAttribute, Directive, ElementRef, inject, input, output } from '@angular/core';

const INTERACTIVE_SELECTOR =
    'button, a[href], input, select, textarea, summary, [role="button"], [role="link"], [contenteditable]:not([contenteditable="false"])';

/** Adds activation to non-native controls. Keep native buttons and links native. */
@Directive({
    selector: '[fdUiActivate]',
    host: {
        '[attr.role]': 'fdUiActivateRole()',
        '[attr.tabindex]': 'fdUiActivateDisabled() ? -1 : 0',
        '[attr.aria-disabled]': 'fdUiActivateDisabled() ? true : null',
        '(click)': 'activateFromClick($event)',
        '(keydown)': 'activateFromKeyboard($event)',
    },
})
export class FdUiActivateDirective {
    public readonly fdUiActivateRole = input<string | null>('button');
    public readonly fdUiActivateDisabled = input(false, { transform: booleanAttribute });
    public readonly fdUiActivated = output<Event>();

    private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);

    protected activateFromClick(event: MouseEvent): void {
        if (event.button !== 0 || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey || this.isNestedControl(event)) {
            return;
        }
        this.activate(event);
    }

    protected activateFromKeyboard(event: KeyboardEvent): void {
        if (event.key !== 'Enter' && event.key !== ' ') {
            return;
        }
        if (
            event.target !== this.element.nativeElement ||
            event.repeat ||
            event.altKey ||
            event.ctrlKey ||
            event.metaKey ||
            event.shiftKey
        ) {
            return;
        }
        if (this.fdUiActivateDisabled()) {
            return;
        }
        event.preventDefault();
        this.activate(event);
    }

    private activate(event: Event): void {
        if (!this.fdUiActivateDisabled()) {
            this.fdUiActivated.emit(event);
        }
    }

    private isNestedControl(event: Event): boolean {
        for (const target of event.composedPath()) {
            if (target === this.element.nativeElement) {
                return false;
            }
            if (target instanceof Element && target.matches(INTERACTIVE_SELECTOR)) {
                return true;
            }
        }
        return false;
    }
}

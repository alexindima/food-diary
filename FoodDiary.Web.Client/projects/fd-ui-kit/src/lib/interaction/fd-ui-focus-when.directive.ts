import { DOCUMENT } from '@angular/common';
import { afterEveryRender, booleanAttribute, Directive, ElementRef, inject, input, output } from '@angular/core';

import { FdUiButtonComponent } from '../button/fd-ui-button';
import { FdUiInputComponent } from '../input/fd-ui-input';
import { FdUiTextareaComponent } from '../textarea/fd-ui-textarea';

@Directive({
    selector: '[fdUiFocusWhen]',
})
export class FdUiFocusWhenDirective {
    public readonly fdUiFocusWhen = input(false, { transform: booleanAttribute });
    public readonly fdUiFocusSelect = input(false, { transform: booleanAttribute });
    public readonly fdUiFocused = output();

    private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly document = inject(DOCUMENT);
    private readonly button = inject(FdUiButtonComponent, { self: true, optional: true });
    private readonly control = inject(FdUiInputComponent, { self: true, optional: true });
    private readonly textarea = inject(FdUiTextareaComponent, { self: true, optional: true });
    private focused = false;

    public constructor() {
        afterEveryRender(() => {
            const host = this.element.nativeElement;
            if (!this.fdUiFocusWhen() || !host.isConnected) {
                this.focused = false;
                return;
            }
            if (this.focused || this.isDisabled()) {
                return;
            }
            this.focusTarget();
            if (this.document.activeElement === host || host.contains(this.document.activeElement)) {
                this.focused = true;
                this.fdUiFocused.emit();
            }
        });
    }

    private isDisabled(): boolean {
        return (
            this.button?.disabled() === true ||
            this.button?.loading() === true ||
            this.control?.disabled() === true ||
            this.textarea?.disabled() === true ||
            this.element.nativeElement.matches(':disabled')
        );
    }

    private focusTarget(): void {
        if (this.button !== null) {
            this.button.focus();
        } else if (this.control !== null) {
            this.control.focus();
        } else if (this.textarea !== null) {
            this.textarea.focus();
        } else {
            const element = this.element.nativeElement;
            element.focus();
            if (
                this.document.activeElement === element &&
                this.fdUiFocusSelect() &&
                (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement)
            ) {
                element.select();
            }
        }
    }
}

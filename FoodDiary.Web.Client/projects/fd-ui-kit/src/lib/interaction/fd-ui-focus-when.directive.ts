import { afterRenderEffect, booleanAttribute, Directive, ElementRef, inject, input, output } from '@angular/core';

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
    private readonly button = inject(FdUiButtonComponent, { self: true, optional: true });
    private readonly control = inject(FdUiInputComponent, { self: true, optional: true });
    private readonly textarea = inject(FdUiTextareaComponent, { self: true, optional: true });
    private focused = false;

    public constructor() {
        afterRenderEffect(() => {
            if (!this.fdUiFocusWhen()) {
                this.focused = false;
                return;
            }
            if (this.focused || this.isDisabled()) {
                return;
            }
            this.focusTarget();
            this.focused = true;
            this.fdUiFocused.emit();
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
            if (this.fdUiFocusSelect() && (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement)) {
                element.select();
            }
        }
    }
}

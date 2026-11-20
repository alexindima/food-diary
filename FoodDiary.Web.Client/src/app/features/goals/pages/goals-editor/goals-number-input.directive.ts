import { afterRenderEffect, Directive, ElementRef, inject, input, output } from '@angular/core';

@Directive({
    selector: 'input[fdGoalsNumber]',
    host: {
        '[attr.value]': 'fdGoalsNumber()',
        '(input)': 'updateNumber()',
    },
})
export class GoalsNumberInputDirective {
    private readonly field = inject<ElementRef<HTMLInputElement>>(ElementRef).nativeElement;
    public readonly fdGoalsNumber = input.required<number>();
    public readonly fdGoalsNumberIncomplete = input.required<boolean>();
    public readonly fdGoalsNumberChange = output<number | null>();

    public constructor() {
        afterRenderEffect({
            write: () => {
                const value = String(this.fdGoalsNumber());
                // Writing a value replaces the browser's incomplete number edit.
                if (!this.fdGoalsNumberIncomplete() && this.field.value !== value) {
                    this.field.value = value;
                }
            },
        });
    }

    protected updateNumber(): void {
        this.fdGoalsNumberChange.emit(this.field.validity.badInput ? null : Number(this.field.value));
    }
}

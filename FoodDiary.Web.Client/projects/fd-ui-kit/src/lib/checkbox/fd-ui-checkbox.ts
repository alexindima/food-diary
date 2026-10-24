import { ChangeDetectionStrategy, Component, computed, type ElementRef, input, model, output, viewChild } from '@angular/core';
import type { FormCheckboxControl, ValidationError } from '@angular/forms/signals';

import { injectFdUiFieldError } from '../form-error/fd-ui-signal-field-error';

let nextId = 0;

@Component({
    selector: 'fd-ui-checkbox',
    templateUrl: './fd-ui-checkbox.html',
    styleUrls: ['./fd-ui-checkbox.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiCheckboxComponent implements FormCheckboxControl {
    private readonly control = viewChild<ElementRef<HTMLInputElement>>('control');
    public readonly id = input(`fd-ui-checkbox-${nextId++}`);
    public readonly label = input('');
    public readonly hint = input<string>();
    public readonly error = input<string | null>();
    public readonly errors = input<readonly ValidationError.WithOptionalFieldTree[]>([]);
    public readonly disabled = input(false);
    public readonly checked = model(false);
    public readonly touched = model(false);
    protected readonly resolvedError = injectFdUiFieldError(this.error, this.errors, this.touched);
    public readonly touch = output();

    protected readonly describedBy = computed(() => {
        if ((this.resolvedError()?.length ?? 0) > 0) {
            return `${this.id()}-error`;
        }

        return (this.hint()?.length ?? 0) > 0 ? `${this.id()}-hint` : null;
    });

    protected updateCheckedValue(event: Event): void {
        if (!(event.target instanceof HTMLInputElement)) {
            return;
        }

        const checkboxInput = event.target;
        this.checked.set(checkboxInput.checked);
    }

    protected touchControl(): void {
        this.touched.set(true);
        this.touch.emit();
    }

    // eslint-disable-next-line local/prefer-protected-template-members -- Public Signal Forms focus contract.
    public focus(): void {
        this.control()?.nativeElement.focus();
    }
}

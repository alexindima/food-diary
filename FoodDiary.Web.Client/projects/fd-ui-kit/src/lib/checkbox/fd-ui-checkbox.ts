import { ChangeDetectionStrategy, Component, computed, effect, input, model, output } from '@angular/core';
import type { FormCheckboxControl } from '@angular/forms/signals';

let nextId = 0;

@Component({
    selector: 'fd-ui-checkbox',
    templateUrl: './fd-ui-checkbox.html',
    styleUrls: ['./fd-ui-checkbox.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiCheckboxComponent implements FormCheckboxControl {
    public readonly id = input(`fd-ui-checkbox-${nextId++}`);
    public readonly label = input('');
    public readonly hint = input<string>();
    public readonly error = input<string | null>();
    public readonly disabled = input(false);
    public readonly checked = model(false);
    public readonly touched = model(false);
    public readonly touch = output();

    protected checkedValue = false;
    protected readonly describedBy = computed(() => {
        if ((this.error()?.length ?? 0) > 0) {
            return `${this.id()}-error`;
        }

        return (this.hint()?.length ?? 0) > 0 ? `${this.id()}-hint` : null;
    });

    public constructor() {
        effect(() => {
            this.checkedValue = this.checked();
        });
    }

    protected updateCheckedValue(event: Event): void {
        if (!(event.target instanceof HTMLInputElement)) {
            return;
        }

        const checkboxInput = event.target;
        this.checkedValue = checkboxInput.checked;
        this.checked.set(checkboxInput.checked);
    }

    protected touchControl(): void {
        this.touched.set(true);
        this.touch.emit();
    }
}

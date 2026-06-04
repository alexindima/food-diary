import { ChangeDetectionStrategy, Component, effect, input, model } from '@angular/core';
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
    public readonly disabled = input(false);
    public readonly checked = model(false);
    public readonly touched = model(false);

    protected checkedValue = false;

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
    }
}

import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

let nextId = 0;

@Component({
    selector: 'fd-ui-checkbox',
    templateUrl: './fd-ui-checkbox.html',
    styleUrls: ['./fd-ui-checkbox.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            multi: true,
            useExisting: FdUiCheckboxComponent,
        },
    ],
})
export class FdUiCheckboxComponent implements ControlValueAccessor {
    public readonly id = input(`fd-ui-checkbox-${nextId++}`);
    public readonly label = input('');
    public readonly hint = input<string>();
    public readonly disabled = model(false);

    protected checked = false;

    private onChange: (value: boolean) => void = () => {};
    private onTouched: () => void = () => {};

    public writeValue(value: boolean | null): void {
        this.checked = value === true;
    }

    public registerOnChange(fn: (value: boolean) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled.set(isDisabled);
    }

    protected updateCheckedValue(event: Event): void {
        if (!(event.target instanceof HTMLInputElement)) {
            return;
        }

        const checkboxInput = event.target;
        this.checked = checkboxInput.checked;
        this.onChange(checkboxInput.checked);
    }

    protected touchControl(): void {
        this.onTouched();
    }
}

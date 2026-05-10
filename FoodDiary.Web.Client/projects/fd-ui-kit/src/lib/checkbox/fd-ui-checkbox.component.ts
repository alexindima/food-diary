import { ChangeDetectionStrategy, Component, forwardRef, input, model, ViewEncapsulation } from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

let nextId = 0;

@Component({
    selector: 'fd-ui-checkbox',
    standalone: true,
    templateUrl: './fd-ui-checkbox.component.html',
    styleUrls: ['./fd-ui-checkbox.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            multi: true,
            useExisting: forwardRef(() => FdUiCheckboxComponent),
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
        this.checked = !!value;
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

    protected handleChange(event: Event): void {
        const checkboxInput = event.target as HTMLInputElement;
        this.checked = checkboxInput.checked;
        this.onChange(checkboxInput.checked);
    }

    protected handleBlur(): void {
        this.onTouched();
    }
}

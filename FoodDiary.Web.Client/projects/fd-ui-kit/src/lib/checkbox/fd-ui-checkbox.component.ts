
import {
    ChangeDetectionStrategy,
    Component,
    forwardRef,
    input, model
} from '@angular/core';
import { MatCheckboxChange, MatCheckboxModule } from '@angular/material/checkbox';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
    selector: 'fd-ui-checkbox',
    standalone: true,
    imports: [MatCheckboxModule],
    templateUrl: './fd-ui-checkbox.component.html',
    styleUrls: ['./fd-ui-checkbox.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            multi: true,
            useExisting: forwardRef(() => FdUiCheckboxComponent),
        },
    ],
})
export class FdUiCheckboxComponent implements ControlValueAccessor {
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

    protected handleChange(event: MatCheckboxChange): void {
        this.checked = event.checked;
        this.onChange(event.checked);
    }

    protected handleBlur(): void {
        this.onTouched();
    }
}

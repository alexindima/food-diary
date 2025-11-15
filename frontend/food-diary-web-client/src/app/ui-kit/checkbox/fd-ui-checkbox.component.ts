import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    forwardRef,
    Input,
} from '@angular/core';
import { MatCheckboxChange, MatCheckboxModule } from '@angular/material/checkbox';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
    selector: 'fd-ui-checkbox',
    standalone: true,
    imports: [CommonModule, MatCheckboxModule],
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
    @Input() public label = '';
    @Input() public hint?: string;
    @Input() public disabled = false;

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
        this.disabled = isDisabled;
    }

    protected handleChange(event: MatCheckboxChange): void {
        this.checked = event.checked;
        this.onChange(event.checked);
    }

    protected handleBlur(): void {
        this.onTouched();
    }
}

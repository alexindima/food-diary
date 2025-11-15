import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    forwardRef,
    Input,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

let uniqueId = 0;

@Component({
    selector: 'fd-ui-textarea',
    standalone: true,
    imports: [CommonModule, MatFormFieldModule, MatInputModule],
    templateUrl: './fd-ui-textarea.component.html',
    styleUrls: ['./fd-ui-textarea.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => FdUiTextareaComponent),
            multi: true,
        },
    ],
})
export class FdUiTextareaComponent implements ControlValueAccessor {
    @Input() public id = `fd-ui-textarea-${uniqueId++}`;
    @Input() public label?: string;
    @Input() public placeholder: string | null = null;
    @Input() public hint?: string;
    @Input() public error?: string | null;
    @Input() public required = false;
    @Input() public rows = 4;
    @Input() public maxlength?: number;
    @Input() public readonly = false;

    protected disabled = false;
    protected internalValue = '';

    private onChange: (value: string) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    public writeValue(value: string | null): void {
        this.internalValue = value ?? '';
    }

    public registerOnChange(fn: (value: string) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }

    protected handleInput(value: string): void {
        if (this.disabled) {
            return;
        }

        this.internalValue = value;
        this.onChange(value);
    }

    protected handleBlur(): void {
        this.onTouched();
    }
}

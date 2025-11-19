import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  forwardRef,
  input
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FdUiFieldSize } from '../types/field-size.type';

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
    public readonly id = input(`fd-ui-textarea-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string | null>(null);
    public readonly hint = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly rows = input(4);
    public readonly maxlength = input<number>();
    public readonly readonly = input(false);
    public readonly size = input<FdUiFieldSize>('md');

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

    protected get sizeClass(): string {
        return `fd-ui-textarea--size-${this.size()}`;
    }
}

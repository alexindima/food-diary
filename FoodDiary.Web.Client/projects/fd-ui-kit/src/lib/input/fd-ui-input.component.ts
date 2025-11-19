import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, forwardRef, ViewEncapsulation, inject, input, output } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldAppearance, MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { FdUiFieldSize } from '../types/field-size.type';

let uniqueId = 0;

@Component({
    selector: 'fd-ui-input',
    standalone: true,
    imports: [CommonModule, MatFormFieldModule, MatInputModule, MatIconModule, MatButtonModule],
    templateUrl: './fd-ui-input.component.html',
    styleUrls: ['./fd-ui-input.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef((): typeof FdUiInputComponent => FdUiInputComponent),
            multi: true,
        },
    ],
})
export class FdUiInputComponent implements ControlValueAccessor {
    private readonly cdr = inject(ChangeDetectorRef);

    public readonly id = input(`fd-ui-input-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly type = input<'text' | 'number' | 'password' | 'email' | 'tel' | 'date' | 'datetime-local' | 'time'>('text');
    public readonly hint = input<string>();
    public readonly error = input<string | null>();
    public readonly prefixIcon = input<string>();
    public readonly suffixIcon = input<string>();
    public readonly clearable = input(false);
    public readonly appearance = input<MatFormFieldAppearance>('outline');
    public readonly floatLabel = input<'auto' | 'always'>('auto');
    public readonly autocomplete = input<string | null>(null);
    public readonly required = input(false);
    public readonly readonly = input(false);
    public readonly maxLength = input<number>();
    public readonly suffixButtonIcon = input<string>();
    public readonly suffixButtonAriaLabel = input<string>();
    public readonly step = input<string | number>();
    public readonly size = input<FdUiFieldSize>('md');
    public readonly hideSubscript = input(false);

    public readonly cleared = output<void>();
    public readonly suffixButtonClicked = output<void>();

    protected isFocused = false;
    protected internalValue = '';
    protected disabled = false;

    private onChange: (value: string) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    protected get sizeClass(): string {
        return `fd-ui-input--size-${this.size()}`;
    }

    public writeValue(value: string | null): void {
        this.internalValue = value ?? '';
        this.cdr.markForCheck();
    }

    public registerOnChange(fn: (value: string) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
        this.cdr.markForCheck();
    }

    protected handleInput(value: string): void {
        if (this.disabled) {
            return;
        }

        this.internalValue = value;
        this.onChange(value);
    }

    protected handleBlur(): void {
        this.isFocused = false;
        this.onTouched();
    }

    protected handleFocus(): void {
        this.isFocused = true;
    }

    protected clearValue(): void {
        if (!this.clearable() || this.disabled) {
            return;
        }

        this.handleInput('');
        // TODO: The 'emit' function requires a mandatory void argument
        this.cleared.emit();
    }

    protected triggerSuffixButton(): void {
        if (this.disabled || !this.suffixButtonIcon()) {
            return;
        }

        // TODO: The 'emit' function requires a mandatory void argument
        this.suffixButtonClicked.emit();
    }
}

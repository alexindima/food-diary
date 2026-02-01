import { ChangeDetectionStrategy, Component, forwardRef, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { FdUiFieldSize } from '../types/field-size.type';

let uniqueId = 0;

@Component({
    selector: 'fd-ui-plain-input',
    standalone: true,
    imports: [CommonModule, MatIconModule],
    templateUrl: './fd-ui-plain-input.component.html',
    styleUrls: ['./fd-ui-plain-input.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef((): typeof FdUiPlainInputComponent => FdUiPlainInputComponent),
            multi: true,
        },
    ],
})
export class FdUiPlainInputComponent implements ControlValueAccessor {
    public readonly id = input(`fd-ui-plain-input-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly type = input<'text' | 'number' | 'password' | 'email' | 'tel' | 'date' | 'datetime-local' | 'time'>('text');
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly readonly = input(false);
    public readonly maxLength = input<number>();
    public readonly suffixButtonIcon = input<string>();
    public readonly suffixButtonAriaLabel = input<string>();
    public readonly step = input<string | number>();
    public readonly size = input<FdUiFieldSize>('md');
    public readonly fillColor = input<string | null>(null);

    public readonly suffixButtonClicked = output<void>();

    protected internalValue: string | number = '';
    protected disabled = false;
    protected isFocused = false;

    private onChange: (value: string) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    protected get sizeClass(): string {
        return `fd-ui-plain-input--size-${this.size()}`;
    }

    public writeValue(value: string | number | null): void {
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

    protected onInput(value: string): void {
        if (this.disabled) {
            return;
        }

        this.internalValue = value;
        this.onChange(value);
    }

    protected onBlur(): void {
        this.isFocused = false;
        this.onTouched();
    }

    protected onFocus(): void {
        this.isFocused = true;
    }

    protected get shouldFloatLabel(): boolean {
        const text = String(this.internalValue ?? '').trim();
        return this.isFocused || text.length > 0;
    }

    protected get shouldShowPlaceholder(): boolean {
        const text = String(this.internalValue ?? '').trim();
        return this.isFocused && text.length === 0;
    }

    protected triggerSuffixButton(): void {
        if (this.disabled || !this.suffixButtonIcon()) {
            return;
        }

        this.suffixButtonClicked.emit();
    }
}

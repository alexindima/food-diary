import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, forwardRef, input } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { FdUiFieldSize } from '../types/field-size.type';
import { FdUiSelectOption } from '../select/fd-ui-select.component';

let uniqueId = 0;

@Component({
    selector: 'fd-ui-plain-select',
    standalone: true,
    imports: [CommonModule, MatIconModule],
    templateUrl: './fd-ui-plain-select.component.html',
    styleUrls: ['./fd-ui-plain-select.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef((): typeof FdUiPlainSelectComponent => FdUiPlainSelectComponent),
            multi: true,
        },
    ],
})
export class FdUiPlainSelectComponent<T = unknown> implements ControlValueAccessor {
    public readonly id = input(`fd-ui-plain-select-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly options = input<FdUiSelectOption<T>[]>([]);
    public readonly size = input<FdUiFieldSize>('md');
    public readonly fillColor = input<string | null>(null);

    protected internalValue: T | null = null;
    protected disabled = false;
    protected isFocused = false;

    private onChange: (value: T | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    protected get sizeClass(): string {
        return `fd-ui-plain-select--size-${this.size()}`;
    }

    protected get selectedIndex(): number {
        return this.options().findIndex(option => Object.is(option.value, this.internalValue));
    }

    protected get shouldFloatLabel(): boolean {
        return this.isFocused || this.selectedIndex >= 0;
    }

    public writeValue(value: T | null): void {
        this.internalValue = value;
    }

    public registerOnChange(fn: (value: T | null) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }

    protected onNativeChange(rawIndex: string): void {
        if (this.disabled) {
            return;
        }

        const optionIndex = Number(rawIndex);
        if (!Number.isInteger(optionIndex) || optionIndex < 0 || optionIndex >= this.options().length) {
            this.internalValue = null;
            this.onChange(null);
            return;
        }

        const value = this.options()[optionIndex]?.value ?? null;
        this.internalValue = value;
        this.onChange(value);
    }

    protected onFocus(): void {
        this.isFocused = true;
    }

    protected onBlur(): void {
        this.isFocused = false;
        this.onTouched();
    }
}

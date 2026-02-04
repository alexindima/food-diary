import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, ViewEncapsulation, forwardRef, input } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { FdUiFieldSize } from '../types/field-size.type';

let uniqueId = 0;

export interface FdUiSelectOption<T = unknown> {
    value: T;
    label: string;
    hint?: string;
}

@Component({
    selector: 'fd-ui-select',
    standalone: true,
    imports: [CommonModule, MatIconModule, MatMenuModule],
    templateUrl: './fd-ui-select.component.html',
    styleUrls: ['./fd-ui-select.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef((): typeof FdUiSelectComponent => FdUiSelectComponent),
            multi: true,
        },
    ],
})
export class FdUiSelectComponent<T = unknown> implements ControlValueAccessor {
    protected readonly isEqual = Object.is;
    public readonly id = input(`fd-ui-select-${uniqueId++}`);
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
    protected isOpen = false;

    private onChange: (value: T | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    protected get sizeClass(): string {
        return `fd-ui-select--size-${this.size()}`;
    }

    protected get selectedIndex(): number {
        return this.options().findIndex(option => this.isEqual(option.value, this.internalValue));
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

    protected onOptionSelect(option: FdUiSelectOption<T>): void {
        if (this.disabled) {
            return;
        }

        this.internalValue = option.value;
        this.onChange(this.internalValue);
        this.onTouched();
    }

    protected onFocus(): void {
        this.isFocused = true;
    }

    protected onBlur(): void {
        this.isFocused = false;
        this.isOpen = false;
        this.onTouched();
    }

    protected onMenuOpened(): void {
        this.isOpen = true;
        this.onFocus();
    }

    protected onMenuClosed(): void {
        this.isOpen = false;
        this.onBlur();
    }

    protected get selectedLabel(): string {
        if (this.selectedIndex < 0) {
            return this.isFocused ? (this.placeholder() ?? '') : '';
        }

        return this.options()[this.selectedIndex]?.label ?? '';
    }

    protected get hasValue(): boolean {
        return this.selectedIndex >= 0;
    }

    protected openMenu(event: MouseEvent, control: HTMLButtonElement): void {
        if (this.disabled) {
            return;
        }

        const target = event.target as HTMLElement | null;
        if (target?.closest('.fd-ui-select__control')) {
            control.focus();
            return;
        }

        control.click();
        control.focus();
    }
}


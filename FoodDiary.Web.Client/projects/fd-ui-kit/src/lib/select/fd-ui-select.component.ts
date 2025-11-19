import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, forwardRef, inject, input } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectChange, MatSelectModule } from '@angular/material/select';
import { FdUiFieldSize } from '../types/field-size.type';

export interface FdUiSelectOption<T = unknown> {
    label: string;
    value: T;
    hint?: string;
}

@Component({
    selector: 'fd-ui-select',
    standalone: true,
    imports: [CommonModule, MatFormFieldModule, MatSelectModule],
    templateUrl: './fd-ui-select.component.html',
    styleUrls: ['./fd-ui-select.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => FdUiSelectComponent),
            multi: true,
        },
    ],
})
export class FdUiSelectComponent<T = unknown> implements ControlValueAccessor {
    private readonly cdr = inject(ChangeDetectorRef);

    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly hint = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly options = input<FdUiSelectOption<T>[]>([]);
    public readonly floatLabel = input<'auto' | 'always'>('auto');
    public readonly size = input<FdUiFieldSize>('md');
    public readonly hideSubscript = input(false);

    protected disabled = false;
    protected internalValue: T | null = null;
    protected readonly onTouchedHandler = () => this.onTouched();

    private onChange: (value: T | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    protected get sizeClass(): string {
        return `fd-ui-select--size-${this.size()}`;
    }

    public writeValue(value: T | null): void {
        this.internalValue = value;
        this.cdr.markForCheck();
    }

    public registerOnChange(fn: (value: T | null) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
        this.cdr.markForCheck();
    }

    protected handleSelectionChange(change: MatSelectChange): void {
        if (this.disabled) {
            return;
        }

        this.internalValue = change.value as T;
        this.onChange(this.internalValue);
        this.onTouched();
    }
}

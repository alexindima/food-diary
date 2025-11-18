import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    forwardRef,
    Input,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectChange, MatSelectModule } from '@angular/material/select';

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
    @Input() public label?: string;
    @Input() public placeholder?: string;
    @Input() public hint?: string;
    @Input() public error?: string | null;
    @Input() public required = false;
    @Input() public options: FdUiSelectOption<T>[] = [];
    @Input() public floatLabel: 'auto' | 'always' = 'auto';

    protected disabled = false;
    protected internalValue: T | null = null;
    protected readonly onTouchedHandler = () => this.onTouched();

    private onChange: (value: T | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    public constructor(private readonly cdr: ChangeDetectorRef) {}

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

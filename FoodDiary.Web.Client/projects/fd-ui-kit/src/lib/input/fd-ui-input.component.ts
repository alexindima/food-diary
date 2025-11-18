import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    EventEmitter,
    forwardRef,
    Input,
    Output,
    ViewEncapsulation,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldAppearance, MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';

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
    @Input() public id = `fd-ui-input-${uniqueId++}`;
    @Input() public label?: string;
    @Input() public placeholder?: string;
    @Input() public type: 'text' | 'number' | 'password' | 'email' | 'tel' | 'date' | 'datetime-local' | 'time' = 'text';
    @Input() public hint?: string;
    @Input() public error?: string | null;
    @Input() public prefixIcon?: string;
    @Input() public suffixIcon?: string;
    @Input() public clearable = false;
    @Input() public appearance: MatFormFieldAppearance = 'outline';
    @Input() public floatLabel: 'auto' | 'always' = 'auto';
    @Input() public autocomplete: string | null = null;
    @Input() public required = false;
    @Input() public readonly = false;
    @Input() public maxLength?: number;
    @Input() public suffixButtonIcon?: string;
    @Input() public suffixButtonAriaLabel?: string;
    @Input() public step?: string | number;

    @Output() public cleared = new EventEmitter<void>();
    @Output() public suffixButtonClicked = new EventEmitter<void>();

    protected isFocused = false;
    protected internalValue = '';
    protected disabled = false;

    private onChange: (value: string) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    public constructor(private readonly cdr: ChangeDetectorRef) {}

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
        if (!this.clearable || this.disabled) {
            return;
        }

        this.handleInput('');
        this.cleared.emit();
    }

    protected triggerSuffixButton(): void {
        if (this.disabled || !this.suffixButtonIcon) {
            return;
        }

        this.suffixButtonClicked.emit();
    }
}

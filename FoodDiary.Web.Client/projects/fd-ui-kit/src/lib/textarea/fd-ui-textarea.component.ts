import { ChangeDetectionStrategy, Component, forwardRef, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { FdUiFieldSize } from '../types/field-size.type';

let uniqueId = 0;

@Component({
    selector: 'fd-ui-textarea',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './fd-ui-textarea.component.html',
    styleUrls: ['./fd-ui-textarea.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef((): typeof FdUiTextareaComponent => FdUiTextareaComponent),
            multi: true,
        },
    ],
})
export class FdUiTextareaComponent implements ControlValueAccessor {
    public readonly id = input(`fd-ui-textarea-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly readonly = input(false);
    public readonly rows = input(4);
    public readonly maxLength = input<number>();
    public readonly size = input<FdUiFieldSize>('md');
    public readonly fillColor = input<string | null>(null);

    protected internalValue = '';
    protected disabled = false;
    protected isFocused = false;

    private onChange: (value: string) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    protected get sizeClass(): string {
        return `fd-ui-textarea--size-${this.size()}`;
    }

    public writeValue(value: string | number | null): void {
        this.internalValue = value === null || value === undefined ? '' : String(value);
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
        return this.isFocused || this.internalValue.trim().length > 0;
    }

    protected get shouldShowPlaceholder(): boolean {
        return this.isFocused && this.internalValue.trim().length === 0;
    }

    protected focusControl(control: HTMLTextAreaElement): void {
        if (this.disabled) {
            return;
        }

        control.focus();
    }
}


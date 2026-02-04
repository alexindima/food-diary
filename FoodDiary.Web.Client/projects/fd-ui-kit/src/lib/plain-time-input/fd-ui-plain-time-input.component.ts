import { ChangeDetectionStrategy, Component, ElementRef, forwardRef, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { FdUiFieldSize } from '../types/field-size.type';

let uniqueId = 0;

@Component({
    selector: 'fd-ui-plain-time-input',
    standalone: true,
    imports: [CommonModule, MatIconModule],
    templateUrl: './fd-ui-plain-time-input.component.html',
    styleUrls: ['./fd-ui-plain-time-input.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef((): typeof FdUiPlainTimeInputComponent => FdUiPlainTimeInputComponent),
            multi: true,
        },
    ],
})
export class FdUiPlainTimeInputComponent implements ControlValueAccessor {
    public readonly id = input(`fd-ui-plain-time-input-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly size = input<FdUiFieldSize>('md');

    protected internalValue = '';
    protected disabled = false;
    protected isFocused = false;

    private readonly host = inject(ElementRef<HTMLElement>);

    private onChange: (value: string | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    protected get sizeClass(): string {
        return `fd-ui-plain-time-input--size-${this.size()}`;
    }

    protected get shouldFloatLabel(): boolean {
        return this.isFocused || this.internalValue.trim().length > 0;
    }

    protected get shouldShowPlaceholder(): boolean {
        return this.isFocused && this.internalValue.trim().length === 0;
    }

    public writeValue(value: string | null): void {
        this.internalValue = value ?? '';
    }

    public registerOnChange(fn: (value: string | null) => void): void {
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

        if (!value) {
            this.internalValue = '';
            this.onChange(null);
            return;
        }

        const parsed = this.parseTime(value);
        if (!parsed) {
            this.internalValue = value;
            return;
        }

        this.internalValue = `${this.padNumber(parsed.hours)}:${this.padNumber(parsed.minutes)}`;
        this.onChange(this.internalValue);
    }

    protected onBlur(): void {
        this.isFocused = false;
        if (this.internalValue) {
            const parsed = this.parseTime(this.internalValue);
            if (parsed) {
                this.internalValue = `${this.padNumber(parsed.hours)}:${this.padNumber(parsed.minutes)}`;
                this.onChange(this.internalValue);
            }
        }
        this.onTouched();
    }

    protected onFocus(): void {
        this.isFocused = true;
    }

    protected focusTime(control: HTMLInputElement): void {
        if (this.disabled) {
            return;
        }
        control.focus();
        control.showPicker?.();
    }

    protected onFocusOut(): void {
        const active = document.activeElement;
        if (active && this.host.nativeElement.contains(active)) {
            return;
        }
        this.isFocused = false;
        this.onTouched();
    }

    private parseTime(value: string): { hours: number; minutes: number } | null {
        const match = value.match(/^(\d{1,2}):?(\d{2})$/);
        if (!match) {
            return null;
        }
        const hours = Number(match[1]);
        const minutes = Number(match[2]);
        if (Number.isNaN(hours) || Number.isNaN(minutes)) {
            return null;
        }
        if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59) {
            return null;
        }
        return { hours, minutes };
    }

    private padNumber(value: number): string {
        return value.toString().padStart(2, '0');
    }
}

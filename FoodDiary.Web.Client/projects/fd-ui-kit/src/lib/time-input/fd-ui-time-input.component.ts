import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, input, signal } from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { FdUiIconComponent } from '../icon/fd-ui-icon.component';
import type { FdUiFieldSize } from '../types/field-size.type';

let uniqueId = 0;

@Component({
    selector: 'fd-ui-time-input',
    standalone: true,
    imports: [CommonModule, FdUiIconComponent],
    templateUrl: './fd-ui-time-input.component.html',
    styleUrls: ['./fd-ui-time-input.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: FdUiTimeInputComponent,
            multi: true,
        },
    ],
})
export class FdUiTimeInputComponent implements ControlValueAccessor {
    public readonly id = input(`fd-ui-time-input-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly size = input<FdUiFieldSize>('md');

    protected readonly internalValue = signal('');
    protected readonly disabled = signal(false);
    protected readonly isFocused = signal(false);

    private readonly host = inject(ElementRef<HTMLElement>);

    private onChange: (value: string | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    protected readonly sizeClass = computed(() => `fd-ui-time-input--size-${this.size()}`);
    protected readonly shouldFloatLabel = computed(() => this.isFocused() || this.internalValue().trim().length > 0);
    protected readonly hostClass = computed(
        () =>
            `fd-ui-time-input ${this.sizeClass()}${this.error() ? ' fd-ui-time-input--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-time-input--floating' : ''}`,
    );
    protected readonly shouldShowPlaceholder = computed(() => this.isFocused() && this.internalValue().trim().length === 0);
    protected readonly placeholderAttribute = computed(() => (this.shouldShowPlaceholder() ? (this.placeholder() ?? 'HH:mm') : null));

    public writeValue(value: string | null): void {
        this.internalValue.set(value ?? '');
    }

    public registerOnChange(fn: (value: string | null) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled.set(isDisabled);
    }

    protected onInput(value: string): void {
        if (this.disabled()) {
            return;
        }

        if (!value) {
            this.internalValue.set('');
            this.onChange(null);
            return;
        }

        const parsed = this.parseTime(value);
        if (!parsed) {
            this.internalValue.set(value);
            return;
        }

        this.internalValue.set(`${this.padNumber(parsed.hours)}:${this.padNumber(parsed.minutes)}`);
        this.onChange(this.internalValue());
    }

    protected onBlur(): void {
        this.isFocused.set(false);
        const internalValue = this.internalValue();
        if (internalValue) {
            const parsed = this.parseTime(internalValue);
            if (parsed) {
                this.internalValue.set(`${this.padNumber(parsed.hours)}:${this.padNumber(parsed.minutes)}`);
                this.onChange(this.internalValue());
            }
        }
        this.onTouched();
    }

    protected onFocus(): void {
        this.isFocused.set(true);
    }

    protected focusTime(control: HTMLInputElement): void {
        if (this.disabled()) {
            return;
        }
        control.focus();
        (control as { showPicker?: () => void }).showPicker?.();
    }

    protected onFocusOut(): void {
        const active = document.activeElement;
        if (active && this.host.nativeElement.contains(active)) {
            return;
        }
        this.isFocused.set(false);
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

import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, input, signal } from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { FdUiIconComponent } from '../icon/fd-ui-icon.component';
import type { FdUiFieldSize } from '../types/field-size.type';

const MIN_TIME_VALUE = 0;
const MAX_HOURS_VALUE = 23;
const MAX_MINUTES_VALUE = 59;
const TIME_MATCH_HOURS_INDEX = 1;
const TIME_MATCH_MINUTES_INDEX = 2;
const PADDED_TIME_PART_LENGTH = 2;

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

    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

    private onChange: (value: string | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    protected readonly sizeClass = computed(() => `fd-ui-time-input--size-${this.size()}`);
    protected readonly shouldFloatLabel = computed(() => this.isFocused() || this.internalValue().trim().length > 0);
    protected readonly hostClass = computed(
        () =>
            `fd-ui-time-input ${this.sizeClass()}${this.error() !== null ? ' fd-ui-time-input--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-time-input--floating' : ''}`,
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

        if (value.length === 0) {
            this.internalValue.set('');
            this.onChange(null);
            return;
        }

        const parsed = this.parseTime(value);
        if (parsed === null) {
            this.internalValue.set(value);
            return;
        }

        this.internalValue.set(`${this.padNumber(parsed.hours)}:${this.padNumber(parsed.minutes)}`);
        this.onChange(this.internalValue());
    }

    protected onBlur(): void {
        this.isFocused.set(false);
        const internalValue = this.internalValue();
        if (internalValue.length > 0) {
            const parsed = this.parseTime(internalValue);
            if (parsed !== null) {
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
        if (active !== null && this.host.nativeElement.contains(active)) {
            return;
        }
        this.isFocused.set(false);
        this.onTouched();
    }

    private parseTime(value: string): { hours: number; minutes: number } | null {
        const match = value.match(/^(\d{1,2}):?(\d{2})$/);
        if (match === null) {
            return null;
        }

        const hours = Number(match[TIME_MATCH_HOURS_INDEX]);
        const minutes = Number(match[TIME_MATCH_MINUTES_INDEX]);
        if (Number.isNaN(hours) || Number.isNaN(minutes)) {
            return null;
        }

        if (hours < MIN_TIME_VALUE || hours > MAX_HOURS_VALUE || minutes < MIN_TIME_VALUE || minutes > MAX_MINUTES_VALUE) {
            return null;
        }

        return { hours, minutes };
    }

    private padNumber(value: number): string {
        return value.toString().padStart(PADDED_TIME_PART_LENGTH, '0');
    }
}

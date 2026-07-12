import { CommonModule, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, ElementRef, inject, input, model, signal } from '@angular/core';
import type { FormValueControl } from '@angular/forms/signals';

import { FdUiIconComponent } from '../icon/fd-ui-icon';
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
    imports: [CommonModule, FdUiIconComponent],
    templateUrl: './fd-ui-time-input.html',
    styleUrls: ['./fd-ui-time-input.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiTimeInputComponent implements FormValueControl<string | null> {
    public readonly id = input(`fd-ui-time-input-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly pickerAriaLabel = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly size = input<FdUiFieldSize>('md');
    public readonly value = model<string | null>(null);
    public readonly touched = model(false);
    public readonly disabled = input(false);

    protected readonly internalValue = signal('');
    protected readonly isFocused = signal(false);

    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly document = inject(DOCUMENT);

    public constructor() {
        effect(() => {
            this.internalValue.set(this.value() ?? '');
        });
    }

    protected readonly sizeClass = computed(() => `fd-ui-time-input--size-${this.size()}`);
    protected readonly hasError = computed(() => {
        const error = this.error();

        return error !== null && error !== undefined && error.trim().length > 0;
    });
    protected readonly shouldFloatLabel = computed(() => this.isFocused() || this.internalValue().trim().length > 0);
    protected readonly hostClass = computed(
        () =>
            `fd-ui-time-input ${this.sizeClass()}${this.hasError() ? ' fd-ui-time-input--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-time-input--floating' : ''}`,
    );
    protected readonly shouldShowPlaceholder = computed(() => this.isFocused() && this.internalValue().trim().length === 0);
    protected readonly placeholderAttribute = computed(() => (this.shouldShowPlaceholder() ? (this.placeholder() ?? 'HH:mm') : null));

    protected onInput(value: string): void {
        if (this.disabled()) {
            return;
        }

        if (value.length === 0) {
            this.internalValue.set('');
            this.value.set(null);
            return;
        }

        const parsed = this.parseTime(value);
        if (parsed === null) {
            this.internalValue.set(value);
            return;
        }

        this.internalValue.set(`${this.padNumber(parsed.hours)}:${this.padNumber(parsed.minutes)}`);
        this.value.set(this.internalValue());
    }

    protected onBlur(): void {
        this.isFocused.set(false);
        const internalValue = this.internalValue();
        if (internalValue.length > 0) {
            const parsed = this.parseTime(internalValue);
            if (parsed !== null) {
                this.internalValue.set(`${this.padNumber(parsed.hours)}:${this.padNumber(parsed.minutes)}`);
                this.value.set(this.internalValue());
            }
        }
        this.touched.set(true);
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
        const active = this.document.activeElement;
        if (active !== null && this.host.nativeElement.contains(active)) {
            return;
        }
        this.isFocused.set(false);
        this.touched.set(true);
    }

    private parseTime(value: string): { hours: number; minutes: number } | null {
        const match = /^(\d{1,2}):?(\d{2})$/.exec(value);
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

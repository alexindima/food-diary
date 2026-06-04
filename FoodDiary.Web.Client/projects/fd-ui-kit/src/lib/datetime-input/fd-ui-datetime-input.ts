import { CommonModule, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, computed, effect, ElementRef, inject, input, signal } from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { disabled as disabledRule, form, FormField } from '@angular/forms/signals';

import { fdUiFormatDateInputValue, fdUiFormatTimeInputValue, fdUiPadDatePart, fdUiParseLocalDateTime } from '../date/fd-ui-date.utils';
import { FdUiDateInputComponent } from '../date-input/fd-ui-date-input';
import { FdUiIconComponent } from '../icon/fd-ui-icon';
import type { FdUiFieldSize } from '../types/field-size.type';

const DEFAULT_TIME_VALUE = '00:00';
const MIN_TIME_VALUE = 0;
const MAX_HOURS_VALUE = 23;
const MAX_MINUTES_VALUE = 59;
const TIME_INDEX_HOURS = 1;
const TIME_INDEX_MINUTES = 2;

let uniqueId = 0;

@Component({
    selector: 'fd-ui-datetime-input',
    imports: [CommonModule, FormField, FdUiDateInputComponent, FdUiIconComponent],
    templateUrl: './fd-ui-datetime-input.html',
    styleUrls: ['./fd-ui-datetime-input.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: FdUiDatetimeInputComponent,
            multi: true,
        },
    ],
})
export class FdUiDatetimeInputComponent implements ControlValueAccessor {
    public readonly id = input(`fd-ui-datetime-input-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly size = input<FdUiFieldSize>('md');

    protected readonly dateModel = signal<string | null>(null);
    protected readonly dateForm = form(this.dateModel, path => {
        disabledRule(path, { when: () => this.disabled() });
    });
    protected readonly timeValue = signal('');
    protected readonly disabled = signal(false);
    protected readonly isFocused = signal(false);
    protected readonly sizeClass = computed(() => `fd-ui-datetime-input--size-${this.size()}`);
    protected readonly hasError = computed(() => {
        const error = this.error();

        return error !== null && error !== undefined && error.trim().length > 0;
    });
    protected readonly shouldFloatLabel = computed(
        () => this.isFocused() || this.dateModel() !== null || this.timeValue().trim().length > 0,
    );
    protected readonly hostClass = computed(
        () =>
            `fd-ui-datetime-input ${this.sizeClass()}${this.hasError() ? ' fd-ui-datetime-input--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-datetime-input--floating' : ''}`,
    );

    private readonly cdr = inject(ChangeDetectorRef);
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly document = inject(DOCUMENT);

    private onChange: (value: string | null) => void = () => {};
    private onTouched: () => void = () => {};
    private lastValidTime = DEFAULT_TIME_VALUE;
    private skippedModelUpdate: { value: string | null } | null = null;

    public constructor() {
        effect(() => {
            const value = this.dateModel();
            if (value === this.skippedModelUpdate?.value) {
                this.skippedModelUpdate = null;
                return;
            }

            this.emitValue();
        });
    }

    public writeValue(value: string | Date | null): void {
        if (value === null || value === '') {
            this.setDateValue(null);
            this.timeValue.set('');
            this.lastValidTime = DEFAULT_TIME_VALUE;
            this.cdr.markForCheck();
            return;
        }

        const parsed = fdUiParseLocalDateTime(value);
        if (parsed === null) {
            this.setDateValue(null);
            this.timeValue.set('');
            this.cdr.markForCheck();
            return;
        }

        const isoDate = fdUiFormatDateInputValue(parsed);

        this.setDateValue(isoDate);
        this.timeValue.set(fdUiFormatTimeInputValue(parsed));
        this.lastValidTime = this.timeValue();
        this.cdr.markForCheck();
    }

    public registerOnChange(fn: (value: string | null) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled.set(isDisabled);
        this.cdr.markForCheck();
    }

    protected onTimeInput(value: string): void {
        if (this.disabled()) {
            return;
        }

        const parsed = this.parseTime(value);
        if (parsed === null) {
            this.timeValue.set(value);
            return;
        }

        this.timeValue.set(`${fdUiPadDatePart(parsed.hours)}:${fdUiPadDatePart(parsed.minutes)}`);
        this.lastValidTime = this.timeValue();
        this.emitValue();
    }

    protected onTimeBlur(): void {
        const timeValue = this.timeValue();
        if (timeValue.length === 0) {
            this.lastValidTime = DEFAULT_TIME_VALUE;
            this.timeValue.set(this.lastValidTime);
            this.emitValue();
        } else {
            const parsed = this.parseTime(timeValue);
            if (parsed !== null) {
                this.timeValue.set(`${fdUiPadDatePart(parsed.hours)}:${fdUiPadDatePart(parsed.minutes)}`);
                this.lastValidTime = this.timeValue();
                this.emitValue();
            }
        }

        this.onTouched();
    }

    protected focusTimeInput(timeInput: HTMLInputElement): void {
        if (this.disabled()) {
            return;
        }

        timeInput.focus();
        (timeInput as { showPicker?: () => void }).showPicker?.();
    }

    protected onFocusIn(): void {
        this.isFocused.set(true);
    }

    protected onFocusOut(): void {
        const active = this.document.activeElement;
        if (active !== null && this.host.nativeElement.contains(active)) {
            return;
        }

        this.isFocused.set(false);
        this.onTouched();
    }

    private emitValue(): void {
        const date = this.dateModel();
        if (date === null || date.length === 0) {
            this.onChange(null);
            return;
        }

        const time = this.parseTime(this.timeValue()) ??
            this.parseTime(this.lastValidTime) ?? { hours: MIN_TIME_VALUE, minutes: MIN_TIME_VALUE };
        this.onChange(`${date}T${fdUiPadDatePart(time.hours)}:${fdUiPadDatePart(time.minutes)}`);
    }

    private parseTime(value: string): { hours: number; minutes: number } | null {
        const match = /^(\d{1,2}):?(\d{2})$/.exec(value);
        if (match === null) {
            return null;
        }

        const hours = Number(match[TIME_INDEX_HOURS]);
        const minutes = Number(match[TIME_INDEX_MINUTES]);
        if (Number.isNaN(hours) || Number.isNaN(minutes)) {
            return null;
        }

        if (hours < MIN_TIME_VALUE || hours > MAX_HOURS_VALUE || minutes < MIN_TIME_VALUE || minutes > MAX_MINUTES_VALUE) {
            return null;
        }

        return { hours, minutes };
    }

    private setDateValue(value: string | null): void {
        this.skippedModelUpdate = { value };
        this.dateModel.set(value);
    }
}

import { CommonModule, DOCUMENT } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    computed,
    ElementRef,
    inject,
    input,
    linkedSignal,
    model,
    output,
    signal,
} from '@angular/core';
import type { FormValueControl, ValidationError } from '@angular/forms/signals';

import {
    fdUiFormatDateInputValue,
    fdUiFormatTimeInputValue,
    fdUiPadDatePart,
    fdUiParseLocalDate,
    fdUiParseLocalDateTime,
} from '../date/fd-ui-date.utils';
import { FdUiDateInputComponent } from '../date-input/fd-ui-date-input';
import { injectFdUiControlFocus } from '../form/fd-ui-control-focus';
import { injectFdUiFieldError } from '../form-error/fd-ui-signal-field-error';
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
    imports: [CommonModule, FdUiDateInputComponent, FdUiIconComponent],
    templateUrl: './fd-ui-datetime-input.html',
    styleUrls: ['./fd-ui-datetime-input.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiDatetimeInputComponent implements FormValueControl<string | Date | null> {
    // eslint-disable-next-line local/prefer-protected-template-members -- Public Signal Forms focus contract.
    public readonly focus = injectFdUiControlFocus();
    public readonly id = input(`fd-ui-datetime-input-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly errors = input<readonly ValidationError.WithOptionalFieldTree[]>([]);
    public readonly required = input(false);
    public readonly size = input<FdUiFieldSize>('md');
    public readonly value = model<string | Date | null>(null);
    public readonly touched = model(false);
    protected readonly resolvedError = injectFdUiFieldError(this.error, this.errors, this.touched);
    public readonly touch = output();
    public readonly disabled = input(false);

    private readonly parsedValue = computed(() => fdUiParseLocalDateTime(this.value()));
    protected readonly dateModel = computed(() => {
        const parsed = this.parsedValue();
        return parsed === null ? null : fdUiFormatDateInputValue(parsed);
    });
    protected readonly timeValue = linkedSignal({
        source: this.value,
        computation: value => {
            const parsed = fdUiParseLocalDateTime(value);
            return parsed === null ? '' : fdUiFormatTimeInputValue(parsed);
        },
    });
    private readonly lastValidTime = linkedSignal<string | Date | null, string>({
        source: this.value,
        computation: (value, previous) => {
            if (value === null || value === '') {
                return DEFAULT_TIME_VALUE;
            }
            const parsed = fdUiParseLocalDateTime(value);
            return parsed === null ? (previous?.value ?? DEFAULT_TIME_VALUE) : fdUiFormatTimeInputValue(parsed);
        },
    });
    protected readonly isFocused = signal(false);
    protected readonly sizeClass = computed(() => `fd-ui-datetime-input--size-${this.size()}`);
    protected readonly hasError = computed(() => {
        const error = this.resolvedError();

        return typeof error === 'string' && error.trim().length > 0;
    });
    protected readonly shouldFloatLabel = computed(
        () => this.isFocused() || this.dateModel() !== null || this.timeValue().trim().length > 0,
    );
    protected readonly hostClass = computed(
        () =>
            `fd-ui-datetime-input ${this.sizeClass()}${this.hasError() ? ' fd-ui-datetime-input--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-datetime-input--floating' : ''}`,
    );

    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly document = inject(DOCUMENT);

    public reset(): void {
        const parsed = this.parsedValue();
        const time = parsed === null ? '' : fdUiFormatTimeInputValue(parsed);
        this.timeValue.set(time);
        this.lastValidTime.set(time.length === 0 ? DEFAULT_TIME_VALUE : time);
        this.isFocused.set(false);
        this.touched.set(false);
    }

    protected onDateChange(value: string | Date | null): void {
        if (this.disabled()) {
            return;
        }

        const parsed = fdUiParseLocalDate(value);
        this.emitValue(parsed === null ? null : fdUiFormatDateInputValue(parsed));
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
        this.lastValidTime.set(this.timeValue());
        this.emitValue();
    }

    protected onTimeBlur(): void {
        const timeValue = this.timeValue();
        if (timeValue.length === 0) {
            this.lastValidTime.set(DEFAULT_TIME_VALUE);
            this.timeValue.set(this.lastValidTime());
            this.emitValue();
        } else {
            const parsed = this.parseTime(timeValue);
            if (parsed !== null) {
                this.timeValue.set(`${fdUiPadDatePart(parsed.hours)}:${fdUiPadDatePart(parsed.minutes)}`);
                this.lastValidTime.set(this.timeValue());
                this.emitValue();
            }
        }

        this.touched.set(true);

        this.touch.emit();
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
        this.touched.set(true);
        this.touch.emit();
    }

    private emitValue(date: string | null = this.dateModel()): void {
        if (date === null || date.length === 0) {
            this.value.set(null);
            return;
        }

        const time = this.parseTime(this.timeValue()) ??
            this.parseTime(this.lastValidTime()) ?? { hours: MIN_TIME_VALUE, minutes: MIN_TIME_VALUE };
        const value = `${date}T${fdUiPadDatePart(time.hours)}:${fdUiPadDatePart(time.minutes)}`;
        this.value.set(value);
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
}

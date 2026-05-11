import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, ElementRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type ControlValueAccessor, FormControl, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';

import { FdUiDateInputComponent } from '../date-input/fd-ui-date-input.component';
import { FdUiIconComponent } from '../icon/fd-ui-icon.component';
import type { FdUiFieldSize } from '../types/field-size.type';

const DEFAULT_TIME_VALUE = '00:00';
const MIN_TIME_VALUE = 0;
const MAX_HOURS_VALUE = 23;
const MAX_MINUTES_VALUE = 59;
const DATE_INDEX_YEAR = 1;
const DATE_INDEX_MONTH = 2;
const DATE_INDEX_DAY = 3;
const DATE_INDEX_HOURS = 4;
const DATE_INDEX_MINUTES = 5;
const NEXT_MONTH_OFFSET = 1;
const PADDED_NUMBER_LENGTH = 2;

let uniqueId = 0;

@Component({
    selector: 'fd-ui-datetime-input',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, FdUiDateInputComponent, FdUiIconComponent],
    templateUrl: './fd-ui-datetime-input.component.html',
    styleUrls: ['./fd-ui-datetime-input.component.scss'],
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

    protected readonly dateControl = new FormControl<string | null>(null);
    protected readonly dateValue = signal<string | null>(null);
    protected readonly timeValue = signal('');
    protected readonly disabled = signal(false);
    protected readonly isFocused = signal(false);
    protected readonly sizeClass = computed(() => `fd-ui-datetime-input--size-${this.size()}`);
    protected readonly shouldFloatLabel = computed(
        () => this.isFocused() || this.dateValue() !== null || this.timeValue().trim().length > 0,
    );
    protected readonly hostClass = computed(
        () =>
            `fd-ui-datetime-input ${this.sizeClass()}${this.error() !== null ? ' fd-ui-datetime-input--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-datetime-input--floating' : ''}`,
    );

    private readonly destroyRef = inject(DestroyRef);
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

    private onChange: (value: string | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;
    private lastValidTime = DEFAULT_TIME_VALUE;

    public constructor() {
        this.dateControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(value => {
            this.dateValue.set(value);
            this.emitValue();
        });
    }

    public writeValue(value: string | Date | null): void {
        if (value === null || value === '') {
            this.dateControl.setValue(null, { emitEvent: false });
            this.dateValue.set(null);
            this.timeValue.set('');
            this.lastValidTime = DEFAULT_TIME_VALUE;
            return;
        }

        const parsed = typeof value === 'string' ? this.parseDateTimeString(value) : value;
        if (parsed === null) {
            this.dateControl.setValue(null, { emitEvent: false });
            this.dateValue.set(null);
            this.timeValue.set('');
            return;
        }

        const isoDate = this.formatDate(parsed);
        const hours = this.padNumber(parsed.getHours());
        const minutes = this.padNumber(parsed.getMinutes());

        this.dateControl.setValue(isoDate, { emitEvent: false });
        this.dateValue.set(isoDate);
        this.timeValue.set(`${hours}:${minutes}`);
        this.lastValidTime = this.timeValue();
    }

    public registerOnChange(fn: (value: string | null) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled.set(isDisabled);
        if (isDisabled) {
            this.dateControl.disable({ emitEvent: false });
        } else {
            this.dateControl.enable({ emitEvent: false });
        }
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

        this.timeValue.set(`${this.padNumber(parsed.hours)}:${this.padNumber(parsed.minutes)}`);
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
                this.timeValue.set(`${this.padNumber(parsed.hours)}:${this.padNumber(parsed.minutes)}`);
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
        const active = document.activeElement;
        if (active !== null && this.host.nativeElement.contains(active)) {
            return;
        }

        this.isFocused.set(false);
        this.onTouched();
    }

    private emitValue(): void {
        const date = this.dateControl.value;
        if (date === null || date.length === 0) {
            this.onChange(null);
            return;
        }

        const time = this.parseTime(this.timeValue()) ??
            this.parseTime(this.lastValidTime) ?? { hours: MIN_TIME_VALUE, minutes: MIN_TIME_VALUE };
        this.onChange(`${date}T${this.padNumber(time.hours)}:${this.padNumber(time.minutes)}`);
    }

    private parseTime(value: string): { hours: number; minutes: number } | null {
        const match = value.match(/^(\d{1,2}):?(\d{2})$/);
        if (match === null) {
            return null;
        }

        const hours = Number(match[DATE_INDEX_YEAR]);
        const minutes = Number(match[DATE_INDEX_MONTH]);
        if (Number.isNaN(hours) || Number.isNaN(minutes)) {
            return null;
        }

        if (hours < MIN_TIME_VALUE || hours > MAX_HOURS_VALUE || minutes < MIN_TIME_VALUE || minutes > MAX_MINUTES_VALUE) {
            return null;
        }

        return { hours, minutes };
    }

    private parseDateTimeString(value: string): Date | null {
        const match = value.match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/);
        if (match === null) {
            const date = new Date(value);
            return Number.isNaN(date.getTime()) ? null : date;
        }

        const year = Number(match[DATE_INDEX_YEAR]);
        const month = Number(match[DATE_INDEX_MONTH]);
        const day = Number(match[DATE_INDEX_DAY]);
        const hours = Number(match[DATE_INDEX_HOURS]);
        const minutes = Number(match[DATE_INDEX_MINUTES]);
        return new Date(year, month - NEXT_MONTH_OFFSET, day, hours, minutes);
    }

    private formatDate(value: Date): string {
        return [value.getFullYear(), this.padNumber(value.getMonth() + NEXT_MONTH_OFFSET), this.padNumber(value.getDate())].join('-');
    }

    private padNumber(value: number): string {
        return value.toString().padStart(PADDED_NUMBER_LENGTH, '0');
    }
}

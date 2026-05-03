import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, forwardRef, inject, input, ViewEncapsulation } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type ControlValueAccessor, FormControl, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';

import { FdUiDateInputComponent } from '../date-input/fd-ui-date-input.component';
import { FdUiIconComponent } from '../icon/fd-ui-icon.component';
import { type FdUiFieldSize } from '../types/field-size.type';

let uniqueId = 0;

@Component({
    selector: 'fd-ui-datetime-input',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, FdUiDateInputComponent, FdUiIconComponent],
    templateUrl: './fd-ui-datetime-input.component.html',
    styleUrls: ['./fd-ui-datetime-input.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef((): typeof FdUiDatetimeInputComponent => FdUiDatetimeInputComponent),
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
    protected timeValue = '';
    protected disabled = false;
    protected isFocused = false;

    private readonly destroyRef = inject(DestroyRef);
    private readonly host = inject(ElementRef<HTMLElement>);

    private onChange: (value: string | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;
    private lastValidTime = '00:00';

    public constructor() {
        this.dateControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.emitValue();
        });
    }

    protected get sizeClass(): string {
        return `fd-ui-datetime-input--size-${this.size()}`;
    }

    protected get shouldFloatLabel(): boolean {
        return this.isFocused || !!this.dateControl.value || this.timeValue.trim().length > 0;
    }

    public writeValue(value: string | Date | null): void {
        if (!value) {
            this.dateControl.setValue(null, { emitEvent: false });
            this.timeValue = '';
            this.lastValidTime = '00:00';
            return;
        }

        const parsed = typeof value === 'string' ? this.parseDateTimeString(value) : value;
        if (!parsed) {
            this.dateControl.setValue(null, { emitEvent: false });
            this.timeValue = '';
            return;
        }

        const isoDate = this.formatDate(parsed);
        const hours = this.padNumber(parsed.getHours());
        const minutes = this.padNumber(parsed.getMinutes());

        this.dateControl.setValue(isoDate, { emitEvent: false });
        this.timeValue = `${hours}:${minutes}`;
        this.lastValidTime = this.timeValue;
    }

    public registerOnChange(fn: (value: string | null) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
        if (isDisabled) {
            this.dateControl.disable({ emitEvent: false });
        } else {
            this.dateControl.enable({ emitEvent: false });
        }
    }

    protected onTimeInput(value: string): void {
        if (this.disabled) {
            return;
        }

        const parsed = this.parseTime(value);
        if (!parsed) {
            this.timeValue = value;
            return;
        }

        this.timeValue = `${this.padNumber(parsed.hours)}:${this.padNumber(parsed.minutes)}`;
        this.lastValidTime = this.timeValue;
        this.emitValue();
    }

    protected onTimeBlur(): void {
        if (!this.timeValue) {
            this.lastValidTime = '00:00';
            this.timeValue = this.lastValidTime;
            this.emitValue();
        } else {
            const parsed = this.parseTime(this.timeValue);
            if (parsed) {
                this.timeValue = `${this.padNumber(parsed.hours)}:${this.padNumber(parsed.minutes)}`;
                this.lastValidTime = this.timeValue;
                this.emitValue();
            }
        }

        this.onTouched();
    }

    protected focusTimeInput(input: HTMLInputElement): void {
        if (this.disabled) {
            return;
        }

        input.focus();
        input.showPicker?.();
    }

    protected onFocusIn(): void {
        this.isFocused = true;
    }

    protected onFocusOut(): void {
        const active = document.activeElement;
        if (active && this.host.nativeElement.contains(active)) {
            return;
        }

        this.isFocused = false;
        this.onTouched();
    }

    private emitValue(): void {
        const date = this.dateControl.value;
        if (!date) {
            this.onChange(null);
            return;
        }

        const time = this.parseTime(this.timeValue) ?? this.parseTime(this.lastValidTime) ?? { hours: 0, minutes: 0 };
        this.onChange(`${date}T${this.padNumber(time.hours)}:${this.padNumber(time.minutes)}`);
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

    private parseDateTimeString(value: string): Date | null {
        const match = value.match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/);
        if (!match) {
            const date = new Date(value);
            return Number.isNaN(date.getTime()) ? null : date;
        }

        const year = Number(match[1]);
        const month = Number(match[2]);
        const day = Number(match[3]);
        const hours = Number(match[4]);
        const minutes = Number(match[5]);
        return new Date(year, month - 1, day, hours, minutes);
    }

    private formatDate(value: Date): string {
        return [value.getFullYear(), this.padNumber(value.getMonth() + 1), this.padNumber(value.getDate())].join('-');
    }

    private padNumber(value: number): string {
        return value.toString().padStart(2, '0');
    }
}

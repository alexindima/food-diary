import { CdkConnectedOverlay, CdkOverlayOrigin } from '@angular/cdk/overlay';
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, input, LOCALE_ID, signal } from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { FdUiCalendarComponent } from '../calendar/fd-ui-calendar.component';
import { FdUiIconComponent } from '../icon/fd-ui-icon.component';
import type { FdUiFieldSize } from '../types/field-size.type';

let uniqueId = 0;

@Component({
    selector: 'fd-ui-date-input',
    standalone: true,
    imports: [CommonModule, CdkOverlayOrigin, CdkConnectedOverlay, FdUiCalendarComponent, FdUiIconComponent],
    templateUrl: './fd-ui-date-input.component.html',
    styleUrls: ['./fd-ui-date-input.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: FdUiDateInputComponent,
            multi: true,
        },
    ],
})
export class FdUiDateInputComponent implements ControlValueAccessor {
    private readonly host = inject(ElementRef<HTMLElement>);
    private readonly locale = inject(LOCALE_ID);

    public readonly id = input(`fd-ui-date-input-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly size = input<FdUiFieldSize>('md');
    public readonly min = input<string | Date | null>(null);
    public readonly max = input<string | Date | null>(null);

    protected readonly value = signal<Date | null>(null);
    protected readonly isOpen = signal(false);
    protected readonly displayMonth = signal(new Date());
    protected readonly disabled = signal(false);
    protected readonly isFocused = signal(false);

    protected readonly sizeClass = computed(() => `fd-ui-date-input--size-${this.size()}`);
    protected readonly shouldFloatLabel = computed(() => this.isFocused() || this.isOpen() || !!this.value());
    protected readonly shouldShowPlaceholder = computed(() => (this.isFocused() || this.isOpen()) && !this.value());
    protected readonly placeholderAttribute = computed(() => (this.shouldShowPlaceholder() ? (this.placeholder() ?? null) : null));
    protected readonly displayValue = computed(() => {
        const value = this.value();
        if (!value) {
            return '';
        }

        return new Intl.DateTimeFormat(this.locale, {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
        }).format(value);
    });

    private onChange: (value: string | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    public writeValue(value: string | Date | null): void {
        const parsed = this.parseDateValue(value);
        this.value.set(parsed);
        this.displayMonth.set(parsed ?? new Date());
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
            this.closeDatePicker();
        }
    }

    protected openDatePicker(): void {
        if (this.disabled()) {
            return;
        }

        this.displayMonth.set(this.value() ?? new Date());
        this.isOpen.set(true);
        this.isFocused.set(true);
    }

    protected closeDatePicker(): void {
        if (!this.isOpen()) {
            return;
        }

        this.isOpen.set(false);
        this.isFocused.set(false);
        this.onTouched();
    }

    protected onDateSelect(value: Date | null): void {
        if (!value) {
            return;
        }

        const normalized = this.stripTime(value);
        this.value.set(normalized);
        this.displayMonth.set(normalized);
        this.onChange(this.formatIsoDate(normalized));
        this.closeDatePicker();
    }

    protected onDisplayMonthChange(value: Date | null): void {
        if (!value) {
            return;
        }

        this.displayMonth.set(value);
    }

    protected onFocusIn(): void {
        this.isFocused.set(true);
    }

    protected onFocusOut(): void {
        const active = document.activeElement;
        if (active && this.host.nativeElement.contains(active)) {
            return;
        }

        if (this.isOpen()) {
            return;
        }

        this.isFocused.set(false);
        this.onTouched();
    }

    protected onInputKeydown(event: KeyboardEvent): void {
        switch (event.key) {
            case 'ArrowDown':
            case 'Enter':
            case ' ':
                event.preventDefault();
                this.openDatePicker();
                break;
            case 'Escape':
                if (this.isOpen()) {
                    event.preventDefault();
                    this.closeDatePicker();
                }
                break;
        }
    }

    protected onOverlayKeydown(event: KeyboardEvent): void {
        if (event.key === 'Escape') {
            event.preventDefault();
            this.closeDatePicker();
        }
    }

    protected readonly minDate = computed(() => this.parseDateValue(this.min()));
    protected readonly maxDate = computed(() => this.parseDateValue(this.max()));

    private parseDateValue(value: string | Date | null | undefined): Date | null {
        if (!value) {
            return null;
        }

        if (value instanceof Date) {
            return Number.isNaN(value.getTime()) ? null : this.stripTime(value);
        }

        const match = value.match(/^(\d{4})-(\d{2})-(\d{2})/);
        if (match) {
            const year = Number(match[1]);
            const month = Number(match[2]);
            const day = Number(match[3]);
            return new Date(year, month - 1, day);
        }

        const parsed = new Date(value);
        return Number.isNaN(parsed.getTime()) ? null : this.stripTime(parsed);
    }

    private stripTime(date: Date): Date {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate());
    }

    private formatIsoDate(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}

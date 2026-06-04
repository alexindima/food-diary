import { CdkConnectedOverlay, CdkOverlayOrigin } from '@angular/cdk/overlay';
import { CommonModule, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, ElementRef, inject, input, LOCALE_ID, model, signal } from '@angular/core';
import type { FormValueControl } from '@angular/forms/signals';

import { FdUiCalendarComponent } from '../calendar/fd-ui-calendar';
import { fdUiFormatDateInputValue, fdUiParseLocalDate, fdUiStartOfLocalDay } from '../date/fd-ui-date.utils';
import { FdUiIconComponent } from '../icon/fd-ui-icon';
import type { FdUiFieldSize } from '../types/field-size.type';

let uniqueId = 0;

@Component({
    selector: 'fd-ui-date-input',
    imports: [CommonModule, CdkOverlayOrigin, CdkConnectedOverlay, FdUiCalendarComponent, FdUiIconComponent],
    templateUrl: './fd-ui-date-input.html',
    styleUrls: ['./fd-ui-date-input.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiDateInputComponent implements FormValueControl<string | Date | null> {
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly locale = inject(LOCALE_ID);
    private readonly document = inject(DOCUMENT);

    public readonly id = input(`fd-ui-date-input-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly size = input<FdUiFieldSize>('md');
    public readonly min = input<string | Date>();
    public readonly max = input<string | Date>();
    public readonly value = model<string | Date | null>(null);
    public readonly touched = model(false);
    public readonly disabled = input(false);

    protected readonly internalValue = signal<Date | null>(null);
    protected readonly isOpen = signal(false);
    protected readonly displayMonth = signal(new Date());
    protected readonly isFocused = signal(false);

    protected readonly sizeClass = computed(() => `fd-ui-date-input--size-${this.size()}`);
    protected readonly hasError = computed(() => {
        const error = this.error();

        return error !== null && error !== undefined && error.trim().length > 0;
    });
    protected readonly shouldFloatLabel = computed(() => this.isFocused() || this.isOpen() || this.internalValue() !== null);
    protected readonly hostClass = computed(
        () =>
            `fd-ui-date-input ${this.sizeClass()}${this.hasError() ? ' fd-ui-date-input--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-date-input--floating' : ''}`,
    );
    protected readonly shouldShowPlaceholder = computed(() => (this.isFocused() || this.isOpen()) && this.internalValue() === null);
    protected readonly placeholderAttribute = computed(() => (this.shouldShowPlaceholder() ? (this.placeholder() ?? null) : null));
    protected readonly displayValue = computed(() => {
        const value = this.internalValue();
        if (value === null) {
            return '';
        }

        return new Intl.DateTimeFormat(this.locale, {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
        }).format(value);
    });

    public constructor() {
        effect(() => {
            this.applyValue(this.value());
        });

        effect(() => {
            if (this.disabled()) {
                this.closeDatePicker();
            }
        });
    }

    protected openDatePicker(): void {
        if (this.disabled()) {
            return;
        }

        this.displayMonth.set(this.internalValue() ?? new Date());
        this.isOpen.set(true);
        this.isFocused.set(true);
    }

    protected closeDatePicker(): void {
        if (!this.isOpen()) {
            return;
        }

        this.isOpen.set(false);
        this.isFocused.set(false);
        this.touched.set(true);
    }

    protected onDateSelect(value: Date | null): void {
        if (value === null) {
            return;
        }

        const normalized = this.stripTime(value);
        this.internalValue.set(normalized);
        this.displayMonth.set(normalized);
        const isoDate = this.formatIsoDate(normalized);
        this.value.set(isoDate);
        this.closeDatePicker();
    }

    protected onDisplayMonthChange(value: Date | null): void {
        if (value === null) {
            return;
        }

        this.displayMonth.set(value);
    }

    protected onFocusIn(): void {
        this.isFocused.set(true);
    }

    protected onFocusOut(): void {
        const active = this.document.activeElement;
        if (active !== null && this.host.nativeElement.contains(active)) {
            return;
        }

        if (this.isOpen()) {
            return;
        }

        this.isFocused.set(false);
        this.touched.set(true);
    }

    protected onInputKeydown(event: KeyboardEvent): void {
        switch (event.key) {
            case 'ArrowDown':
            case 'Enter':
            case ' ': {
                event.preventDefault();
                this.openDatePicker();
                break;
            }
            case 'Escape': {
                if (this.isOpen()) {
                    event.preventDefault();
                    this.closeDatePicker();
                }
                break;
            }
        }
    }

    protected onOverlayKeydown(event: KeyboardEvent): void {
        if (event.key === 'Escape') {
            event.preventDefault();
            this.closeDatePicker();
        }
    }

    protected readonly minDate = computed(() => fdUiParseLocalDate(this.min()));
    protected readonly maxDate = computed(() => fdUiParseLocalDate(this.max()));

    private stripTime(date: Date): Date {
        return fdUiStartOfLocalDay(date);
    }

    private formatIsoDate(date: Date): string {
        return fdUiFormatDateInputValue(date);
    }

    private applyValue(value: string | Date | null): void {
        const parsed = fdUiParseLocalDate(value);
        this.internalValue.set(parsed);
        this.displayMonth.set(parsed ?? new Date());
    }
}

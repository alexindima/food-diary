import { CdkConnectedOverlay, CdkOverlayOrigin } from '@angular/cdk/overlay';
import { CommonModule, DOCUMENT } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    computed,
    effect,
    ElementRef,
    inject,
    input,
    LOCALE_ID,
    model,
    output,
    signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import type { FormValueControl } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { map, of } from 'rxjs';

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

    private readonly translateService = inject(TranslateService, { optional: true });
    protected readonly language = toSignal(this.translateService?.onLangChange.pipe(map(event => event.lang)) ?? of(this.locale), {
        initialValue: this.translateService?.getCurrentLang() ?? this.locale,
    });
    public readonly allowManualInput = input(false);
    public readonly id = input(`fd-ui-date-input-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly clearAriaLabel = input<string>();
    public readonly pickerAriaLabel = input<string>();
    public readonly placeholder = input<string>();
    public readonly todayLabel = input<string>();
    public readonly error = input<string | null>();
    public readonly invalidDateLabel = input<string>();
    private readonly invalidManualDate = signal(false);
    protected readonly fieldError = computed(() => this.error() ?? (this.invalidManualDate() ? this.invalidDateLabel() : null));
    public readonly required = input(false);
    public readonly showRequiredIndicator = input(true);
    public readonly size = input<FdUiFieldSize>('md');
    public readonly min = input<string | Date>();
    public readonly max = input<string | Date>();
    public readonly latestDate = input<string | Date>();
    public readonly earliestDate = input<string | Date>();
    public readonly value = model<string | Date | null>(null);
    public readonly touched = model(false);
    public readonly touch = output();
    public readonly disabled = input(false);

    protected readonly internalValue = signal<Date | null>(null);
    protected readonly isOpen = signal(false);
    protected readonly displayMonth = signal(new Date());
    protected readonly isFocused = signal(false);

    protected readonly sizeClass = computed(() => `fd-ui-date-input--size-${this.size()}`);
    protected readonly hasError = computed(() => {
        const error = this.fieldError();

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

        const todayLabel = this.todayLabel();
        if (todayLabel !== undefined && this.isToday(value)) {
            return todayLabel;
        }

        return new Intl.DateTimeFormat(this.language(), {
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

    protected readonly manualValue = computed(() => {
        const value = this.internalValue();
        return value === null ? '' : fdUiFormatDateInputValue(value);
    });
    protected onManualInput(event: Event): void {
        const target = event.target;
        if (!(target instanceof HTMLInputElement)) {
            return;
        }
        const parsed = fdUiParseLocalDate(target.value);
        const min = this.minDate();
        const max = this.maxDate();
        if (parsed !== null && target.validity.valid && (min === null || parsed >= min) && (max === null || parsed <= max)) {
            this.invalidManualDate.set(false);
            this.onDateSelect(parsed);
        } else {
            this.invalidManualDate.set(target.value !== '' || target.validity.badInput);
            this.value.set(null);
            this.internalValue.set(null);
        }
        this.touched.set(true);
        this.touch.emit();
    }

    protected clearValue(): void {
        if (this.disabled()) {
            return;
        }
        this.invalidManualDate.set(false);
        this.internalValue.set(null);
        this.value.set(null);
        this.touched.set(true);
        this.touch.emit();
        this.closeDatePicker();
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
        this.touch.emit();
    }

    protected onDateSelect(value: Date | null): void {
        if (value === null) {
            return;
        }

        const normalized = this.stripTime(value);
        this.invalidManualDate.set(false);
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
        this.touch.emit();
    }

    protected onInputKeydown(event: KeyboardEvent): void {
        switch (event.key) {
            case 'ArrowDown':
            case 'Enter':
            case ' ': {
                if (this.allowManualInput() && event.key !== 'ArrowDown') {
                    return;
                }
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

    protected readonly minDate = computed(() => fdUiParseLocalDate(this.earliestDate() ?? this.min()));
    protected readonly maxDate = computed(() => fdUiParseLocalDate(this.latestDate() ?? this.max()));

    private stripTime(date: Date): Date {
        return fdUiStartOfLocalDay(date);
    }

    private isToday(date: Date): boolean {
        const today = new Date();
        return date.getFullYear() === today.getFullYear() && date.getMonth() === today.getMonth() && date.getDate() === today.getDate();
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

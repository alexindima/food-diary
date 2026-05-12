import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, input, LOCALE_ID, model, signal } from '@angular/core';

import { FdUiButtonComponent } from '../button/fd-ui-button.component';

const WEEK_DAYS_COUNT = 7;
const CALENDAR_WEEKS_COUNT = 6;
const UTC_WEEKDAY_REFERENCE_YEAR = 2024;
const UTC_WEEKDAY_REFERENCE_MONTH = 0;
const UTC_WEEKDAY_REFERENCE_DAY = 7;
const PREVIOUS_DAY_OFFSET = -1;
const NEXT_DAY_OFFSET = 1;
const PREVIOUS_WEEK_OFFSET = -WEEK_DAYS_COUNT;
const NEXT_WEEK_OFFSET = WEEK_DAYS_COUNT;
const PREVIOUS_MONTH_OFFSET = -1;
const NEXT_MONTH_OFFSET = 1;
const LAST_WEEKDAY_OFFSET = CALENDAR_WEEKS_COUNT;

type FdUiCalendarCell = {
    date: Date;
    iso: string;
    label: string;
    isCurrentMonth: boolean;
    isToday: boolean;
    isSelected: boolean;
    isActive: boolean;
    isDisabled: boolean;
};

@Component({
    selector: 'fd-ui-calendar',
    standalone: true,
    imports: [CommonModule, FdUiButtonComponent],
    templateUrl: './fd-ui-calendar.component.html',
    styleUrl: './fd-ui-calendar.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiCalendarComponent {
    private readonly locale = inject(LOCALE_ID);
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly today = this.stripTime(new Date());
    private readonly activeDate = signal<Date>(this.today);

    public readonly value = model<Date | null>(null);
    public readonly displayMonth = model<Date | null>(null);
    public readonly min = input<Date | null>(null);
    public readonly max = input<Date | null>(null);
    public readonly weekStartsOn = input<0 | 1>(1);

    protected readonly visibleMonth = computed(() => {
        const month = this.displayMonth() ?? this.value() ?? this.activeDate();
        return this.startOfMonth(month);
    });

    protected readonly monthLabel = computed(() => {
        return new Intl.DateTimeFormat(this.locale, {
            month: 'long',
            year: 'numeric',
        }).format(this.visibleMonth());
    });

    protected readonly weekdayLabels = computed(() => {
        const startIndex = this.weekStartsOn();
        return Array.from({ length: WEEK_DAYS_COUNT }, (_, index) => {
            const day = new Date(
                Date.UTC(
                    UTC_WEEKDAY_REFERENCE_YEAR,
                    UTC_WEEKDAY_REFERENCE_MONTH,
                    UTC_WEEKDAY_REFERENCE_DAY + ((startIndex + index) % WEEK_DAYS_COUNT),
                ),
            );
            return new Intl.DateTimeFormat(this.locale, { weekday: 'short', timeZone: 'UTC' }).format(day);
        });
    });

    protected readonly weeks = computed(() => {
        const monthStart = this.visibleMonth();
        const gridStart = this.startOfWeek(monthStart);
        const selectedValue = this.value();
        const selectedIso = selectedValue !== null ? this.toIsoDate(selectedValue) : null;
        const activeIso = this.toIsoDate(this.activeDate());

        return Array.from({ length: CALENDAR_WEEKS_COUNT }, (_week, weekIndex) =>
            Array.from({ length: WEEK_DAYS_COUNT }, (_day, dayIndex) => {
                const cellDate = this.addDays(gridStart, weekIndex * WEEK_DAYS_COUNT + dayIndex);
                const iso = this.toIsoDate(cellDate);

                return {
                    date: cellDate,
                    iso,
                    label: String(cellDate.getDate()),
                    isCurrentMonth: cellDate.getMonth() === monthStart.getMonth(),
                    isToday: iso === this.toIsoDate(this.today),
                    isSelected: iso === selectedIso,
                    isActive: iso === activeIso,
                    isDisabled: this.isOutOfRange(cellDate, this.min(), this.max()),
                } satisfies FdUiCalendarCell;
            }),
        );
    });

    public constructor() {
        this.activeDate.set(this.value() ?? this.displayMonth() ?? this.today);
    }

    protected selectDate(date: Date): void {
        if (this.isOutOfRange(date, this.min(), this.max())) {
            return;
        }

        const normalized = this.stripTime(date);
        this.activeDate.set(normalized);
        this.value.set(normalized);
    }

    protected showPreviousMonth(): void {
        this.changeMonth(-1);
    }

    protected showNextMonth(): void {
        this.changeMonth(1);
    }

    protected onCellKeydown(event: KeyboardEvent, date: Date): void {
        const nextDate = this.getNextDateForKey(event, date);

        if (nextDate === null) {
            return;
        }

        event.preventDefault();
        const normalized = this.clampDate(this.stripTime(nextDate), this.min(), this.max());
        this.activeDate.set(normalized);

        if (normalized.getMonth() !== this.visibleMonth().getMonth() || normalized.getFullYear() !== this.visibleMonth().getFullYear()) {
            this.displayMonth.set(this.startOfMonth(normalized));
        }

        this.focusCell(normalized);
    }

    private getNextDateForKey(event: KeyboardEvent, date: Date): Date | null {
        const handlers = new Map<string, () => Date>([
            ['ArrowLeft', (): Date => this.addDays(date, PREVIOUS_DAY_OFFSET)],
            ['ArrowRight', (): Date => this.addDays(date, NEXT_DAY_OFFSET)],
            ['ArrowUp', (): Date => this.addDays(date, PREVIOUS_WEEK_OFFSET)],
            ['ArrowDown', (): Date => this.addDays(date, NEXT_WEEK_OFFSET)],
            ['Home', (): Date => this.startOfWeek(date)],
            ['End', (): Date => this.addDays(this.startOfWeek(date), LAST_WEEKDAY_OFFSET)],
            ['PageUp', (): Date => this.addMonths(date, PREVIOUS_MONTH_OFFSET)],
            ['PageDown', (): Date => this.addMonths(date, NEXT_MONTH_OFFSET)],
        ]);

        const handler = handlers.get(event.key);
        if (handler !== undefined) {
            return handler();
        }

        if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            this.selectDate(date);
        }

        return null;
    }

    private changeMonth(offset: number): void {
        const target = this.startOfMonth(this.addMonths(this.visibleMonth(), offset));
        this.displayMonth.set(target);
        this.focusCell(this.activeDate());
    }

    private focusCell(date: Date): void {
        queueMicrotask(() => {
            const iso = this.toIsoDate(date);
            const host = this.host.nativeElement;
            const cell = host.querySelector(`[data-date="${iso}"]`);
            if (cell instanceof HTMLElement) {
                cell.focus();
            }
        });
    }

    private startOfWeek(date: Date): Date {
        const normalized = this.stripTime(date);
        const delta = (normalized.getDay() - this.weekStartsOn() + WEEK_DAYS_COUNT) % WEEK_DAYS_COUNT;
        return this.addDays(normalized, -delta);
    }

    private startOfMonth(date: Date): Date {
        return new Date(date.getFullYear(), date.getMonth(), 1);
    }

    private addDays(date: Date, days: number): Date {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
    }

    private addMonths(date: Date, months: number): Date {
        return new Date(date.getFullYear(), date.getMonth() + months, date.getDate());
    }

    private clampDate(date: Date, min: Date | null, max: Date | null): Date {
        if (min !== null && date < this.stripTime(min)) {
            return this.stripTime(min);
        }

        if (max !== null && date > this.stripTime(max)) {
            return this.stripTime(max);
        }

        return date;
    }

    private isOutOfRange(date: Date, min: Date | null, max: Date | null): boolean {
        const normalized = this.stripTime(date);
        if (min !== null && normalized < this.stripTime(min)) {
            return true;
        }

        if (max !== null && normalized > this.stripTime(max)) {
            return true;
        }

        return false;
    }

    private stripTime(date: Date | null): Date {
        if (date === null) {
            return this.today;
        }

        return new Date(date.getFullYear(), date.getMonth(), date.getDate());
    }

    private toIsoDate(date: Date | null): string {
        const normalized = this.stripTime(date);
        const year = normalized.getFullYear();
        const month = String(normalized.getMonth() + NEXT_MONTH_OFFSET).padStart(2, '0');
        const day = String(normalized.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}

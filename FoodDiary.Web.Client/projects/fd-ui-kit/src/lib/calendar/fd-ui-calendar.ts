import { CommonModule } from '@angular/common';
import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    ElementRef,
    inject,
    Injector,
    input,
    LOCALE_ID,
    model,
    signal,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { FdUiButtonComponent } from '../button/fd-ui-button';
import {
    fdUiAddLocalDays,
    fdUiAddLocalMonths,
    fdUiFormatDateInputValue,
    fdUiStartOfLocalDay,
    fdUiStartOfLocalMonth,
} from '../date/fd-ui-date.utils';

export type FdUiCalendarSelectionMode = 'date' | 'week';
export type FdUiCalendarAppearance = 'standalone' | 'embedded';
export type FdUiCalendarMarkerTone = 'brand' | 'danger' | 'warning';

export type FdUiCalendarMarker = {
    date: string;
    tone: FdUiCalendarMarkerTone;
    label: string;
};

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
    ariaLabel: string;
    markers: readonly FdUiCalendarMarker[];
};

@Component({
    selector: 'fd-ui-calendar',
    imports: [CommonModule, FdUiButtonComponent, TranslatePipe],
    templateUrl: './fd-ui-calendar.html',
    styleUrl: './fd-ui-calendar.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiCalendarComponent {
    private readonly defaultLocale = inject(LOCALE_ID);
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly injector = inject(Injector);
    private readonly today = this.stripTime(new Date());
    private readonly activeDate = signal<Date>(this.today);

    public readonly value = model<Date | null>(null);
    public readonly displayMonth = model<Date | null>(null);
    public readonly min = input<Date | null>(null);
    public readonly max = input<Date | null>(null);
    public readonly weekStartsOn = input<0 | 1>(1);
    public readonly selectionMode = input<FdUiCalendarSelectionMode>('date');
    public readonly appearance = input<FdUiCalendarAppearance>('standalone');
    public readonly locale = input<string | null>(null);
    public readonly selectedWeekLabel = input<string | null>(null);
    public readonly markers = input<readonly FdUiCalendarMarker[]>([]);

    protected readonly visibleMonth = computed(() => {
        const month = this.displayMonth() ?? this.value() ?? this.activeDate();
        return this.startOfMonth(month);
    });

    protected readonly monthLabel = computed(() => {
        return new Intl.DateTimeFormat(this.effectiveLocale(), {
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
            return new Intl.DateTimeFormat(this.effectiveLocale(), { weekday: 'short', timeZone: 'UTC' }).format(day);
        });
    });

    private readonly effectiveLocale = computed(() => this.locale() ?? this.defaultLocale);

    protected readonly weeks = computed(() => {
        const monthStart = this.visibleMonth();
        const gridStart = this.startOfWeek(monthStart);
        const selectedValue = this.value();
        const selectedIso = selectedValue !== null ? this.toIsoDate(selectedValue) : null;
        const selectedWeekStart = selectedValue !== null ? this.startOfWeek(selectedValue) : null;
        const activeIso = this.toIsoDate(this.activeDate());

        return Array.from({ length: CALENDAR_WEEKS_COUNT }, (_week, weekIndex) =>
            Array.from({ length: WEEK_DAYS_COUNT }, (_day, dayIndex) => {
                const cellDate = this.addDays(gridStart, weekIndex * WEEK_DAYS_COUNT + dayIndex);
                const iso = this.toIsoDate(cellDate);
                const markers = this.uniqueMarkersForDate(iso);
                const dateLabel = new Intl.DateTimeFormat(this.effectiveLocale(), {
                    day: 'numeric',
                    month: 'long',
                    year: 'numeric',
                }).format(cellDate);

                return {
                    date: cellDate,
                    iso,
                    label: String(cellDate.getDate()),
                    isCurrentMonth: cellDate.getMonth() === monthStart.getMonth(),
                    isToday: iso === this.toIsoDate(this.today),
                    isSelected:
                        this.selectionMode() === 'week'
                            ? selectedWeekStart !== null && this.isSameWeek(cellDate, selectedWeekStart)
                            : iso === selectedIso,
                    isActive: iso === activeIso,
                    isDisabled: this.isSelectionOutOfRange(cellDate),
                    ariaLabel: [dateLabel, ...markers.map(marker => marker.label)].join('. '),
                    markers,
                } satisfies FdUiCalendarCell;
            }),
        );
    });

    public constructor() {
        this.activeDate.set(this.value() ?? this.displayMonth() ?? this.today);
    }

    protected selectDate(date: Date): void {
        if (this.isSelectionOutOfRange(date)) {
            return;
        }

        const normalized = this.selectionMode() === 'week' ? this.startOfWeek(date) : this.stripTime(date);
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
        afterNextRender(
            () => {
                const iso = this.toIsoDate(date);
                const host = this.host.nativeElement;
                const cell = host.querySelector(`[data-date="${iso}"]`);
                if (cell instanceof HTMLElement) {
                    cell.focus();
                }
            },
            { injector: this.injector },
        );
    }

    private startOfWeek(date: Date): Date {
        const normalized = this.stripTime(date);
        const delta = (normalized.getDay() - this.weekStartsOn() + WEEK_DAYS_COUNT) % WEEK_DAYS_COUNT;
        return this.addDays(normalized, -delta);
    }

    private uniqueMarkersForDate(iso: string): readonly FdUiCalendarMarker[] {
        const uniqueMarkers = new Map<string, FdUiCalendarMarker>();
        for (const marker of this.markers().filter(item => item.date === iso)) {
            uniqueMarkers.set(`${marker.tone}:${marker.label}`, marker);
        }
        return [...uniqueMarkers.values()];
    }

    private isSameWeek(date: Date, weekStart: Date): boolean {
        return this.toIsoDate(this.startOfWeek(date)) === this.toIsoDate(weekStart);
    }

    private isSelectionOutOfRange(date: Date): boolean {
        const comparableDate = this.selectionMode() === 'week' ? this.startOfWeek(date) : date;
        const minimum = this.min();
        const maximum = this.max();
        const comparableMinimum = minimum !== null && this.selectionMode() === 'week' ? this.startOfWeek(minimum) : minimum;
        const comparableMaximum = maximum !== null && this.selectionMode() === 'week' ? this.startOfWeek(maximum) : maximum;
        return this.isOutOfRange(comparableDate, comparableMinimum, comparableMaximum);
    }

    private startOfMonth(date: Date): Date {
        return fdUiStartOfLocalMonth(date);
    }

    private addDays(date: Date, days: number): Date {
        return fdUiAddLocalDays(date, days);
    }

    private addMonths(date: Date, months: number): Date {
        return fdUiAddLocalMonths(date, months);
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

        return fdUiStartOfLocalDay(date);
    }

    private toIsoDate(date: Date | null): string {
        return fdUiFormatDateInputValue(this.stripTime(date));
    }
}

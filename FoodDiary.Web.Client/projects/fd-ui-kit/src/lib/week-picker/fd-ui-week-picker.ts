import { CdkConnectedOverlay, CdkOverlayOrigin } from '@angular/cdk/overlay';
import { ChangeDetectionStrategy, Component, computed, inject, input, LOCALE_ID, model, signal } from '@angular/core';

import { FdUiButtonComponent } from '../button/fd-ui-button';
import { FdUiCalendarComponent } from '../calendar/fd-ui-calendar';
import { fdUiAddLocalDays, fdUiStartOfLocalDay, fdUiStartOfLocalWeek } from '../date/fd-ui-date.utils';
import { FdUiIconComponent } from '../icon/fd-ui-icon';

const WEEK_LAST_DAY_OFFSET = 6;
const PREVIOUS_WEEK_OFFSET = -7;
const NEXT_WEEK_OFFSET = 7;

@Component({
    selector: 'fd-ui-week-picker',
    imports: [CdkOverlayOrigin, CdkConnectedOverlay, FdUiButtonComponent, FdUiCalendarComponent, FdUiIconComponent],
    templateUrl: './fd-ui-week-picker.html',
    styleUrl: './fd-ui-week-picker.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiWeekPickerComponent {
    private readonly defaultLocale = inject(LOCALE_ID);

    public readonly value = model<Date>(fdUiStartOfLocalWeek(new Date()));
    public readonly min = input<Date | null>(null);
    public readonly max = input<Date | null>(fdUiStartOfLocalWeek(new Date()));
    public readonly panelTitle = input('Choose a week');
    public readonly currentWeekLabel = input('Current week');
    public readonly returnToCurrentWeekLabel = input('Return to current week');
    public readonly previousWeekAriaLabel = input('Previous week');
    public readonly nextWeekAriaLabel = input('Next week');
    public readonly openCalendarAriaLabel = input('Choose a week');
    public readonly locale = input<string | null>(null);

    protected readonly isOpen = signal(false);
    protected readonly displayMonth = signal<Date>(this.value());
    protected readonly currentWeek = fdUiStartOfLocalWeek(new Date());
    protected readonly rangeLabel = computed(() => this.formatRange(this.normalizedValue()));
    protected readonly isCurrentWeek = computed(() => this.isSameDay(this.normalizedValue(), this.currentWeek));
    protected readonly previousDisabled = computed(() =>
        this.isBeforeMinimum(fdUiAddLocalDays(this.normalizedValue(), PREVIOUS_WEEK_OFFSET)),
    );
    protected readonly nextDisabled = computed(() => this.isAfterMaximum(fdUiAddLocalDays(this.normalizedValue(), NEXT_WEEK_OFFSET)));
    protected readonly effectiveLocale = computed(() => this.locale() ?? this.defaultLocale);

    protected open(): void {
        this.displayMonth.set(this.normalizedValue());
        this.isOpen.set(true);
    }

    protected close(): void {
        this.isOpen.set(false);
    }

    protected selectPreviousWeek(): void {
        this.selectWeek(fdUiAddLocalDays(this.normalizedValue(), PREVIOUS_WEEK_OFFSET));
    }

    protected selectNextWeek(): void {
        this.selectWeek(fdUiAddLocalDays(this.normalizedValue(), NEXT_WEEK_OFFSET));
    }

    protected selectCurrentWeek(): void {
        this.selectWeek(this.currentWeek);
        this.close();
    }

    protected onCalendarSelect(value: Date | null): void {
        if (value !== null) {
            this.selectWeek(value);
            this.close();
        }
    }

    protected onDisplayMonthChange(value: Date | null): void {
        if (value !== null) {
            this.displayMonth.set(value);
        }
    }

    protected onOverlayKeydown(event: KeyboardEvent): void {
        if (event.key === 'Escape') {
            event.preventDefault();
            this.close();
        }
    }

    protected normalizedValue(): Date {
        return fdUiStartOfLocalWeek(this.value());
    }

    private selectWeek(date: Date): void {
        const normalized = fdUiStartOfLocalWeek(date);
        if (this.isBeforeMinimum(normalized) || this.isAfterMaximum(normalized)) {
            return;
        }

        this.value.set(normalized);
        this.displayMonth.set(normalized);
    }

    private isBeforeMinimum(date: Date): boolean {
        const minimum = this.min();
        return minimum !== null && date < fdUiStartOfLocalWeek(minimum);
    }

    private isAfterMaximum(date: Date): boolean {
        const maximum = this.max();
        return maximum !== null && date > fdUiStartOfLocalWeek(maximum);
    }

    private formatRange(weekStart: Date): string {
        const weekEnd = fdUiAddLocalDays(weekStart, WEEK_LAST_DAY_OFFSET);
        const sameMonth = weekStart.getMonth() === weekEnd.getMonth() && weekStart.getFullYear() === weekEnd.getFullYear();
        const startOptions: Intl.DateTimeFormatOptions = sameMonth ? { day: 'numeric' } : { day: 'numeric', month: 'long' };
        const start = new Intl.DateTimeFormat(this.effectiveLocale(), startOptions).format(weekStart);
        const end = new Intl.DateTimeFormat(this.effectiveLocale(), { day: 'numeric', month: 'long' }).format(weekEnd);
        return `${start}–${end}`;
    }

    private isSameDay(left: Date, right: Date): boolean {
        return fdUiStartOfLocalDay(left).getTime() === fdUiStartOfLocalDay(right).getTime();
    }
}

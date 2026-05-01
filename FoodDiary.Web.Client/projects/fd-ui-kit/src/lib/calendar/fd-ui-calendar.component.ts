import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, input, LOCALE_ID, output, signal } from '@angular/core';

import { FdUiButtonComponent } from '../button/fd-ui-button.component';

interface FdUiCalendarCell {
    date: Date;
    iso: string;
    label: string;
    isCurrentMonth: boolean;
    isToday: boolean;
    isSelected: boolean;
    isActive: boolean;
    isDisabled: boolean;
}

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
    private readonly host = inject(ElementRef<HTMLElement>);
    private readonly today = this.stripTime(new Date());
    private readonly activeDate = signal<Date>(this.today);

    public readonly value = input<Date | null>(null);
    public readonly displayMonth = input<Date | null>(null);
    public readonly min = input<Date | null>(null);
    public readonly max = input<Date | null>(null);
    public readonly weekStartsOn = input<0 | 1>(1);

    public readonly valueChange = output<Date>();
    public readonly displayMonthChange = output<Date>();

    protected readonly visibleMonth = computed(() => {
        const month = this.displayMonth() ?? this.value() ?? this.activeDate() ?? this.today;
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
        return Array.from({ length: 7 }, (_, index) => {
            const day = new Date(Date.UTC(2024, 0, 7 + ((startIndex + index) % 7)));
            return new Intl.DateTimeFormat(this.locale, { weekday: 'short', timeZone: 'UTC' }).format(day);
        });
    });

    protected readonly weeks = computed(() => {
        const monthStart = this.visibleMonth();
        const gridStart = this.startOfWeek(monthStart);
        const selectedIso = this.toIsoDate(this.value());
        const activeIso = this.toIsoDate(this.activeDate()) ?? selectedIso ?? this.toIsoDate(this.today);

        return Array.from({ length: 6 }, (_, weekIndex) =>
            Array.from({ length: 7 }, (_, dayIndex) => {
                const cellDate = this.addDays(gridStart, weekIndex * 7 + dayIndex);
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
        this.valueChange.emit(normalized);
    }

    protected showPreviousMonth(): void {
        this.changeMonth(-1);
    }

    protected showNextMonth(): void {
        this.changeMonth(1);
    }

    protected onCellKeydown(event: KeyboardEvent, date: Date): void {
        let nextDate: Date | null = null;

        switch (event.key) {
            case 'ArrowLeft':
                nextDate = this.addDays(date, -1);
                break;
            case 'ArrowRight':
                nextDate = this.addDays(date, 1);
                break;
            case 'ArrowUp':
                nextDate = this.addDays(date, -7);
                break;
            case 'ArrowDown':
                nextDate = this.addDays(date, 7);
                break;
            case 'Home':
                nextDate = this.startOfWeek(date);
                break;
            case 'End':
                nextDate = this.addDays(this.startOfWeek(date), 6);
                break;
            case 'PageUp':
                nextDate = this.addMonths(date, -1);
                break;
            case 'PageDown':
                nextDate = this.addMonths(date, 1);
                break;
            case 'Enter':
            case ' ':
                event.preventDefault();
                this.selectDate(date);
                return;
            default:
                return;
        }

        event.preventDefault();
        const normalized = this.clampDate(this.stripTime(nextDate), this.min(), this.max());
        this.activeDate.set(normalized);

        if (normalized.getMonth() !== this.visibleMonth().getMonth() || normalized.getFullYear() !== this.visibleMonth().getFullYear()) {
            this.displayMonthChange.emit(this.startOfMonth(normalized));
        }

        this.focusCell(normalized);
    }

    private changeMonth(offset: number): void {
        const target = this.startOfMonth(this.addMonths(this.visibleMonth(), offset));
        this.displayMonthChange.emit(target);
        this.focusCell(this.activeDate());
    }

    private focusCell(date: Date): void {
        queueMicrotask(() => {
            const iso = this.toIsoDate(date);
            const host = this.host.nativeElement as HTMLElement;
            const cell = host.querySelector(`[data-date="${iso}"]`);
            if (cell instanceof HTMLElement) {
                cell.focus();
            }
        });
    }

    private startOfWeek(date: Date): Date {
        const normalized = this.stripTime(date);
        const delta = (normalized.getDay() - this.weekStartsOn() + 7) % 7;
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
        if (min && date < this.stripTime(min)) {
            return this.stripTime(min);
        }

        if (max && date > this.stripTime(max)) {
            return this.stripTime(max);
        }

        return date;
    }

    private isOutOfRange(date: Date, min: Date | null, max: Date | null): boolean {
        const normalized = this.stripTime(date);
        if (min && normalized < this.stripTime(min)) {
            return true;
        }

        if (max && normalized > this.stripTime(max)) {
            return true;
        }

        return false;
    }

    private stripTime(date: Date | null): Date {
        if (!date) {
            return this.today;
        }

        return new Date(date.getFullYear(), date.getMonth(), date.getDate());
    }

    private toIsoDate(date: Date | null): string {
        const normalized = this.stripTime(date);
        const year = normalized.getFullYear();
        const month = String(normalized.getMonth() + 1).padStart(2, '0');
        const day = String(normalized.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}

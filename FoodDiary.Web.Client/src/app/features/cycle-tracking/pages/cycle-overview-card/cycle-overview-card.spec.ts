import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { CycleOverviewViewModel } from '../cycle-tracking-page-lib/cycle-tracking-page.types';
import { CycleOverviewCardComponent } from './cycle-overview-card';

const OVERVIEW: CycleOverviewViewModel = {
    todayDateKey: '2026-08-17',
    todayDateLabel: 'Monday, August 17',
    monthLabel: 'August 2026',
    cycleDayNumber: 8,
    hasTodayEntry: false,
    days: [
        {
            dateKey: '2026-08-16',
            weekdayLabel: 'Sun',
            dayLabel: '16',
            cycleDayNumber: 7,
            isToday: false,
            isFuture: false,
            isBleeding: true,
            isPredictedPeriod: false,
            isTracked: true,
        },
        {
            dateKey: '2026-08-17',
            weekdayLabel: 'Mon',
            dayLabel: '17',
            cycleDayNumber: 8,
            isToday: true,
            isFuture: false,
            isBleeding: false,
            isPredictedPeriod: false,
            isTracked: false,
        },
    ],
};

describe('CycleOverviewCardComponent', () => {
    let fixture: ComponentFixture<CycleOverviewCardComponent>;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [CycleOverviewCardComponent],
            providers: [provideTranslateTesting()],
        });
        fixture = TestBed.createComponent(CycleOverviewCardComponent);
        fixture.componentRef.setInput('overview', OVERVIEW);
        fixture.detectChanges();
    });

    it('renders today and the compact day strip', () => {
        const host = fixture.nativeElement as HTMLElement;

        expect(host.textContent).toContain(OVERVIEW.todayDateLabel);
        expect(host.querySelectorAll('.cycle-overview__day')).toHaveLength(OVERVIEW.days.length);
        expect(host.querySelector('.cycle-overview__day--today')?.getAttribute('aria-current')).toBe('date');
    });

    it('emits a selected past date', () => {
        const selected = vi.fn();
        fixture.componentInstance.dateSelected.subscribe(selected);

        (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.cycle-overview__day')?.click();

        expect(selected).toHaveBeenCalledWith('2026-08-16');
    });

    it('emits the primary log action', () => {
        const logToday = vi.fn();
        fixture.componentInstance.logToday.subscribe(logToday);

        const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button');
        buttons.item(buttons.length - 1).click();

        expect(logToday).toHaveBeenCalledOnce();
    });
});

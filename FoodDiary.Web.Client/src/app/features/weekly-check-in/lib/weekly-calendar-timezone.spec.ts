import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NEVER, of } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { MeasurementSystemService } from '../../../shared/measurements/measurement-system.service';
import type { CalendarDate } from '../../../shared/models/semantics/date-value';
import { WeeklyCheckInService } from '../api/weekly-check-in.service';
import { WeeklyGoalService } from '../api/weekly-goal.service';
import { WeeklyCheckInFacade } from './weekly-check-in.facade';

afterEach(() => vi.useRealTimers());

describe('weekly goal calendar boundaries in the active timezone', () => {
    it.each([
        { local: '2024-02-29T00:05:00', selected: '2024-02-26', next: '2024-03-04' },
        { local: '2025-12-31T23:55:00', selected: '2025-12-29', next: '2026-01-05' },
        { local: '2026-01-01T00:05:00', selected: '2025-12-29', next: '2026-01-05' },
        { local: '2026-03-08T01:55:00', selected: '2026-03-02', next: '2026-03-09' },
        { local: '2026-03-29T01:55:00', selected: '2026-03-23', next: '2026-03-30' },
        { local: '2026-11-01T01:55:00', selected: '2026-10-26', next: '2026-11-02' },
    ])('keeps local Monday $selected and next Monday $next', ({ local, selected, next }) => {
        vi.useFakeTimers({ toFake: ['Date'] });
        vi.setSystemTime(new Date(local));
        TestBed.configureTestingModule({
            providers: [
                WeeklyCheckInFacade,
                { provide: WeeklyCheckInService, useValue: { getData: vi.fn((_week: CalendarDate) => NEVER) } },
                { provide: WeeklyGoalService, useValue: { getGoal: vi.fn((_week: CalendarDate) => of(null)) } },
                { provide: MeasurementSystemService, useValue: { system: signal<'metric' | 'imperial'>('metric') } },
            ],
        });
        const facade = TestBed.inject(WeeklyCheckInFacade);
        expect(facade.selectedWeekStartIso()).toBe(selected);
        expect(facade.goalWeekStartIso()).toBe(next);
    });
});

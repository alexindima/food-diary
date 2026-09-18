import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminRetentionService } from '../api/admin-retention.service';
import { AdminRetentionComponent } from './admin-retention';

describe('AdminRetentionComponent', () => {
    const query = new BehaviorSubject(convertToParamMap({}));
    const getReport = vi.fn();

    beforeEach(async () => {
        query.next(convertToParamMap({}));
        getReport.mockReset().mockReturnValue(
            of({
                fromUtc: '2026-01-01T00:00:00Z',
                cohortFromUtc: '2025-11-01T00:00:00Z',
                cohortToUtc: '2026-01-03T00:00:00Z',
                toUtc: '2026-01-03T00:00:00Z',
                mealEntriesInPeriod: 3,
                asOfUtc: '2026-02-01T00:00:00Z',
                activeUsersInPeriod: 0,
                activityByDay: [
                    { date: '2026-01-01T00:00:00Z', activeUsers: 1, mealEntries: 3 },
                    { date: '2026-01-02T00:00:00Z', activeUsers: 0, mealEntries: 0 },
                ],
                cohorts: [{ date: '2026-01-01T00:00:00Z', registered: 2, activatedWithinSevenDays: 0, day1: 0, day7: 0, day30: null }],
            }),
        );
        await TestBed.configureTestingModule({
            imports: [AdminRetentionComponent],
            providers: [
                provideRouter([]),
                ...provideTranslateTesting(),
                { provide: ActivatedRoute, useValue: { queryParamMap: query } },
                { provide: AdminRetentionService, useValue: { getReport } },
            ],
        }).compileComponents();
    });

    it('renders a mature zero separately from a cohort whose observation window is incomplete', () => {
        const fixture = TestBed.createComponent(AdminRetentionComponent);
        fixture.detectChanges();
        const text = (fixture.nativeElement as HTMLElement).textContent;
        expect(text).toContain('Jan 1, 2026');
        expect(text).toContain('0.0% (0/2)');
        expect(text).toContain('ADMIN_RETENTION.IMMATURE');
    });

    it('sends independent activity and cohort periods and renders zero days', () => {
        query.next(
            convertToParamMap({
                period: 'custom',
                from: '2025-11-01',
                to: '2025-12-01',
                activity_period: 'custom',
                activity_from: '2026-01-01',
                activity_to: '2026-01-02',
            }),
        );
        const fixture = TestBed.createComponent(AdminRetentionComponent);
        fixture.detectChanges();
        expect(getReport).toHaveBeenLastCalledWith({
            from: '2026-01-01',
            to: '2026-01-02',
            cohortFrom: '2025-11-01',
            cohortTo: '2025-12-01',
        });
        const element = fixture.nativeElement as HTMLElement;
        expect(element.textContent).toContain('Jan 2, 2026');
        expect(element.querySelectorAll('tbody')[0].rows[1].textContent).toMatch(/Jan 2, 2026\s*0\s*0/);
        expect(element.querySelector('details')).toBeNull();
    });

    it('shows failure and can retry without presenting a false empty report', () => {
        getReport.mockReturnValueOnce(throwError(() => new Error('offline')));
        const fixture = TestBed.createComponent(AdminRetentionComponent);
        fixture.detectChanges();
        expect(fixture.componentInstance['failed']()).toBe(true);
        expect(fixture.componentInstance['report']()).toBeNull();
        fixture.componentInstance['retry']();
        fixture.detectChanges();
        expect(fixture.componentInstance['failed']()).toBe(false);
        expect(fixture.componentInstance['report']()).not.toBeNull();
    });
});

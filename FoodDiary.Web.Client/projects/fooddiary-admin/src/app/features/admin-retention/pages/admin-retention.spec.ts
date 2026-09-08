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
                from: '2026-01-01',
                to: '2026-01-02',
                asOfUtc: '2026-02-01T00:00:00Z',
                activeUsersInPeriod: 0,
                activityByDay: [],
                cohorts: [{ date: '2026-01-01', registered: 2, activatedWithinSevenDays: 0, day1: 0, day7: 0, day30: null }],
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

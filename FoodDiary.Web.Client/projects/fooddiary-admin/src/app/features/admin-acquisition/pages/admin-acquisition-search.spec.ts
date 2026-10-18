import { HttpErrorResponse } from '@angular/common/http';
import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { type Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminAcquisitionFacade } from '../lib/admin-acquisition.facade';
import type { MarketingAttributionSummary } from '../models/admin-acquisition.data';
import type { MarketingAttributionRange } from '../models/admin-acquisition-range';
import { AdminAcquisitionComponent } from './admin-acquisition';

type RangeRequest = Mock<(params: Record<string, string | number>) => Observable<MarketingAttributionRange>>;
const SEARCH_LIMIT = 320;
const BAD_REQUEST = 400;
const UNAVAILABLE = 503;
const HOUR_MS = 3_600_000;

describe('AdminAcquisition search boundaries and error recovery', () => {
    let fixture: ComponentFixture<AdminAcquisitionComponent>;
    const getRange: RangeRequest = vi.fn(params => of(emptyRange(params)));

    beforeEach(async () => {
        getRange.mockReset();
        getRange.mockImplementation(params => of(emptyRange(params)));
        await TestBed.configureTestingModule({
            imports: [AdminAcquisitionComponent],
            providers: [provideRouter([]), ...provideTranslateTesting(), { provide: AdminAcquisitionFacade, useValue: { getRange } }],
        }).compileComponents();
        fixture = TestBed.createComponent(AdminAcquisitionComponent);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
    });

    it('limits the native search field to the API boundary', () => {
        const search = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('input#acquisition-event-search');
        expect(search?.maxLength).toBe(SEARCH_LIMIT);
    });

    it.each([BAD_REQUEST, UNAVAILABLE])(
        'keeps filters editable after HTTP %s and preserves the selected period on recovery',
        async status => {
            await verifyEditableRecoveryAsync(fixture, getRange, status);
        },
    );
});

async function verifyEditableRecoveryAsync(
    fixture: ComponentFixture<AdminAcquisitionComponent>,
    getRange: RangeRequest,
    status: number,
): Promise<void> {
    const original = status === BAD_REQUEST ? 'q'.repeat(SEARCH_LIMIT + 1) : 'retryable';
    getRange.mockImplementationOnce(() => throwError(() => new HttpErrorResponse({ status })));
    await TestBed.inject(Router).navigate([], {
        queryParams: { period: '7d', search: original, eventType: 'page_landing', channel: 'tracked', page: 2 },
    });
    await fixture.whenStable();
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const search = element.querySelector<HTMLInputElement>('input#acquisition-event-search');
    expect(element.querySelector('[role=alert]')).not.toBeNull();
    expect(search, 'failed reports must retain an editable search filter').not.toBeNull();
    if (search === null) {
        throw new Error('Search filter disappeared');
    }
    expect(search.value).toBe(original);
    expect(search.disabled).toBe(false);
    search.value = 'recovered';
    search.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
    const apply = element.querySelector<HTMLButtonElement>('.admin-filter-bar fd-ui-button button');
    expect(apply).toBeDefined();
    apply?.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(getRange).toHaveBeenLastCalledWith(
        expect.objectContaining({ search: 'recovered', eventType: 'page_landing', channel: 'tracked', page: 1 }),
    );
    expect(TestBed.inject(Router).parseUrl(TestBed.inject(Router).url).queryParams['period']).toBe('7d');
    expect(element.querySelector('[role=alert]')).toBeNull();
    expect(element.querySelectorAll('input#acquisition-event-search')).toHaveLength(1);
}

function emptyRange(params: Record<string, string | number>): MarketingAttributionRange {
    const fromUtc = String(params['fromUtc']);
    const toUtc = String(params['toUtc']);
    const hours = (Date.parse(toUtc) - Date.parse(fromUtc)) / HOUR_MS;
    const summary: MarketingAttributionSummary = {
        windowHours: hours,
        generatedAtUtc: toUtc,
        events: 0,
        visits: 0,
        signups: 0,
        premiumStarts: 0,
        anonymousVisitors: 0,
        sessions: 0,
        attributedEvents: 0,
        organicEvents: 0,
        attributedVisits: 0,
        organicVisits: 0,
        signupRatePercent: 0,
        premiumRatePercent: 0,
        lastEventAtUtc: null,
        topCampaigns: [],
        topSources: [],
        recentEvents: [],
    };
    return {
        fromUtc,
        toUtc,
        previousFromUtc: new Date(Date.parse(fromUtc) - hours * HOUR_MS).toISOString(),
        current: summary,
        previous: summary,
        byDay: [],
        eventTotal: 0,
    };
}

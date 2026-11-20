import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { FrontendObservabilityService } from '../../services/frontend-observability.service';
import type { FastingOverviewHttpResponse } from '../../shared/api/sdk/generated/model/fasting-overview-http-response';
import type { FastingSessionHttpResponse } from '../../shared/api/sdk/generated/model/fasting-session-http-response';
import { UserService } from '../../shared/api/user.service';
import { LocalizationService } from '../../shared/i18n/localization.service';
import fastingRoutes from './fasting.routes';

const STARTED_AT = '2026-05-18T08:00:00.000Z';
const ENDED_AT = '2026-05-20T12:00:00.000Z';
const OLD_NOTES = 'Older journal with only an energy rating';
const EXTENDED_BASE_HOURS = 36;
const INTERMITTENT_BASE_HOURS = 14;
const ADJUSTMENT_HOURS = 4;
const ENERGY_RATING = 4;
const WELLBEING_METRIC_COUNT = 3;
const HISTORY_ROW = '.fasting-history-dialog__row';
const DETAILS_DIALOG = 'fd-fasting-session-details-dialog';

afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.inject(FdUiDialogService).closeAll();
    vi.unstubAllGlobals();
});

describe('Fasting history with a protocol newer than the client catalogue', () => {
    it.each([
        {
            planType: 'Intermittent',
            protocol: 'Fast14Eat10',
            base: INTERMITTENT_BASE_HOURS,
            added: 0,
            label: 'Intermittent fasting · 14 hours',
        },
        {
            planType: 'Extended',
            protocol: 'Fast36Plus',
            base: EXTENDED_BASE_HOURS,
            added: ADJUSTMENT_HOURS,
            label: 'Extended fasting · 36 hours (+4 hours)',
        },
        {
            planType: 'Extended',
            protocol: 'Fast36Plus',
            base: EXTENDED_BASE_HOURS,
            added: -ADJUSTMENT_HOURS,
            label: 'Extended fasting · 36 hours (-4 hours)',
        },
    ])('preserves the $planType plan and saved duration change of $added hours', async ({ planType, protocol, base, added, label }) => {
        const session: FastingSessionHttpResponse = {
            id: 'b1a34167-6127-4238-8c99-a7faca04491a',
            startedAtUtc: STARTED_AT,
            endedAtUtc: ENDED_AT,
            protocol,
            planType,
            occurrenceKind: 'FastingWindow',
            initialPlannedDurationHours: base,
            addedDurationHours: added,
            plannedDurationHours: base + added,
            isCompleted: true,
            status: 'Completed',
            checkInAtUtc: ENDED_AT,
            energyLevel: ENERGY_RATING,
            symptoms: [],
            checkInNotes: OLD_NOTES,
            checkIns: [],
        };
        const harness = await renderFastingHistoryAsync(session);
        const history = requireFastingElement(document, 'fd-fasting-history-dialog');
        const row = requireFastingElement(history, HISTORY_ROW);
        expect(row.textContent).toContain(label);
        row.click();
        harness.detectChanges();

        const details = requireFastingElement(document, DETAILS_DIALOG);
        expect(details.querySelector('.fasting-session-details-dialog__summary strong')?.textContent.trim()).toBe(label);
        expect(details.querySelector('.fasting__history-notes')?.textContent).toContain(OLD_NOTES);
        expect(details.querySelector('.fasting__history-checkin-summary')?.textContent).toBe('Hunger — · Energy ⚡ · Mood —');
        expect(details.querySelector('fd-fasting-check-in-chart')).toBeNull();
    });
});

describe('Fasting wellbeing history', () => {
    it('plots both check-ins when their server timestamps fall within the same millisecond', async () => {
        const firstNote = 'Check-in before a glass of water';
        const secondNote = 'Check-in after a glass of water';
        const harness = await renderFastingHistoryAsync({
            id: 'b1a34167-6127-4238-8c99-a7faca04491a',
            startedAtUtc: STARTED_AT,
            endedAtUtc: '2026-05-18T22:00:00.000Z',
            protocol: 'Fast14Eat10',
            planType: 'Intermittent',
            occurrenceKind: 'FastingWindow',
            initialPlannedDurationHours: INTERMITTENT_BASE_HOURS,
            addedDurationHours: 0,
            plannedDurationHours: INTERMITTENT_BASE_HOURS,
            isCompleted: true,
            status: 'Completed',
            symptoms: [],
            checkIns: [
                {
                    id: 'b7410a8f-8962-4918-994d-8b084803a788',
                    checkedInAtUtc: '2026-05-18T12:00:00.308400Z',
                    hungerLevel: 4,
                    energyLevel: 2,
                    moodLevel: 3,
                    symptoms: [],
                    notes: firstNote,
                },
                {
                    id: '637c70a8-18a5-4528-85e3-ea24bb3e69f9',
                    checkedInAtUtc: '2026-05-18T12:00:00.308900Z',
                    hungerLevel: 2,
                    energyLevel: 3,
                    moodLevel: 4,
                    symptoms: [],
                    notes: secondNote,
                },
            ],
        });
        const history = requireFastingElement(document, 'fd-fasting-history-dialog');
        requireFastingElement(history, HISTORY_ROW).click();
        harness.detectChanges();

        const details = requireFastingElement(document, DETAILS_DIALOG);
        const entries = details.querySelectorAll('fd-fasting-history-check-in-entry');
        expect(entries).toHaveLength(2);
        expect(details.textContent).toContain(firstNote);
        expect(details.textContent).toContain(secondNote);

        const chart = requireFastingElement(details, 'fd-fasting-check-in-chart');
        const points = Array.from(chart.querySelectorAll<HTMLElement>('.fd-ui-line-chart__point'));
        expect(points).toHaveLength(entries.length * WELLBEING_METRIC_COUNT);
        const [firstHunger, secondHunger] = points;
        expect(firstHunger.style.left).not.toBe(secondHunger.style.left);
        expect(firstHunger.style.top).not.toBe(secondHunger.style.top);
    });
});

async function renderFastingHistoryAsync(session: FastingSessionHttpResponse): Promise<RouterTestingHarness> {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'fasting', children: fastingRoutes }]),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: UserService, useValue: { user: signal(null) } },
            { provide: LocalizationService, useValue: { getCurrentLanguage: (): string => 'en' } },
            { provide: FrontendObservabilityService, useValue: { recordFastingLifecycleEvent: vi.fn() } },
        ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', {
        FASTING: {
            INTERMITTENT_TYPE: 'Intermittent fasting',
            EXTENDED_TYPE: 'Extended fasting',
            HOURS: 'hours',
            CHECK_IN: { SUMMARY: 'Hunger {{hunger}} · Energy {{energy}} · Mood {{mood}}' },
        },
    });
    await firstValueFrom(translate.use('en'));
    const harness = await RouterTestingHarness.create('/fasting');
    TestBed.inject(HttpTestingController).expectOne(`${environment.apiUrls.fasting}/overview`).flush(historyOverview(session));
    harness.detectChanges();
    const page = harness.routeNativeElement;
    if (page === null) {
        throw new Error('Expected the fasting route');
    }
    requireFastingElement(page, '.fasting-redesign__history-action button').click();
    harness.detectChanges();
    return harness;
}

function historyOverview(session: FastingSessionHttpResponse): FastingOverviewHttpResponse {
    return {
        currentSession: null,
        stats: {
            totalCompleted: 1,
            currentStreak: 1,
            averageDurationHours: session.plannedDurationHours,
            completionRateLast30Days: 100,
            checkInRateLast30Days: 100,
        },
        insights: { alerts: [], insights: [] },
        history: { data: [session], page: 1, limit: 10, totalPages: 1, totalItems: 1 },
    };
}

function requireFastingElement(root: ParentNode, selector: string): HTMLElement {
    const element = root.querySelector(selector);
    if (!(element instanceof HTMLElement)) {
        throw new Error(`Expected fasting history element ${selector}`);
    }
    return element;
}

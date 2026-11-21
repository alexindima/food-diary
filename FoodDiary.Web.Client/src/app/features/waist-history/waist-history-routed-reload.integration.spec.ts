import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { DeferBlockBehavior, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import type { UserDesiredWaistHttpResponse } from '../../shared/api/sdk/generated/model/user-desired-waist-http-response';
import type { WaistEntryHttpResponse } from '../../shared/api/sdk/generated/model/waist-entry-http-response';
import type { WaistHistoryPageSummaryHttpResponse } from '../../shared/api/sdk/generated/model/waist-history-page-summary-http-response';
import { formatWaistHistoryDateInput } from './lib/waist-history-range.utils';
import routes from './waist-history.routes';

const WAIST_ROUTE = '/waist-history';
const WAIST_URL = environment.apiUrls.waists;
const GOAL_URL = `${environment.apiUrls.users}/desired-waist`;
const ENTRY_DIALOG = 'fd-waist-history-entry-dialog';
const GOAL_DIALOG = 'fd-waist-history-goal-dialog';
const ADD = 'button[aria-label="WAIST_HISTORY.QUICK_ADD"]';
const CONFIGURE_GOAL = '.waist-history-page__goal-primary-action button';
const INITIAL_MEASUREMENT = 81;
const SAVED_MEASUREMENT = 84;
const ORIGINAL_TARGET = 79;
const SAVED_TARGET = 75;
const CHART_START = '2026-06-01';
const CHART_END = '2026-06-30';
const INITIAL_ENTRY = {
    id: 'previous-waist-entry',
    userId: 'waist-editor-owner',
    date: '2026-06-20',
    circumferenceCm: INITIAL_MEASUREMENT,
} satisfies WaistEntryHttpResponse;
const NEW_ENTRY = {
    ...INITIAL_ENTRY,
    id: 'new-waist-entry',
    date: formatWaistHistoryDateInput(new Date()),
    circumferenceCm: SAVED_MEASUREMENT,
};
const ORIGINAL_GOAL = {
    desiredWaistCm: ORIGINAL_TARGET,
    startWaistCm: INITIAL_MEASUREMENT,
    startedAtUtc: '2026-06-20T10:00:00Z',
} satisfies UserDesiredWaistHttpResponse;
const NEW_GOAL = {
    desiredWaistCm: SAVED_TARGET,
    startWaistCm: SAVED_MEASUREMENT,
    startedAtUtc: '2026-06-21T10:00:00Z',
} satisfies UserDesiredWaistHttpResponse;

describe('Waist history routed refresh ownership', () => {
    beforeEach(() => {
        vi.stubGlobal('matchMedia', (media: string) => ({
            matches: false,
            media,
            onchange: null,
            addListener: vi.fn(),
            removeListener: vi.fn(),
            addEventListener: vi.fn(),
            removeEventListener: vi.fn(),
            dispatchEvent: vi.fn(() => true),
        }));
        TestBed.configureTestingModule({
            deferBlockBehavior: DeferBlockBehavior.Manual,
            providers: [
                provideRouter([{ path: 'waist-history', children: routes }]),
                provideHttpClient(),
                provideHttpClientTesting(),
                provideTranslateTesting(),
            ],
        });
    });

    afterEach(() => {
        vi.unstubAllGlobals();
        TestBed.inject(HttpTestingController).verify();
    });

    it.each(['older response', 'older error'] as const)(
        'preserves the saved goal after an %s from the measurement refresh',
        async outcome => {
            const harness = await openWaistHistoryAsync();
            const older = await saveMeasurementAsync(harness);
            const latest = await saveGoalAsync(harness);
            latest.flush(pageSummary(NEW_ENTRY, NEW_GOAL));

            if (outcome === 'older response') {
                older.flush(pageSummary(NEW_ENTRY, ORIGINAL_GOAL));
            } else {
                older.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
            }
            harness.detectChanges();

            expect(pageError(harness).hidden).toBe(true);
            expect(currentMeasurement(harness)).toContain(String(SAVED_MEASUREMENT));
            await openEditorAsync(harness, CONFIGURE_GOAL, GOAL_DIALOG);
            expect(numberInput(GOAL_DIALOG).value).toBe(String(SAVED_TARGET));
        },
    );

    it('applies current body measurements even when a newer chart range read completes first', async () => {
        const harness = await openWaistHistoryAsync();
        const page = await saveMeasurementAsync(harness);
        const period = findButtonByLabel(requiredHost(harness.routeNativeElement), 'fd-period-filter button', 'WAIST_HISTORY.RANGE_YEAR');
        period.click();
        harness.detectChanges();
        const chart = expectHttp('GET', `${WAIST_URL}/summary`);
        chart.flush([{ startDate: CHART_START, endDate: CHART_END, averageCircumferenceCm: SAVED_MEASUREMENT }]);
        harness.detectChanges();
        expect(currentMeasurement(harness)).toContain(String(INITIAL_MEASUREMENT));

        page.flush(pageSummary(NEW_ENTRY, ORIGINAL_GOAL));
        harness.detectChanges();

        expect(period.getAttribute('aria-checked')).toBe('true');
        expect(currentMeasurement(harness)).toContain(String(SAVED_MEASUREMENT));
        expect(pageError(harness).hidden).toBe(true);
    });
});

async function openWaistHistoryAsync(): Promise<RouterTestingHarness> {
    const harness = await RouterTestingHarness.create(WAIST_ROUTE);
    expectHttp('GET', `${WAIST_URL}/page-summary`).flush(pageSummary(INITIAL_ENTRY, ORIGINAL_GOAL));
    harness.detectChanges();
    expect(currentMeasurement(harness)).toContain(String(INITIAL_MEASUREMENT));
    return harness;
}

async function saveMeasurementAsync(harness: RouterTestingHarness): Promise<TestRequest> {
    await openEditorAsync(harness, ADD, ENTRY_DIALOG);
    changeNumber(ENTRY_DIALOG, SAVED_MEASUREMENT);
    harness.detectChanges();
    click(requiredHost(document.querySelector(ENTRY_DIALOG)), 'button[type="submit"]');
    const request = expectHttp('POST', WAIST_URL);
    expect(request.request.body).toEqual({ date: `${NEW_ENTRY.date}T00:00:00.000Z`, circumferenceCm: SAVED_MEASUREMENT });
    request.flush(NEW_ENTRY);
    await waitUntilClosedAsync(harness, ENTRY_DIALOG);
    return expectHttp('GET', `${WAIST_URL}/page-summary`);
}

async function saveGoalAsync(harness: RouterTestingHarness): Promise<TestRequest> {
    await openEditorAsync(harness, CONFIGURE_GOAL, GOAL_DIALOG);
    changeNumber(GOAL_DIALOG, SAVED_TARGET);
    harness.detectChanges();
    click(requiredHost(document.querySelector(GOAL_DIALOG)), 'button[type="submit"]');
    const request = expectHttp('PUT', GOAL_URL);
    expect(request.request.body).toEqual({ desiredWaistCm: SAVED_TARGET });
    request.flush(NEW_GOAL);
    await waitUntilClosedAsync(harness, GOAL_DIALOG);
    return expectHttp('GET', `${WAIST_URL}/page-summary`);
}

function pageSummary(entry: WaistEntryHttpResponse, goal: UserDesiredWaistHttpResponse): WaistHistoryPageSummaryHttpResponse {
    return {
        entries: entry.id === INITIAL_ENTRY.id ? [entry] : [entry, INITIAL_ENTRY],
        summary: [],
        heightCm: 180,
        goal,
        goalHistory: [],
    };
}

function pageError(harness: RouterTestingHarness): HTMLElement {
    const host = requiredHost(harness.routeNativeElement);
    const message = [...host.querySelectorAll<HTMLElement>('[role="alert"]')].find(element =>
        element.textContent.includes('WAIST_HISTORY.ERROR_LOAD_PAGE'),
    );
    return requiredHost(message?.parentElement ?? null);
}

function currentMeasurement(harness: RouterTestingHarness): string {
    return requiredHost(harness.routeNativeElement).querySelector('.waist-history-page__metric-value')?.textContent.trim() ?? '';
}

async function openEditorAsync(harness: RouterTestingHarness, trigger: string, dialog: string): Promise<void> {
    click(requiredHost(harness.routeNativeElement), trigger);
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(document.querySelector(dialog)).not.toBeNull();
    });
}

async function waitUntilClosedAsync(harness: RouterTestingHarness, selector: string): Promise<void> {
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(document.querySelector(selector)).toBeNull();
    });
}

function changeNumber(dialog: string, value: number): void {
    const input = numberInput(dialog);
    input.value = String(value);
    input.dispatchEvent(new Event('input', { bubbles: true }));
}

function numberInput(dialog: string): HTMLInputElement {
    const input = document.querySelector(`${dialog} fd-ui-input input`);
    if (!(input instanceof HTMLInputElement)) {
        throw new Error(`Expected waist measurement input in ${dialog}`);
    }
    return input;
}

function click(parent: HTMLElement, selector: string): void {
    const button = parent.querySelector(selector);
    if (!(button instanceof HTMLButtonElement) || button.disabled) {
        throw new Error(`Expected enabled waist history action ${selector}`);
    }
    button.click();
}

function findButtonByLabel(parent: HTMLElement, selector: string, label: string): HTMLButtonElement {
    const button = [...parent.querySelectorAll<HTMLButtonElement>(selector)].find(item => item.textContent.trim() === label);
    if (button === undefined || button.disabled) {
        throw new Error(`Expected enabled waist history action ${label}`);
    }
    return button;
}

function requiredHost(element: Element | null): HTMLElement {
    if (!(element instanceof HTMLElement)) {
        throw new Error('Expected the rendered waist history host');
    }
    return element;
}

function expectHttp(method: 'GET' | 'POST' | 'PUT', url: string): TestRequest {
    return TestBed.inject(HttpTestingController).expectOne(request => request.method === method && request.url === url, `${method} ${url}`);
}

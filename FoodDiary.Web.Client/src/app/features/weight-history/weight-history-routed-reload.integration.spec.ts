import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { DeferBlockBehavior, DeferBlockState, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import type { UserDesiredWeightHttpResponse } from '../../shared/api/sdk/generated/model/user-desired-weight-http-response';
import type { WeightEntryHttpResponse } from '../../shared/api/sdk/generated/model/weight-entry-http-response';
import type { WeightHistoryPageSummaryHttpResponse } from '../../shared/api/sdk/generated/model/weight-history-page-summary-http-response';
import routes from './weight-history.routes';

const WEIGHT_ROUTE = '/weight-history';
const WEIGHT_URL = environment.apiUrls.weights;
const GOAL_URL = `${environment.apiUrls.users}/desired-weight`;
const ENTRY_DIALOG = 'fd-weight-history-entry-dialog';
const GOAL_DIALOG = 'fd-weight-history-goal-dialog';
const ENTRIES_LOADER = 'fd-weight-history-entries-card .weight-history-page__loader';
const ADD = 'button[aria-label="WEIGHT_HISTORY.QUICK_ADD"]';
const CONFIGURE_GOAL = '.weight-history-page__goal-primary-action button';
const INITIAL_MEASUREMENT = 81;
const SAVED_MEASUREMENT = 84;
const ORIGINAL_TARGET = 79;
const SAVED_TARGET = 75;
const CHART_START = '2026-06-01';
const CHART_END = '2026-06-30';
const CURRENT_DAY = '2026-06-21';
const INITIAL_ENTRY = {
    id: 'previous-weight-entry',
    userId: 'weight-editor-owner',
    date: '2026-06-20',
    weightKg: INITIAL_MEASUREMENT,
} satisfies WeightEntryHttpResponse;
const NEW_ENTRY = {
    ...INITIAL_ENTRY,
    id: 'new-weight-entry',
    date: CURRENT_DAY,
    weightKg: SAVED_MEASUREMENT,
};
const ORIGINAL_GOAL = {
    desiredWeightKg: ORIGINAL_TARGET,
    startWeightKg: INITIAL_MEASUREMENT,
    startedAtUtc: '2026-06-20T10:00:00Z',
} satisfies UserDesiredWeightHttpResponse;
const NEW_GOAL = {
    desiredWeightKg: SAVED_TARGET,
    startWeightKg: SAVED_MEASUREMENT,
    startedAtUtc: '2026-06-21T10:00:00Z',
} satisfies UserDesiredWeightHttpResponse;

describe('Weight history routed refresh ownership', () => {
    beforeEach(configureWeightHistory);

    afterEach(() => {
        vi.useRealTimers();
        vi.unstubAllGlobals();
        TestBed.inject(HttpTestingController).verify();
    });

    it.each(['older response', 'older error'] as const)(
        'preserves the saved goal after an %s from the measurement refresh',
        async outcome => {
            const harness = await openWeightHistoryAsync();
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

    it('keeps the entries loading while the newer full refresh is pending', async () => {
        const harness = await openWeightHistoryAsync();
        for (const block of await harness.fixture.getDeferBlocks()) {
            await block.render(DeferBlockState.Complete);
        }
        const older = await saveMeasurementAsync(harness);
        const latest = await saveGoalAsync(harness);
        const host = requiredHost(harness.routeNativeElement);
        expect(host.querySelector(ENTRIES_LOADER)?.textContent).toContain('WEIGHT_HISTORY.LOADING');

        older.flush(pageSummary(NEW_ENTRY, ORIGINAL_GOAL));
        harness.detectChanges();
        const loadingWhileWaiting = host.querySelector(ENTRIES_LOADER)?.textContent;

        latest.flush(pageSummary(NEW_ENTRY, NEW_GOAL));
        harness.detectChanges();
        expect(loadingWhileWaiting).toContain('WEIGHT_HISTORY.LOADING');
        expect(host.querySelector(ENTRIES_LOADER)).toBeNull();
        expect(currentMeasurement(harness)).toContain(String(SAVED_MEASUREMENT));
    });

    it('applies current body measurements even when a newer chart range read completes first', async () => {
        const harness = await openWeightHistoryAsync();
        const page = await saveMeasurementAsync(harness);
        const period = findButtonByLabel(requiredHost(harness.routeNativeElement), 'fd-period-filter button', 'WEIGHT_HISTORY.RANGE_YEAR');
        period.click();
        harness.detectChanges();
        const chart = expectHttp('GET', `${WEIGHT_URL}/summary`);
        chart.flush([{ startDate: CHART_START, endDate: CHART_END, averageWeightKg: SAVED_MEASUREMENT }]);
        harness.detectChanges();
        expect(currentMeasurement(harness)).toContain(String(INITIAL_MEASUREMENT));

        page.flush(pageSummary(NEW_ENTRY, ORIGINAL_GOAL));
        harness.detectChanges();

        expect(period.getAttribute('aria-checked')).toBe('true');
        expect(currentMeasurement(harness)).toContain(String(SAVED_MEASUREMENT));
        expect(pageError(harness).hidden).toBe(true);
    });
});

function configureWeightHistory(): void {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(`${CURRENT_DAY}T12:00:00`));
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
            provideRouter([{ path: 'weight-history', children: routes }]),
            provideHttpClient(),
            provideHttpClientTesting(),
            provideTranslateTesting(),
        ],
    });
}

async function openWeightHistoryAsync(): Promise<RouterTestingHarness> {
    const harness = await RouterTestingHarness.create(WEIGHT_ROUTE);
    const localeRequests = TestBed.inject(HttpTestingController).match(
        request => request.method === 'GET' && /^\.\/assets\/i18n\/en\/(core|app)\.json(?:\?|$)/.test(request.url),
    );
    for (const request of localeRequests) {
        request.flush({});
    }
    expectHttp('GET', `${WEIGHT_URL}/page-summary`).flush(pageSummary(INITIAL_ENTRY, ORIGINAL_GOAL));
    harness.detectChanges();
    expect(currentMeasurement(harness)).toContain(String(INITIAL_MEASUREMENT));
    return harness;
}

async function saveMeasurementAsync(harness: RouterTestingHarness): Promise<TestRequest> {
    await openEditorAsync(harness, ADD, ENTRY_DIALOG);
    changeNumber(ENTRY_DIALOG, SAVED_MEASUREMENT);
    harness.detectChanges();
    click(requiredHost(document.querySelector(ENTRY_DIALOG)), 'button[type="submit"]');
    const request = expectHttp('POST', WEIGHT_URL);
    expect(request.request.body).toEqual({ date: `${NEW_ENTRY.date}T00:00:00.000Z`, weightKg: SAVED_MEASUREMENT });
    request.flush(NEW_ENTRY);
    await waitUntilClosedAsync(harness, ENTRY_DIALOG);
    return expectHttp('GET', `${WEIGHT_URL}/page-summary`);
}

async function saveGoalAsync(harness: RouterTestingHarness): Promise<TestRequest> {
    await openEditorAsync(harness, CONFIGURE_GOAL, GOAL_DIALOG);
    changeNumber(GOAL_DIALOG, SAVED_TARGET);
    harness.detectChanges();
    click(requiredHost(document.querySelector(GOAL_DIALOG)), 'button[type="submit"]');
    const request = expectHttp('PUT', GOAL_URL);
    expect(request.request.body).toEqual({ desiredWeightKg: SAVED_TARGET });
    request.flush(NEW_GOAL);
    await waitUntilClosedAsync(harness, GOAL_DIALOG);
    return expectHttp('GET', `${WEIGHT_URL}/page-summary`);
}

function pageSummary(entry: WeightEntryHttpResponse, goal: UserDesiredWeightHttpResponse): WeightHistoryPageSummaryHttpResponse {
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
        element.textContent.includes('WEIGHT_HISTORY.ERROR_LOAD_PAGE'),
    );
    return requiredHost(message?.parentElement ?? null);
}

function currentMeasurement(harness: RouterTestingHarness): string {
    return requiredHost(harness.routeNativeElement).querySelector('.weight-history-page__metric-value')?.textContent.trim() ?? '';
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
        throw new Error(`Expected weight measurement input in ${dialog}`);
    }
    return input;
}

function click(parent: HTMLElement, selector: string): void {
    const button = parent.querySelector(selector);
    if (!(button instanceof HTMLButtonElement) || button.disabled) {
        throw new Error(`Expected enabled weight history action ${selector}`);
    }
    button.click();
}

function findButtonByLabel(parent: HTMLElement, selector: string, label: string): HTMLButtonElement {
    const button = [...parent.querySelectorAll<HTMLButtonElement>(selector)].find(item => item.textContent.trim() === label);
    if (button === undefined || button.disabled) {
        throw new Error(`Expected enabled weight history action ${label}`);
    }
    return button;
}

function requiredHost(element: Element | null): HTMLElement {
    if (!(element instanceof HTMLElement)) {
        throw new Error('Expected the rendered weight history host');
    }
    return element;
}

function expectHttp(method: 'GET' | 'POST' | 'PUT', url: string): TestRequest {
    return TestBed.inject(HttpTestingController).expectOne(request => request.method === method && request.url === url, `${method} ${url}`);
}

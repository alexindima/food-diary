import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { afterEach, describe, expect, it, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { environment } from '../../../environments/environment';
import type { RecipeModerationItem } from '../../shared/api/sdk/generated/model/recipe-moderation-item';
import type { RecipeModerationPage } from '../../shared/api/sdk/generated/model/recipe-moderation-page';
import { AdminAuthService } from '../admin-auth/contracts/admin-session';
import { adminModerationRoutes } from './admin-moderation.routes';

const QUEUE_URL = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/moderation/recipes`;
const PAGE_SIZE = 20;
const FIRST_PAGE = 1;
const SECOND_PAGE = 2;
const QUEUE_COUNT_FIELDS = 3;
const APPLE = recipe('09ee5310-90c9-4198-83ad-330a236ef65f', 'Apple salad');
const BEAN = recipe('331961d5-2756-4f5b-a457-3dc7861403ba', 'Bean soup');

type QueueFixture = {
    harness: RouterTestingHarness;
    http: HttpTestingController;
    view: HTMLElement;
};

afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
});

describe('Reading the recipe moderation queue', () => {
    it('cancels an older queue read on refresh and recovers from a failed read', async () => {
        const { harness, http, view } = await renderQueueAsync();
        const initial = queueRequest(http);
        expect(view.querySelector('[role="status"]')?.textContent).toContain('ADMIN_COMMON.LOADING');

        button(view, 'ADMIN_RECIPE_MODERATION.REFRESH').click();
        harness.detectChanges();
        expect(initial.cancelled).toBe(true);
        queueRequest(http).flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
        await settleQueueAsync(harness);
        expect(view.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_COMMON.LOAD_ERROR');

        button(view, 'ADMIN_COMMON.RETRY').click();
        harness.detectChanges();
        queueRequest(http).flush(page([]));
        await settleQueueAsync(harness);
        expect(view.querySelector('tbody')?.textContent).toContain('ADMIN_COMMON.EMPTY');
        expect(view.querySelectorAll('.counts dd')).toHaveLength(QUEUE_COUNT_FIELDS);
        expect(view.querySelector('[role="alert"]')).toBeNull();
    });

    it('reconciles an out-of-range page and applies the actual status selection from page one', async () => {
        const { harness, http, view } = await renderQueueAsync();
        queueRequest(http).flush(page([APPLE], PAGE_SIZE + 1));
        await settleQueueAsync(harness);

        const next = required(view.querySelector<HTMLButtonElement>('fd-ui-pagination button:last-child'));
        expect(next.disabled).toBe(false);
        next.click();
        harness.detectChanges();
        queueRequest(http, SECOND_PAGE).flush({ ...page([]), page: SECOND_PAGE });
        harness.detectChanges();
        queueRequest(http).flush(page([BEAN]));
        await settleQueueAsync(harness);
        expect(view.querySelector('tbody')?.textContent).toContain('Bean soup');

        await chooseStatusAsync(harness, view, 'ADMIN_RECIPE_MODERATION.NEEDS_REVIEW');
        queueRequest(http, FIRST_PAGE, 'NeedsReview').flush(page([APPLE]));
        await settleQueueAsync(harness);
        expect(view.querySelector('tbody')?.textContent).toContain('Apple salad');
        expect(view.querySelector('tbody')?.textContent).not.toContain('Bean soup');

        await chooseStatusAsync(harness, view, 'ADMIN_COMMON.ALL');
        queueRequest(http).flush(page([APPLE, BEAN]));
        await settleQueueAsync(harness);
        expect(view.querySelectorAll('tbody tr')).toHaveLength(2);
    });

    it('focuses the detail panel and discards the previous read when the selected revision changes', async () => {
        const { harness, http, view } = await renderQueueAsync();
        await showRowsAsync(harness, http);
        detailsButton(view, 'Apple salad').click();
        harness.detectChanges();
        const firstDetail = detailsRequest(http, APPLE);
        const panel = detailPanel(view);
        expect(document.activeElement).toBe(panel);
        expect(panel.querySelector('[role="status"]')?.textContent).toContain('ADMIN_COMMON.LOADING');

        detailsButton(view, 'Bean soup').click();
        harness.detectChanges();
        expect(firstDetail.cancelled).toBe(true);
        detailsRequest(http, BEAN).flush({}, { status: HttpStatusCode.BadGateway, statusText: 'Unavailable' });
        await settleQueueAsync(harness);
        expect(panel.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_COMMON.LOAD_ERROR');

        button(panel, 'ADMIN_COMMON.RETRY').click();
        harness.detectChanges();
        detailsRequest(http, BEAN).flush(BEAN);
        await settleQueueAsync(harness);
        expect(panel.textContent).toContain(BEAN.revisionId);
        expect(panel.textContent).not.toContain(APPLE.revisionId);
    });
});

it('keeps a replacement detail read when a still-rendered Retry belongs to the failed revision', async () => {
    const { harness, http, view } = await renderQueueAsync();
    await showRowsAsync(harness, http);
    detailsButton(view, 'Apple salad').click();
    harness.detectChanges();
    detailsRequest(http, APPLE).flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
    await settleQueueAsync(harness);
    const panel = detailPanel(view);
    expect(panel.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_COMMON.LOAD_ERROR');
    const oldRetry = button(panel, 'ADMIN_COMMON.RETRY');

    detailsButton(view, 'Bean soup').click();
    expect(oldRetry.isConnected).toBe(true);
    expect(oldRetry.disabled).toBe(false);
    oldRetry.click();
    harness.detectChanges();

    const replacement = detailsRequest(http, BEAN);
    const extraReads = http.match(request => request.method === 'GET' && request.url.startsWith(`${QUEUE_URL}/`));
    expect({ cancelled: replacement.cancelled, extraReads: extraReads.map(read => read.request.url) }).toEqual({
        cancelled: false,
        extraReads: [],
    });
    replacement.flush(BEAN);
    await settleQueueAsync(harness);
    expect(panel.textContent).toContain(BEAN.revisionId);
    expect(panel.textContent).not.toContain(APPLE.revisionId);
    expect(panel.querySelector('[role="alert"]')).toBeNull();
});

describe('Submitting moderation decisions', () => {
    it('delivers a decision to the selected revision and reloads the queue and detail once', async () => {
        const { harness, http, view } = await renderQueueAsync();
        await showRowsAsync(harness, http);
        await selectAppleAsync(harness, http, view);
        const panel = detailPanel(view);
        const requestChanges = button(panel, 'ADMIN_RECIPE_MODERATION.REQUEST_CHANGES');
        expect(requestChanges.disabled).toBe(true);
        inputReason(panel, '   ');
        harness.detectChanges();
        expect(requestChanges.disabled).toBe(true);
        inputReason(panel, '  Please verify the listed ingredients.  ');
        harness.detectChanges();
        expect(requestChanges.disabled).toBe(false);

        requestChanges.click();
        requestChanges.click();
        const resolve = http.expectOne({ method: 'POST', url: `${QUEUE_URL}/${APPLE.revisionId}/resolve` });
        expect(resolve.request.body).toEqual({ approve: false, reason: 'Please verify the listed ingredients.' });
        resolve.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        harness.detectChanges();
        const changed = { ...APPLE, status: 'ChangesRequested', reason: 'Please verify the listed ingredients.' };
        queueRequest(http).flush(page([changed, BEAN]));
        detailsRequest(http, APPLE).flush(changed);
        await settleQueueAsync(harness);
        expect(panel.textContent).toContain('Please verify the listed ingredients.');
        expect(panel.querySelector('textarea')).toBeNull();
    });

    it('keeps a pending decision with its revision even when another row still accepts a native click', async () => {
        const { harness, http, view } = await renderQueueAsync();
        await showRowsAsync(harness, http);
        await selectAppleAsync(harness, http, view);
        const otherRevision = detailsButton(view, 'Bean soup');
        button(detailPanel(view), 'ADMIN_RECIPE_MODERATION.APPROVE').click();
        const resolve = http.expectOne({ method: 'POST', url: `${QUEUE_URL}/${APPLE.revisionId}/resolve` });
        expect(resolve.request.body).toEqual({ approve: true, reason: null });

        expect(otherRevision.disabled).toBe(false);
        otherRevision.click();
        harness.detectChanges();
        http.expectNone({ method: 'GET', url: `${QUEUE_URL}/${BEAN.revisionId}` });
        expect(otherRevision.disabled).toBe(true);

        resolve.flush({}, { status: HttpStatusCode.Conflict, statusText: 'Conflicting revision' });
        harness.detectChanges();
        queueRequest(http).flush(page([APPLE, BEAN]));
        detailsRequest(http, APPLE).flush(APPLE);
        await settleQueueAsync(harness);
        expect(detailPanel(view).textContent).toContain(APPLE.revisionId);
        expect(detailPanel(view).querySelector('[role="alert"]')?.textContent).toContain('ADMIN_RECIPE_MODERATION.STALE');
        detailsButton(view, 'Bean soup').click();
        harness.detectChanges();
        detailsRequest(http, BEAN).flush(BEAN);
        await settleQueueAsync(harness);
        expect(detailPanel(view).textContent).toContain(BEAN.revisionId);
        expect(detailPanel(view).querySelector('[role="alert"]')).toBeNull();
    });
});

it('allows retrying a delayed moderation check after a failed action', async () => {
    const { harness, http, view } = await renderQueueAsync();
    const delayed = { ...APPLE, status: 'CheckDelayed' };
    queueRequest(http).flush(page([delayed]));
    await settleQueueAsync(harness);
    detailsButton(view, 'Apple salad').click();
    harness.detectChanges();
    detailsRequest(http, delayed).flush(delayed);
    await settleQueueAsync(harness);

    const panel = detailPanel(view);
    button(panel, 'ADMIN_RECIPE_MODERATION.RETRY').click();
    http.expectOne({ method: 'POST', url: `${QUEUE_URL}/${APPLE.revisionId}/retry` }).flush(
        {},
        { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' },
    );
    harness.detectChanges();
    expect(panel.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_MODERATION.ACTION_ERROR');
    button(panel, 'ADMIN_RECIPE_MODERATION.RETRY').click();
    http.expectOne({ method: 'POST', url: `${QUEUE_URL}/${APPLE.revisionId}/retry` }).flush(null, {
        status: HttpStatusCode.NoContent,
        statusText: 'No Content',
    });
    const pending = { ...delayed, status: 'Pending' };
    harness.detectChanges();
    queueRequest(http).flush(page([pending]));
    detailsRequest(http, APPLE).flush(pending);
    await settleQueueAsync(harness);
    expect(panel.querySelector('[role="alert"]')).toBeNull();
    expect(panel.textContent).toContain('ADMIN_RECIPE_MODERATION.PENDING');
});

async function renderQueueAsync(): Promise<QueueFixture> {
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'moderation', children: adminModerationRoutes }]),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            {
                provide: AdminAuthService,
                useValue: {
                    applySsoFromQueryAsync: vi.fn<AdminAuthService['applySsoFromQueryAsync']>().mockResolvedValue(undefined),
                    sessionUnavailable: (): boolean => false,
                    isAuthenticated: (): boolean => true,
                    isAdmin: (): boolean => true,
                },
            },
        ],
    });
    const harness = await RouterTestingHarness.create('/moderation/recipes');
    return { harness, http: TestBed.inject(HttpTestingController), view: required(harness.routeNativeElement) };
}

function recipe(revisionId: string, name: string): RecipeModerationItem {
    return {
        revisionId,
        recipeId: '73db512b-35c5-4727-b457-e43dc0253321',
        ownerId: '1d1fe570-72e2-4e6a-922c-ecbea567fd66',
        text: `${name}\nIngredients and preparation`,
        status: 'NeedsReview',
        createdAt: '2026-11-19T09:00:00Z',
        policyVersionId: 'd6c0b820-eeb3-4a29-b11e-3f771b04a668',
        attempts: 1,
    };
}

function page(items: RecipeModerationItem[], totalItems = items.length): RecipeModerationPage {
    return { items, totalItems, page: FIRST_PAGE, limit: PAGE_SIZE, delayedCount: 0, pendingCount: 0, needsReviewCount: items.length };
}

function queueRequest(http: HttpTestingController, pageNumber = FIRST_PAGE, status: string | null = null): TestRequest {
    const request = http.expectOne(req => req.method === 'GET' && req.url === QUEUE_URL, 'read recipe moderation queue');
    expect(request.request.params.get('page')).toBe(String(pageNumber));
    expect(request.request.params.get('limit')).toBe(String(PAGE_SIZE));
    expect(request.request.params.get('status')).toBe(status);
    return request;
}

function detailsRequest(http: HttpTestingController, item: RecipeModerationItem): TestRequest {
    return http.expectOne({ method: 'GET', url: `${QUEUE_URL}/${item.revisionId}` });
}

async function settleQueueAsync(harness: RouterTestingHarness): Promise<void> {
    await harness.fixture.whenStable();
    harness.detectChanges();
}

async function showRowsAsync(harness: RouterTestingHarness, http: HttpTestingController): Promise<void> {
    queueRequest(http).flush(page([APPLE, BEAN]));
    await settleQueueAsync(harness);
}

async function selectAppleAsync(harness: RouterTestingHarness, http: HttpTestingController, view: HTMLElement): Promise<void> {
    detailsButton(view, 'Apple salad').click();
    harness.detectChanges();
    detailsRequest(http, APPLE).flush(APPLE);
    await settleQueueAsync(harness);
}

function detailsButton(view: HTMLElement, title: string): HTMLButtonElement {
    const row = required([...view.querySelectorAll('tbody tr')].find(entry => entry.querySelector('strong')?.textContent === title));
    return button(row, 'ADMIN_RECIPE_MODERATION.DETAILS');
}

function detailPanel(view: HTMLElement): HTMLElement {
    return required(view.querySelector<HTMLElement>('section[aria-label="ADMIN_RECIPE_MODERATION.DETAILS"]'));
}

function button(view: Element, text: string): HTMLButtonElement {
    return required([...view.querySelectorAll('button')].find(control => control.textContent.trim() === text));
}

function inputReason(panel: HTMLElement, text: string): void {
    const textarea = required(panel.querySelector('textarea'));
    textarea.value = text;
    textarea.dispatchEvent(new Event('input', { bubbles: true }));
}

async function chooseStatusAsync(harness: RouterTestingHarness, view: HTMLElement, name: string): Promise<void> {
    if (typeof Element.prototype.scrollIntoView !== 'function') {
        Object.defineProperty(Element.prototype, 'scrollIntoView', { configurable: true, value: vi.fn() });
        onTestFinished(() => {
            Reflect.deleteProperty(Element.prototype, 'scrollIntoView');
        });
    }
    required(view.querySelector<HTMLButtonElement>('fd-ui-select button[aria-haspopup="listbox"]')).click();
    await harness.fixture.whenStable();
    required([...document.querySelectorAll<HTMLElement>('[role="option"]')].find(option => option.textContent.trim() === name)).click();
    harness.detectChanges();
}

function required<T>(value: T | null | undefined): T {
    if (value === null || value === undefined) {
        throw new Error('Expected a rendered recipe moderation control');
    }
    return value;
}

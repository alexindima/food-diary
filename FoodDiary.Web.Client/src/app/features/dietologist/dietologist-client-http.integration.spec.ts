import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { afterEach, beforeEach, expect, it, onTestFinished, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import type { AttentionSignalHttpResponse } from '../../shared/api/sdk/generated/model/attention-signal-http-response';
import type { BulkRecommendationRecipientResultHttpResponse } from '../../shared/api/sdk/generated/model/bulk-recommendation-recipient-result-http-response';
import type { BulkRecommendationResultHttpResponse } from '../../shared/api/sdk/generated/model/bulk-recommendation-result-http-response';
import type { ClientTaskHttpResponse } from '../../shared/api/sdk/generated/model/client-task-http-response';
import type { RecommendationHttpResponse } from '../../shared/api/sdk/generated/model/recommendation-http-response';
import type { RecommendationTemplateHttpResponse } from '../../shared/api/sdk/generated/model/recommendation-template-http-response';
import { getStringProperty } from '../../shared/lib/unknown-value.utils';
import { entityId } from '../../shared/models/semantics/entity-id';
import { dietologistRoutes } from './dietologist.routes';
import { createClient } from './pages/clients/dietologist-clients-lib/dietologist-clients.test-data';

const BASE_URL = environment.apiUrls.dietologist;
const CLIENTS_API = `${BASE_URL}/clients`;
const CLIENTS_URL = '/dietologist';
const ATTENTION_API = `${CLIENTS_API}/attention`;
const TEMPLATES_API = `${BASE_URL}/recommendation-templates`;
const BULK_API = `${BASE_URL}/recommendations/bulk`;
const BULK_SECTION = '[aria-labelledby="bulk-recommendations-title"]';
const ATTENTION_SECTION = '[aria-labelledby="attention-title"]';
const TASK_SECTION = '[aria-labelledby="dietologist-client-tasks-title"]';
const TASK_CREATE = 'CLIENT_TASKS.FORM.CREATE';
const TASK_CANCEL = 'CLIENT_TASKS.CANCEL';
const TASK_OPEN = 'CLIENT_TASKS.STATUS.OPEN';
const SNOOZE = 'ATTENTION.SNOOZE';
const RECOMMENDATION_FORM = '.client-dashboard__recommendation-form';
const RECOMMENDATION_LIST = '.client-dashboard__recommendation-list';
const CREATED_AT = '2026-05-17T09:00:00.000Z';
const NOW = '2026-05-17T12:00:00.000Z';
const SNOOZED_UNTIL = '2026-05-24T12:00:00.000Z';
const CLIENT = createClient({
    firstName: 'Mira',
    permissions: { ...createClient().permissions, shareMeals: false, shareStatistics: true, shareGoals: true },
});
const SECOND_BULK_CLIENT = createClient({
    userId: entityId<'user'>('second-client'),
    firstName: 'Yuri',
    email: 'second-client@example.test',
});
const CLIENT_API = `${CLIENTS_API}/${CLIENT.userId}`;
const CLIENT_URL = `${CLIENTS_URL}/clients/${CLIENT.userId}`;
const TASKS_API = `${CLIENT_API}/tasks`;
const RECOMMENDATIONS_API = `${CLIENT_API}/recommendations`;
const ATTENTION_SIGNAL = {
    id: 'attention-1',
    clientUserId: CLIENT.userId,
    type: 'DiaryInactivity',
    severity: 'High',
    reason: 'NoRecentDiaryEntries',
    detectedAtUtc: CREATED_AT,
} satisfies AttentionSignalHttpResponse;
const TASK = {
    id: 'check-in-1',
    clientUserId: CLIENT.userId,
    dietologistUserId: 'dietologist-1',
    title: 'Check in after lunch',
    details: 'Review the last three meals.',
    dueAtUtc: null,
    status: 'Open',
    isOverdue: false,
    createdAtUtc: CREATED_AT,
} satisfies ClientTaskHttpResponse;
const OTHER_TASK = { ...TASK, id: 'check-in-2', title: 'Keep the activity reminder' };
const TEMPLATE = {
    id: 'protein-template',
    name: 'Protein guide',
    text: 'Include beans at lunch.',
    isArchived: false,
    createdAtUtc: CREATED_AT,
} satisfies RecommendationTemplateHttpResponse;
const RECOMMENDATION = {
    id: 'recommendation-1',
    dietologistUserId: TASK.dietologistUserId,
    text: TEMPLATE.text,
    isRead: false,
    createdAtUtc: CREATED_AT,
} satisfies RecommendationHttpResponse;

let http: HttpTestingController;
const toastSuccess = vi.fn<FdUiToastService['success']>();
const toastError = vi.fn<FdUiToastService['error']>();

beforeEach(() => {
    toastSuccess.mockClear();
    toastError.mockClear();
    TestBed.configureTestingModule({
        providers: [
            provideTranslateTesting(),
            provideRouter([{ path: 'dietologist', children: dietologistRoutes }]),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: FdUiToastService, useValue: { success: toastSuccess, error: toastError } },
        ],
    });
    http = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    TestBed.inject(FdUiDialogService).closeAll();
    vi.unstubAllGlobals();
    http.verify();
});

it('keeps an unresolved batch identity after confirming a different recommendation', async () => {
    const harness = await openBulkDirectoryAsync();
    const composer = element(render(harness), BULK_SECTION, HTMLElement);
    const draft = element(composer, 'textarea', HTMLTextAreaElement);
    const recipient = bulkCheckbox(composer, CLIENT.userId);
    const originalText = 'Review the last three meals.';
    const separateText = 'Add an afternoon walk.';

    recipient.click();
    setText(draft, originalText);
    const lost = sendBulkRecommendation(harness, composer);
    const originalKey = expectBulkRequest(lost, [CLIENT.userId], originalText);
    lost.error(new ProgressEvent('error'));
    harness.detectChanges();
    expect(toastError).toHaveBeenCalledExactlyOnceWith('BULK_RECOMMENDATIONS.ERROR');
    expect(recipient.checked).toBe(true);
    expect(draft.value).toBe(originalText);

    setText(draft, separateText);
    const separate = sendBulkRecommendation(harness, composer);
    const separateKey = expectBulkRequest(separate, [CLIENT.userId], separateText);
    expect(separateKey).not.toBe(originalKey);
    completeBulk(separate, separateKey, [deliveredBulkRecipient(CLIENT.userId)]);
    harness.detectChanges();
    expect(recipient.checked).toBe(false);
    expect(draft.value).toBe('');

    recipient.click();
    setText(draft, `  ${originalText}  `);
    const replay = sendBulkRecommendation(harness, composer);
    const replayKey = expectBulkRequest(replay, [CLIENT.userId], originalText);
    expect(replayKey).toBe(originalKey);
    completeBulk(replay, replayKey, [deliveredBulkRecipient(CLIENT.userId, true)]);
    harness.detectChanges();

    expect(recipient.checked).toBe(false);
    expect(draft.value).toBe('');
    expect(toastSuccess).toHaveBeenCalledTimes(2);

    recipient.click();
    setText(draft, originalText);
    const later = sendBulkRecommendation(harness, composer);
    const laterKey = expectBulkRequest(later, [CLIENT.userId], originalText);
    expect(laterKey).not.toBe(originalKey);
    completeBulk(later, laterKey, [deliveredBulkRecipient(CLIENT.userId)]);
});

it('reviews checked recipients and retries only the recipient who was not sent the recommendation', async () => {
    const harness = await openBulkDirectoryAsync();
    const composer = element(render(harness), BULK_SECTION, HTMLElement);
    const draft = element(composer, 'textarea', HTMLTextAreaElement);
    const first = bulkCheckbox(composer, CLIENT.userId);
    const second = bulkCheckbox(composer, SECOND_BULK_CLIENT.userId);
    const text = 'Keep a regular lunch schedule.';
    first.click();
    second.click();
    setText(draft, text);

    const review = openBulkReview(harness, composer);
    expect(review.textContent).toContain(CLIENT.firstName);
    expect(review.textContent).toContain(SECOND_BULK_CLIENT.firstName);
    expect(review.textContent).toContain(text);
    button(review, 'COMMON.CANCEL').click();
    harness.detectChanges();
    http.expectNone(BULK_API);
    expect(first.checked && second.checked).toBe(true);
    expect(draft.value).toBe(text);

    const initial = sendBulkRecommendation(harness, composer);
    const initialKey = expectBulkRequest(initial, [CLIENT.userId, SECOND_BULK_CLIENT.userId], text);
    harness.detectChanges();
    expect(first.disabled && second.disabled && draft.disabled).toBe(true);
    completeBulk(initial, initialKey, [
        deliveredBulkRecipient(CLIENT.userId),
        { clientUserId: SECOND_BULK_CLIENT.userId, succeeded: false, wasAlreadyProcessed: false, errorCode: 'ClientNotFound' },
    ]);
    harness.detectChanges();

    expect(toastError).toHaveBeenCalledExactlyOnceWith('BULK_RECOMMENDATIONS.PARTIAL_ERROR');
    expect(first.checked).toBe(false);
    expect(second.checked).toBe(true);
    expect(draft.value).toBe(text);
    const retry = sendBulkRecommendation(harness, composer);
    const retryKey = expectBulkRequest(retry, [SECOND_BULK_CLIENT.userId], text);
    expect(retryKey).not.toBe(initialKey);
    completeBulk(retry, retryKey, [deliveredBulkRecipient(SECOND_BULK_CLIENT.userId)]);
    harness.detectChanges();
    expect(second.checked).toBe(false);
    expect(draft.value).toBe('');
    expect(toastSuccess).toHaveBeenCalledExactlyOnceWith('BULK_RECOMMENDATIONS.SUCCESS');
});

it('applies the directory attention threshold and lets the dietologist retry a failed snooze', async () => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(NOW));
    onTestFinished(() => {
        vi.useRealTimers();
    });

    const harness = await RouterTestingHarness.create(CLIENTS_URL);
    firstPage(CLIENTS_API).flush([CLIENT]);
    attentionRequest('3').flush([]);
    const attention = element(render(harness), ATTENTION_SECTION, HTMLElement);
    element(attention, 'summary', HTMLElement).click();
    const inactivityDays = element(attention, '#attention-inactivity', HTMLInputElement);
    inactivityDays.value = '5';
    inactivityDays.dispatchEvent(new Event('change', { bubbles: true }));
    button(attention, 'ATTENTION.APPLY').click();
    harness.detectChanges();
    attentionRequest('5').flush([ATTENTION_SIGNAL]);
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(attention.textContent).toContain('ATTENTION.UNNAMED_CLIENT');
    expect(attention.textContent).toContain('ATTENTION.REASON.NoRecentDiaryEntries');
    expect(element(attention, 'time', HTMLTimeElement).dateTime).toBe(CREATED_AT);
    expect(attention.textContent).not.toContain(CLIENT.firstName);

    button(attention, SNOOZE).click();
    const request = snoozeRequest();
    harness.detectChanges();
    expect(button(attention, SNOOZE).disabled).toBe(true);
    unavailable(request);
    harness.detectChanges();

    expect(toastError).toHaveBeenCalledExactlyOnceWith('ATTENTION.ACTION_ERROR');
    expect(attention.textContent).toContain('ATTENTION.UNNAMED_CLIENT');
    expect(button(attention, SNOOZE).disabled).toBe(false);
    button(attention, SNOOZE).click();
    snoozeRequest().flush(null);
    harness.detectChanges();

    expect(attention.textContent).toContain('ATTENTION.EMPTY');
    expect(attention.querySelector('article')).toBeNull();
    expect(toastSuccess).toHaveBeenCalledExactlyOnceWith('ATTENTION.SNOOZE_SUCCESS');
});

it('does not request private client resources when the deep link is absent from accepted connections', async () => {
    const unlistedId = 'unlisted-client';
    const unlistedUrl = `${CLIENTS_URL}/clients/${unlistedId}`;
    const harness = await RouterTestingHarness.create(unlistedUrl);
    firstPage(CLIENTS_API).flush([CLIENT]);

    const dashboard = render(harness);
    expect(TestBed.inject(Router).url).toBe(unlistedUrl);
    expect(dashboard.textContent).toContain('DIETOLOGIST.CLIENT_DASHBOARD.NOT_FOUND');
    expect(dashboard.querySelector(TASK_SECTION)).toBeNull();
    http.expectNone(candidate => candidate.url.startsWith(`${CLIENTS_API}/${unlistedId}/`) || candidate.url === TEMPLATES_API);
});

it('keeps a failed reminder draft and applies the accepted task states only to its own row', async () => {
    const harness = await openDashboardAsync();
    const tasks = element(render(harness), TASK_SECTION, HTMLElement);
    const title = element(tasks, 'form input[type="text"]', HTMLInputElement);
    const details = element(tasks, 'form textarea', HTMLTextAreaElement);
    setText(title, `  ${TASK.title}  `);
    setText(details, `  ${TASK.details}  `);
    harness.detectChanges();

    button(tasks, TASK_CREATE).click();
    unavailable(createTaskRequest());
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(toastError).toHaveBeenCalledExactlyOnceWith('CLIENT_TASKS.CREATE_ERROR');
    });
    expect(title.value).toBe(`  ${TASK.title}  `);
    expect(details.value).toBe(`  ${TASK.details}  `);
    expect(tasks.textContent).not.toContain(TASK.title);

    button(tasks, TASK_CREATE).click();
    createTaskRequest().flush(TASK);
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(taskRow(tasks, TASK.title).textContent).toContain(TASK_OPEN);
    });
    expect(title.value).toBe('');
    expect(details.value).toBe('');
    expect(taskRow(tasks, TASK.title).textContent).toContain(TASK.details);
    expect(toastSuccess).toHaveBeenCalledExactlyOnceWith('CLIENT_TASKS.CREATED');

    button(taskRow(tasks, TASK.title), TASK_CANCEL).click();
    requestFor('PUT', `${CLIENTS_API}/tasks/${TASK.id}/cancel`).flush({
        ...TASK,
        status: 'Cancelled',
        statusChangedAtUtc: NOW,
    } satisfies ClientTaskHttpResponse);
    harness.detectChanges();

    const cancelled = taskRow(tasks, TASK.title);
    expect(cancelled.textContent).toContain('CLIENT_TASKS.STATUS.CANCELLED');
    expect(cancelled.querySelector('button')).toBeNull();
    expect(taskRow(tasks, OTHER_TASK.title).textContent).toContain(TASK_OPEN);
    expect(button(taskRow(tasks, OTHER_TASK.title), TASK_CANCEL).disabled).toBe(false);
});

it('cancels the pending reminder from the previous client and lets the next client save independently', async () => {
    const harness = await openDashboardAsync();
    const originalTasks = element(render(harness), TASK_SECTION, HTMLElement);
    setText(element(originalTasks, 'form input[type="text"]', HTMLInputElement), TASK.title);
    setText(element(originalTasks, 'form textarea', HTMLTextAreaElement), TASK.details);
    harness.detectChanges();
    button(originalTasks, TASK_CREATE).click();
    const previous = createTaskRequest();

    const replacement = createClient({
        userId: entityId<'user'>('replacement-client'),
        firstName: 'Nina',
        permissions: { ...createClient().permissions, shareMeals: false },
    });
    const replacementApi = `${CLIENTS_API}/${replacement.userId}`;
    await harness.navigateByUrl(`${CLIENTS_URL}/clients/${replacement.userId}`);
    firstPage(CLIENTS_API).flush([CLIENT, replacement]);
    firstPage(`${replacementApi}/recommendations`).flush([]);
    firstPage(`${replacementApi}/tasks`).flush([]);
    firstPage(TEMPLATES_API).flush([]);

    const dashboard = render(harness);
    expect(dashboard.querySelector('h1')?.textContent).toContain(replacement.firstName);
    expect(previous.cancelled).toBe(true);
    expect(toastError).not.toHaveBeenCalled();
    expect(toastSuccess).not.toHaveBeenCalled();

    const replacementTasks = element(dashboard, TASK_SECTION, HTMLElement);
    const title = element(replacementTasks, 'form input[type="text"]', HTMLInputElement);
    expect(title.value).toBe('');
    const nextTask = {
        ...TASK,
        id: 'replacement-task',
        clientUserId: replacement.userId,
        title: 'Review next week',
        details: null,
    } satisfies ClientTaskHttpResponse;
    setText(title, nextTask.title);
    harness.detectChanges();
    button(replacementTasks, TASK_CREATE).click();
    const pending = requestFor('POST', `${replacementApi}/tasks`);
    expect(pending.request.body).toEqual({ title: nextTask.title, details: null, dueAtUtc: null });
    pending.flush(nextTask);

    await vi.waitFor(() => {
        harness.detectChanges();
        expect(taskRow(replacementTasks, nextTask.title).textContent).toContain(TASK_OPEN);
    });
    expect(replacementTasks.textContent).not.toContain(TASK.title);
    expect(toastSuccess).toHaveBeenCalledExactlyOnceWith('CLIENT_TASKS.CREATED');
});

it('can reuse a stored recommendation and archive its template without removing the sent recommendation', async () => {
    const harness = await openDashboardAsync();
    const dashboard = render(harness);
    const composer = element(dashboard, RECOMMENDATION_FORM, HTMLFormElement);
    const message = element(composer, 'textarea', HTMLTextAreaElement);
    setText(message, `  ${TEMPLATE.text}  `);
    setText(element(composer, 'input[type="text"]', HTMLInputElement), `  ${TEMPLATE.name}  `);
    harness.detectChanges();

    button(composer, 'RECOMMENDATION_TEMPLATES.SAVE').click();
    const create = requestFor('POST', TEMPLATES_API);
    expect(create.request.body).toEqual({ name: TEMPLATE.name, text: TEMPLATE.text });
    create.flush(TEMPLATE);
    harness.detectChanges();

    setText(message, '');
    button(dashboard, TEMPLATE.name).click();
    harness.detectChanges();
    expect(message.value).toBe(TEMPLATE.text);
    button(composer, 'DIETOLOGIST.CLIENT_DASHBOARD.RECOMMENDATIONS.SEND').click();
    const send = requestFor('POST', RECOMMENDATIONS_API);
    expect(send.request.body).toEqual({ text: TEMPLATE.text });
    send.flush(RECOMMENDATION);
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(element(dashboard, RECOMMENDATION_LIST, HTMLElement).textContent).toContain(RECOMMENDATION.text);
    });
    expect(message.value).toBe('');

    button(dashboard, 'RECOMMENDATION_TEMPLATES.ARCHIVE_SHORT').click();
    requestFor('DELETE', `${TEMPLATES_API}/${TEMPLATE.id}`).flush(null);
    harness.detectChanges();

    expect(dashboard.textContent).toContain('RECOMMENDATION_TEMPLATES.EMPTY');
    expect(element(dashboard, RECOMMENDATION_LIST, HTMLElement).textContent).toContain(RECOMMENDATION.text);
});

function requestFor(method: string, url: string): TestRequest {
    return http.expectOne(request => request.method === method && request.url === url, `${method} ${url}`);
}

async function openBulkDirectoryAsync(): Promise<RouterTestingHarness> {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    const harness = await RouterTestingHarness.create(CLIENTS_URL);
    firstPage(CLIENTS_API).flush([CLIENT, SECOND_BULK_CLIENT]);
    attentionRequest('3').flush([]);
    harness.detectChanges();
    return harness;
}

function bulkCheckbox(composer: ParentNode, clientId: string): HTMLInputElement {
    return element(composer, `#bulk-client-${clientId}`, HTMLInputElement);
}

function openBulkReview(harness: RouterTestingHarness, composer: ParentNode): HTMLElement {
    harness.detectChanges();
    button(composer, 'BULK_RECOMMENDATIONS.SEND').click();
    harness.detectChanges();
    http.expectNone(BULK_API);
    return element(document, 'fd-bulk-recommendation-review', HTMLElement);
}

function sendBulkRecommendation(harness: RouterTestingHarness, composer: ParentNode): TestRequest {
    button(openBulkReview(harness, composer), 'BULK_RECOMMENDATIONS.CONFIRM').click();
    return requestFor('POST', BULK_API);
}

function expectBulkRequest(request: TestRequest, clientUserIds: string[], text: string): string {
    const key = getStringProperty(request.request.body, 'idempotencyKey');
    if (key === undefined || key.length === 0) {
        throw new Error('Expected the bulk recommendation request identity');
    }
    expect(request.request.body).toEqual({ clientUserIds, text, idempotencyKey: key });
    return key;
}

function deliveredBulkRecipient(clientUserId: string, wasAlreadyProcessed = false): BulkRecommendationRecipientResultHttpResponse {
    return { clientUserId, succeeded: true, wasAlreadyProcessed, recommendationId: `confirmed-${clientUserId}` };
}

function completeBulk(request: TestRequest, idempotencyKey: string, recipients: BulkRecommendationRecipientResultHttpResponse[]): void {
    request.flush({ idempotencyKey, recipients } satisfies BulkRecommendationResultHttpResponse);
}

function firstPage(url: string): TestRequest {
    const request = requestFor('GET', url);
    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('limit')).toBe('100');
    return request;
}

function attentionRequest(inactivityDays: string): TestRequest {
    const request = requestFor('GET', ATTENTION_API);
    expect(request.request.params.get('inactivityDays')).toBe(inactivityDays);
    expect(request.request.params.get('calorieDeviationPercent')).toBe('25');
    expect(request.request.params.get('sustainedDays')).toBe('3');
    expect(request.request.params.get('weightChangePercent')).toBe('3');
    expect(request.request.params.get('lookbackDays')).toBe('14');
    return request;
}

function snoozeRequest(): TestRequest {
    const request = requestFor('PUT', `${ATTENTION_API}/${ATTENTION_SIGNAL.id}/state`);
    expect(request.request.body).toEqual({
        clientUserId: CLIENT.userId,
        action: 'Snooze',
        snoozedUntilUtc: SNOOZED_UNTIL,
    });
    return request;
}

function createTaskRequest(): TestRequest {
    const request = requestFor('POST', TASKS_API);
    expect(request.request.body).toEqual({ title: TASK.title, details: TASK.details, dueAtUtc: null });
    return request;
}

function unavailable(request: TestRequest): void {
    request.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
}

async function openDashboardAsync(): Promise<RouterTestingHarness> {
    const harness = await RouterTestingHarness.create(CLIENT_URL);
    firstPage(CLIENTS_API).flush([CLIENT]);
    harness.detectChanges();
    flushDashboard();
    requestFor('GET', `${CLIENT_API}/goals`).flush({ id: CLIENT.userId, dailyCalorieTarget: 2100 });
    firstPage(RECOMMENDATIONS_API).flush([]);
    firstPage(TASKS_API).flush([OTHER_TASK]);
    const templates = firstPage(TEMPLATES_API);
    expect(templates.request.params.get('search')).toBe('');
    expect(templates.request.params.get('includeArchived')).toBe('false');
    templates.flush([]);

    expect(render(harness).querySelector('h1')?.textContent).toContain(CLIENT.firstName);
    return harness;
}

function flushDashboard(): void {
    const request = requestFor('GET', `${CLIENT_API}/dashboard`);
    const params = request.request.params;
    const from = params.get('dateFrom');
    const to = params.get('dateTo');
    expect(from).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    expect(to).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    request.flush({
        date: from,
        dateTo: to,
        dailyGoal: 2100,
        weeklyCalorieGoal: 14700,
        statistics: { totalCalories: 0, averageProteins: 0, averageFats: 0, averageCarbs: 0, averageFiber: 0 },
        weeklyCalories: [],
        weight: { latest: null, previous: null, desiredWeightKg: null },
        waist: { latest: null, previous: null, desiredWaistCm: null },
        meals: { items: [], total: 0 },
    });
}

function render(harness: RouterTestingHarness): HTMLElement {
    harness.detectChanges();
    if (harness.routeNativeElement === null) {
        throw new Error('Expected the Dietologist route to render');
    }
    return harness.routeNativeElement;
}

function button(host: ParentNode, label: string): HTMLButtonElement {
    const match = Array.from(host.querySelectorAll('button')).find(candidate => candidate.textContent.trim() === label);
    if (match === undefined) {
        throw new Error(`Expected the Dietologist action «${label}»`);
    }
    return match;
}

function taskRow(host: ParentNode, title: string): HTMLElement {
    const match = Array.from(host.querySelectorAll('article')).find(candidate => candidate.querySelector('strong')?.textContent === title);
    if (match === undefined) {
        throw new Error(`Expected the reminder «${title}»`);
    }
    return match;
}

function element<T extends HTMLElement>(host: ParentNode, selector: string, type: new () => T): T {
    const match = host.querySelector(selector);
    if (!(match instanceof type)) {
        throw new Error(`Expected the Dietologist element ${selector}`);
    }
    return match;
}

function setText(control: HTMLInputElement | HTMLTextAreaElement, value: string): void {
    control.value = value;
    control.dispatchEvent(new Event('input', { bubbles: true }));
}

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { afterEach, describe, expect, it } from 'vitest';

import { environment } from '../../../environments/environment';
import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { COLLECTION_PAGE_SIZE } from '../../shared/api/load-paged-collection';
import type { ClientTaskHttpResponse } from '../../shared/api/sdk/generated/model/client-task-http-response';
import type { RecommendationCommentHttpResponse } from '../../shared/api/sdk/generated/model/recommendation-comment-http-response';
import type { RecommendationCommentHttpResponsePagedHttpResponse } from '../../shared/api/sdk/generated/model/recommendation-comment-http-response-paged-http-response';
import type { RecommendationHttpResponse } from '../../shared/api/sdk/generated/model/recommendation-http-response';
import recommendationsRoutes from './recommendations.routes';

const RECOMMENDATIONS_PATH = '/recommendations';
const RECOMMENDATIONS_URL = environment.apiUrls.recommendations;
const TASKS_URL = environment.apiUrls.clientTasks;
const DIETOLOGIST_ID = 'fb6fc74b-9645-42a9-a3bc-48d30e21c38d';
const CLIENT_ID = '72ba5e43-e29e-4b21-82e8-72956044a11e';
const FIRST_RECOMMENDATION_ID = '090324f0-eec0-4e5d-901a-8bda90381c46';
const SECOND_RECOMMENDATION_ID = 'ff504da4-61fc-4a97-a9c9-11dbd5ba7420';
const FIRST_TASK_ID = '4d275748-886b-434d-8329-0c14ffdfe8c0';
const SECOND_TASK_ID = '4658f792-28ca-4107-8023-d30a70047703';
const COMMENT_ID = '194ca0f0-2c2d-47d9-8ae1-000000000001';
const TIMESTAMP = '2026-05-01T10:15:00Z';
const FIRST_RECOMMENDATION_TEXT = 'Include fruit in breakfast.';
const SECOND_RECOMMENDATION_TEXT = 'Plan something easy for dinner.';
const FIRST_TASK_TITLE = 'Log breakfast today';
const SECOND_TASK_TITLE = 'Plan an afternoon snack';
const COMPLETE_LABEL = 'CLIENT_TASKS.COMPLETE';
const REOPEN_LABEL = 'CLIENT_TASKS.REOPEN';
const COMMENT_PAGE_SIZE = 50;

afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
});

async function renderRecommendationsAsync(url = RECOMMENDATIONS_PATH): Promise<RouterTestingHarness> {
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'recommendations', children: recommendationsRoutes }], withComponentInputBinding()),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
        ],
    });

    return RouterTestingHarness.create(url);
}

function view(harness: RouterTestingHarness): HTMLElement {
    TestBed.tick();
    harness.detectChanges();
    const root = harness.routeNativeElement;
    if (!(root instanceof HTMLElement)) {
        throw new Error('Expected the recommendations route to be rendered');
    }
    return root;
}

function button(root: ParentNode, text: string): HTMLButtonElement {
    const action = Array.from(root.querySelectorAll('button')).find(candidate => candidate.textContent.includes(text));
    if (action === undefined) {
        throw new Error(`Expected an action containing ${text}`);
    }
    return action;
}

function taskRow(root: HTMLElement, title: string): HTMLElement {
    const task = Array.from(root.querySelectorAll<HTMLElement>('.recommendations-page__task')).find(candidate =>
        candidate.textContent.includes(title),
    );
    if (task === undefined) {
        throw new Error(`Expected a task with title ${title}`);
    }
    return task;
}

function expectList(http: HttpTestingController, url: string): TestRequest {
    const request = http.expectOne(
        candidate => candidate.method === 'GET' && candidate.url === url && candidate.params.get('page') === '1',
    );
    expect(request.request.params.get('limit')).toBe(String(COLLECTION_PAGE_SIZE));
    return request;
}

function loadRecommendations(http: HttpTestingController): void {
    const author = { dietologistUserId: DIETOLOGIST_ID, createdAtUtc: TIMESTAMP, isRead: false };
    expectList(http, RECOMMENDATIONS_URL).flush([
        { ...author, id: FIRST_RECOMMENDATION_ID, text: FIRST_RECOMMENDATION_TEXT },
        { ...author, id: SECOND_RECOMMENDATION_ID, text: SECOND_RECOMMENDATION_TEXT },
    ] satisfies RecommendationHttpResponse[]);
}

function expectComments(http: HttpTestingController, recommendationId: string): TestRequest {
    const url = `${RECOMMENDATIONS_URL}/${recommendationId}/comments`;
    return http.expectOne(request => request.method === 'GET' && request.url === url && request.params.get('page') === '1');
}

function deliverComments(request: TestRequest, data: RecommendationCommentHttpResponse[]): void {
    request.flush({
        data,
        page: 1,
        limit: COMMENT_PAGE_SIZE,
        totalPages: 1,
        totalItems: data.length,
    } satisfies RecommendationCommentHttpResponsePagedHttpResponse);
}

const firstTask = {
    id: FIRST_TASK_ID,
    clientUserId: CLIENT_ID,
    dietologistUserId: DIETOLOGIST_ID,
    title: FIRST_TASK_TITLE,
    createdAtUtc: TIMESTAMP,
    dueAtUtc: TIMESTAMP,
    status: 'Open',
    isOverdue: true,
} satisfies ClientTaskHttpResponse;

const secondTask = {
    ...firstTask,
    id: SECOND_TASK_ID,
    title: SECOND_TASK_TITLE,
    dueAtUtc: null,
    status: 'Completed',
    isOverdue: false,
} satisfies ClientTaskHttpResponse;

describe('Recommendation discussions on the lazy route', () => {
    it('switches from a deep-linked unread discussion without leaving its old request active', async () => {
        const harness = await renderRecommendationsAsync(
            `${RECOMMENDATIONS_PATH}?recommendationId=${FIRST_RECOMMENDATION_ID}&recommendationId=${SECOND_RECOMMENDATION_ID}`,
        );
        const http = TestBed.inject(HttpTestingController);
        expectList(http, TASKS_URL).flush([]);
        loadRecommendations(http);
        await waitForAsyncTasksAsync();
        const root = view(harness);

        const firstCard = button(root, FIRST_RECOMMENDATION_TEXT);
        const secondCard = button(root, SECOND_RECOMMENDATION_TEXT);
        expect(firstCard.getAttribute('aria-expanded')).toBe('true');
        expect(firstCard.textContent).toContain('RECOMMENDATIONS.STATUS_MARKING');
        expect(secondCard.getAttribute('aria-expanded')).toBe('false');
        const firstComments = expectComments(http, FIRST_RECOMMENDATION_ID);
        const firstRead = http.expectOne({ method: 'PUT', url: `${RECOMMENDATIONS_URL}/${FIRST_RECOMMENDATION_ID}/read` });

        secondCard.click();
        view(harness);

        expect(firstComments.cancelled).toBe(true);
        expect(firstCard.getAttribute('aria-expanded')).toBe('false');
        expect(secondCard.getAttribute('aria-expanded')).toBe('true');
        expect(root.querySelectorAll('fd-recommendation-thread')).toHaveLength(1);
        firstRead.flush(null);
        view(harness);
        expect(firstCard.textContent).toContain('RECOMMENDATIONS.STATUS_READ');
        expect(secondCard.textContent).toContain('RECOMMENDATIONS.STATUS_MARKING');

        http.expectOne({ method: 'PUT', url: `${RECOMMENDATIONS_URL}/${SECOND_RECOMMENDATION_ID}/read` }).flush(null);
        deliverComments(expectComments(http, SECOND_RECOMMENDATION_ID), []);
        await waitForAsyncTasksAsync();
        view(harness);
        expect(secondCard.textContent).toContain('RECOMMENDATIONS.STATUS_READ');
        expect(root.querySelector('fd-recommendation-thread')?.textContent).toContain('RECOMMENDATIONS.DISCUSSION.EMPTY');

        const composer = root.querySelector('textarea');
        if (composer === null) {
            throw new Error('Expected the active discussion composer');
        }
        composer.value = '  Thank you for the dinner suggestion.  ';
        composer.dispatchEvent(new Event('input', { bubbles: true }));
        view(harness);
        button(root, 'RECOMMENDATIONS.DISCUSSION.SEND').click();
        const post = http.expectOne({ method: 'POST', url: `${RECOMMENDATIONS_URL}/${SECOND_RECOMMENDATION_ID}/comments` });
        expect(post.request.body).toEqual({ text: 'Thank you for the dinner suggestion.' });
        post.flush({
            id: COMMENT_ID,
            recommendationId: SECOND_RECOMMENDATION_ID,
            authorUserId: CLIENT_ID,
            authorEmail: 'client@example.test',
            createdAtUtc: TIMESTAMP,
            text: 'Thank you for the dinner suggestion.',
        } satisfies RecommendationCommentHttpResponse);
        view(harness);

        expect(root.querySelector('fd-recommendation-thread')?.textContent).toContain('client@example.test');
        expect(root.querySelector('fd-recommendation-thread')?.textContent).toContain('Thank you for the dinner suggestion.');
        expect(composer.value).toBe('');

        await harness.navigateByUrl(RECOMMENDATIONS_PATH);
        expect(view(harness).querySelector('fd-recommendation-thread')).toBeNull();
    });
});

describe('Recommendation tasks on the lazy route', () => {
    it('submits a repeated task action once and keeps the response attached to its row', async () => {
        const harness = await renderRecommendationsAsync();
        const http = TestBed.inject(HttpTestingController);
        expectList(http, RECOMMENDATIONS_URL).flush([]);
        expectList(http, TASKS_URL).flush([firstTask, secondTask]);
        await waitForAsyncTasksAsync();
        const root = view(harness);
        const firstRow = taskRow(root, FIRST_TASK_TITLE);
        const secondRow = taskRow(root, SECOND_TASK_TITLE);
        expect(firstRow.textContent).toContain('CLIENT_TASKS.OVERDUE');
        expect(root.textContent).toContain('RECOMMENDATIONS.EMPTY_TITLE');

        const completeButton = button(firstRow, COMPLETE_LABEL);
        completeButton.click();
        completeButton.click();
        view(harness);
        expect(button(firstRow, COMPLETE_LABEL).disabled).toBe(true);
        expect(button(secondRow, REOPEN_LABEL).disabled).toBe(false);
        const complete = http.expectOne({ method: 'PUT', url: `${TASKS_URL}/${FIRST_TASK_ID}/status` });
        expect(complete.request.body).toEqual({ status: 'Completed' });

        button(secondRow, REOPEN_LABEL).click();
        view(harness);
        const reopen = http.expectOne({ method: 'PUT', url: `${TASKS_URL}/${SECOND_TASK_ID}/status` });
        expect(reopen.request.body).toEqual({ status: 'Open' });
        reopen.flush({ ...secondTask, status: 'Open', details: 'Pack a snack before leaving.' } satisfies ClientTaskHttpResponse);
        view(harness);

        expect(secondRow.textContent).toContain('Pack a snack before leaving.');
        expect(button(secondRow, COMPLETE_LABEL).disabled).toBe(false);
        expect(button(firstRow, COMPLETE_LABEL).disabled).toBe(true);
        expect(firstRow.textContent).toContain('CLIENT_TASKS.OVERDUE');

        complete.flush({ ...firstTask, status: 'Completed', isOverdue: false } satisfies ClientTaskHttpResponse);
        view(harness);
        expect(firstRow.textContent).not.toContain('CLIENT_TASKS.OVERDUE');
        expect(button(firstRow, REOPEN_LABEL).disabled).toBe(false);
        expect(button(secondRow, COMPLETE_LABEL).disabled).toBe(false);
    });
});

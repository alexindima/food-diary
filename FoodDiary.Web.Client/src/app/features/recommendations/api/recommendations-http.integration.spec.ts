import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, tap } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { COLLECTION_PAGE_SIZE } from '../../../shared/api/load-paged-collection';
import type { ApiErrorHttpResponse } from '../../../shared/api/sdk/generated/model/api-error-http-response';
import type { ClientTaskHttpResponse } from '../../../shared/api/sdk/generated/model/client-task-http-response';
import type { RecommendationCommentHttpResponse } from '../../../shared/api/sdk/generated/model/recommendation-comment-http-response';
import type { RecommendationCommentHttpResponsePagedHttpResponse } from '../../../shared/api/sdk/generated/model/recommendation-comment-http-response-paged-http-response';
import type { RecommendationHttpResponse } from '../../../shared/api/sdk/generated/model/recommendation-http-response';
import type { RecommendationComment } from '../../../shared/models/dietologist.data';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { ClientTasksService } from './client-tasks.service';
import { RecommendationsService } from './recommendations.service';

const RECOMMENDATION_ID = entityId<'recommendation'>('090324f0-eec0-4e5d-901a-8bda90381c46');
const TASK_ID = entityId<'client-task'>('4d275748-886b-434d-8329-0c14ffdfe8c0');
const CLIENT_ID = entityId<'user'>('72ba5e43-e29e-4b21-82e8-72956044a11e');
const DIETOLOGIST_ID = entityId<'user'>('fb6fc74b-9645-42a9-a3bc-48d30e21c38d');
const RECOMMENDATIONS_URL = environment.apiUrls.recommendations;
const COMMENTS_URL = `${RECOMMENDATIONS_URL}/${RECOMMENDATION_ID}/comments`;
const TASKS_URL = environment.apiUrls.clientTasks;
const COMMENTS_PAGE_SIZE = 50;
const UUID_SUFFIX_LENGTH = 12;
const LAST_ORDER = COMMENTS_PAGE_SIZE + 1;
const CREATED_AT = '2026-05-01T10:15:00Z';
const DUE_AT = '2026-05-06T09:00:00Z';
const CHANGED_AT = '2026-05-05T09:30:00Z';
const POSTED_TEXT = 'I can try this for breakfast tomorrow.';
const INACCESSIBLE = { error: 'Dietologist.InvitationNotFound' } satisfies ApiErrorHttpResponse;

let recommendations: RecommendationsService;
let tasks: ClientTasksService;
let http: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    recommendations = TestBed.inject(RecommendationsService);
    tasks = TestBed.inject(ClientTasksService);
    http = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    http.verify();
});

function makeComment(
    order: number,
): Required<Pick<RecommendationCommentHttpResponse, 'id' | 'recommendationId' | 'authorUserId' | 'text' | 'createdAtUtc'>> {
    return {
        id: `194ca0f0-2c2d-47d9-8ae1-${String(order).padStart(UUID_SUFFIX_LENGTH, '0')}`,
        recommendationId: RECOMMENDATION_ID,
        authorUserId: CLIENT_ID,
        text: `Update ${order}`,
        createdAtUtc: CREATED_AT,
    } satisfies RecommendationCommentHttpResponse;
}

function expectCommentPage(page: number): TestRequest {
    const request = http.expectOne(
        candidate => candidate.url === COMMENTS_URL && candidate.method === 'GET' && candidate.params.get('page') === String(page),
    );
    expect(request.request.params.get('limit')).toBe(String(COMMENTS_PAGE_SIZE));
    return request;
}

function sendCommentPage(page: number, data: RecommendationCommentHttpResponse[], totalPages = 2, totalItems = LAST_ORDER): void {
    const response = {
        data,
        page,
        limit: COMMENTS_PAGE_SIZE,
        totalPages,
        totalItems,
    } satisfies RecommendationCommentHttpResponsePagedHttpResponse;
    expectCommentPage(page).flush(response);
}

const FIRST_PAGE = Array.from({ length: COMMENTS_PAGE_SIZE }, (_, index) => makeComment(index + 1));

describe('Recommendation thread HTTP pagination', () => {
    it('resolves a new thread without requesting a second page', async () => {
        const pending = firstValueFrom(recommendations.getComments(RECOMMENDATION_ID));
        sendCommentPage(1, [], 0, 0);

        await expect(pending).resolves.toEqual([]);
        http.expectNone(candidate => candidate.url === COMMENTS_URL);
    });

    it('returns the complete thread in page order and fills redacted author fields', async () => {
        const published = vi.fn<(items: RecommendationComment[]) => void>();
        const pending = firstValueFrom(recommendations.getComments(RECOMMENDATION_ID).pipe(tap(published)));
        sendCommentPage(1, FIRST_PAGE);
        expect(published).not.toHaveBeenCalled();

        const last = { ...makeComment(LAST_ORDER), authorEmail: 'client@example.test' };
        sendCommentPage(2, [last]);
        const result = await pending;
        expect(result.map(comment => comment.id)).toEqual([...FIRST_PAGE.map(comment => comment.id), last.id]);
        expect(result[0]).toMatchObject({ authorFirstName: null, authorLastName: null, authorEmail: null });
        expect(result.at(-1)).toEqual({ ...last, authorFirstName: null, authorLastName: null });
        http.expectNone(candidate => candidate.url === COMMENTS_URL);
    });

    it('keeps partial comments unpublished after a failed page and restarts at page one', async () => {
        const published = vi.fn<(items: RecommendationComment[]) => void>();
        const pending = firstValueFrom(recommendations.getComments(RECOMMENDATION_ID).pipe(tap(published)));
        sendCommentPage(1, FIRST_PAGE);
        expectCommentPage(2).flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.ServiceUnavailable });
        expect(published).not.toHaveBeenCalled();
        const retry = firstValueFrom(recommendations.getComments(RECOMMENDATION_ID));
        sendCommentPage(1, FIRST_PAGE);
        sendCommentPage(2, [makeComment(LAST_ORDER)]);
        await expect(retry).resolves.toHaveLength(LAST_ORDER);
    });

    it('cancels the outstanding page when the caller leaves the thread', () => {
        const published = vi.fn<(items: RecommendationComment[]) => void>();
        const subscription = recommendations.getComments(RECOMMENDATION_ID).subscribe(published);
        sendCommentPage(1, FIRST_PAGE);
        const secondPage = expectCommentPage(2);

        subscription.unsubscribe();

        expect(secondPage.cancelled).toBe(true);
        expect(published).not.toHaveBeenCalled();
        http.expectNone(candidate => candidate.url === COMMENTS_URL);
    });
});

describe('Recommendation thread comment writes', () => {
    it('posts only the comment text to the selected recommendation and returns saved author metadata', async () => {
        const pending = firstValueFrom(recommendations.createComment(RECOMMENDATION_ID, { text: POSTED_TEXT }));
        const write = http.expectOne({ method: 'POST', url: COMMENTS_URL });
        expect(write.request.body).toEqual({ text: POSTED_TEXT });
        const saved = {
            ...makeComment(LAST_ORDER),
            authorUserId: DIETOLOGIST_ID,
            authorFirstName: 'Ada',
            authorLastName: 'Lovelace',
            authorEmail: 'ada@example.test',
            text: POSTED_TEXT,
        } satisfies RecommendationCommentHttpResponse;
        write.flush(saved, { status: HttpStatusCode.Created, statusText: 'Created' });

        await expect(pending).resolves.toEqual({
            ...saved,
            id: entityId<'recommendation-comment'>(saved.id),
            createdAtUtc: utcInstant(CREATED_AT),
        });
    });

    it('propagates a thread that stopped being accessible before submission', async () => {
        const pending = firstValueFrom(recommendations.createComment(RECOMMENDATION_ID, { text: POSTED_TEXT }));
        http.expectOne({ method: 'POST', url: COMMENTS_URL }).flush(INACCESSIBLE, {
            status: HttpStatusCode.NotFound,
            statusText: 'Not Found',
        });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.NotFound, error: INACCESSIBLE });
        http.expectNone(candidate => candidate.url === COMMENTS_URL);
    });
});

const OPEN_TASK = {
    id: TASK_ID,
    clientUserId: CLIENT_ID,
    dietologistUserId: DIETOLOGIST_ID,
    title: 'Log breakfast before noon',
    dueAtUtc: DUE_AT,
    status: 'Open',
    isOverdue: false,
    createdAtUtc: CREATED_AT,
} satisfies ClientTaskHttpResponse;

describe('Recommendation history HTTP contract', () => {
    it('keeps a read recommendation when the author has no visible profile names', async () => {
        const pending = firstValueFrom(recommendations.getMyRecommendations());
        const read = {
            id: RECOMMENDATION_ID,
            dietologistUserId: DIETOLOGIST_ID,
            text: 'Set a small goal for the coming week.',
            isRead: true,
            createdAtUtc: CREATED_AT,
            readAtUtc: CHANGED_AT,
        } satisfies RecommendationHttpResponse;
        const request = http.expectOne(candidate => candidate.method === 'GET' && candidate.url === RECOMMENDATIONS_URL);
        request.flush([read]);

        await expect(pending).resolves.toEqual([
            {
                ...read,
                createdAtUtc: utcInstant(CREATED_AT),
                readAtUtc: utcInstant(CHANGED_AT),
                dietologistFirstName: null,
                dietologistLastName: null,
            },
        ]);
    });
});

describe('Client task HTTP contract', () => {
    it('loads assigned tasks with nullable status metadata and an unchanged due instant', async () => {
        const requestPending = firstValueFrom(tasks.getMyTasks());
        const request = http.expectOne(candidate => candidate.method === 'GET' && candidate.url === TASKS_URL);
        expect(request.request.params.get('page')).toBe('1');
        expect(request.request.params.get('limit')).toBe(String(COLLECTION_PAGE_SIZE));
        const withoutDeadline = {
            ...OPEN_TASK,
            id: entityId<'client-task'>('4658f792-28ca-4107-8023-d30a70047703'),
            title: 'Try another breakfast when convenient',
            dueAtUtc: null,
        } satisfies ClientTaskHttpResponse;
        request.flush([OPEN_TASK, withoutDeadline]);

        await expect(requestPending).resolves.toEqual([
            { ...OPEN_TASK, dueAtUtc: utcInstant(DUE_AT), createdAtUtc: utcInstant(CREATED_AT), statusChangedAtUtc: null, details: null },
            { ...withoutDeadline, createdAtUtc: utcInstant(CREATED_AT), statusChangedAtUtc: null, details: null },
        ]);
        http.expectNone(candidate => candidate.url === TASKS_URL);
    });

    it.each(['Completed', 'Open'] as const)('writes the task status %s and returns the server timestamp', async status => {
        const pending = firstValueFrom(tasks.changeStatus(TASK_ID, status));
        const request = http.expectOne({ method: 'PUT', url: `${TASKS_URL}/${TASK_ID}/status` });
        expect(request.request.body).toEqual({ status });
        const updated = {
            ...OPEN_TASK,
            status,
            details: 'Include a drink if possible.',
            statusChangedAtUtc: CHANGED_AT,
        } satisfies ClientTaskHttpResponse;
        request.flush(updated);

        await expect(pending).resolves.toEqual({
            ...updated,
            createdAtUtc: utcInstant(CREATED_AT),
            dueAtUtc: utcInstant(DUE_AT),
            statusChangedAtUtc: utcInstant(CHANGED_AT),
        });
    });

    it('propagates a removed assignment when marking a task complete', async () => {
        const pending = firstValueFrom(tasks.changeStatus(TASK_ID, 'Completed'));
        http.expectOne({ method: 'PUT', url: `${TASKS_URL}/${TASK_ID}/status` }).flush(INACCESSIBLE, {
            status: HttpStatusCode.NotFound,
            statusText: 'Not Found',
        });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.NotFound, error: INACCESSIBLE });
    });
});

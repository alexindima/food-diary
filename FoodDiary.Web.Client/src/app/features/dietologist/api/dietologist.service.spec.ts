import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { DietologistRelationshipHttpResponse } from '../../../shared/api/sdk/generated/model/dietologist-relationship-http-response';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { DietologistService } from './dietologist.service';

const BASE_URL = environment.apiUrls.dietologist;
const PERMISSIONS = {
    shareMeals: false,
    shareStatistics: true,
    shareWeight: true,
    shareWaist: true,
    shareGoals: false,
    shareHydration: true,
    shareProfile: true,
    shareFasting: false,
};
const INVITE_PERMISSIONS = {
    shareMeals: true,
    shareStatistics: true,
    shareWeight: false,
    shareWaist: true,
    shareGoals: true,
    shareHydration: false,
    shareProfile: true,
    shareFasting: true,
};
const LOCAL_DATE_YEAR = 2026;
const MAY_MONTH_INDEX = 4;
const PERIOD_START_DAY = 17;
const PERIOD_END_DAY = 23;
const COLLECTION_PAGE_LIMIT = 100;
const NO_MEALS_DASHBOARD_RESPONSE = {
    date: '2026-05-17',
    dateTo: '2026-05-23',
    dailyGoal: 2000,
    weeklyCalorieGoal: 14000,
    statistics: { totalCalories: 0, averageProteins: 0, averageFats: 0, averageCarbs: 0, averageFiber: 0 },
    weeklyCalories: [],
    weight: { latest: null, previous: null, desiredWeightKg: null },
    waist: { latest: null, previous: null, desiredWaistCm: null },
    meals: { items: [], total: 0 },
};

let service: DietologistService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [DietologistService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(DietologistService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('DietologistService relationship reads', () => {
    it('keeps only goal fields from legacy responses and preserves explicit zero targets', () => {
        service.getClientGoals(entityId<'user'>('client-1')).subscribe(goals => {
            expect(goals).toEqual({
                id: 'client-1',
                dailyCalorieTarget: 2100,
                proteinTarget: 0,
                fatTarget: null,
                carbTarget: null,
                fiberTarget: null,
                waterGoal: 1800,
                hydrationGoal: 2600,
                desiredWeightKg: null,
                desiredWaistCm: null,
                stepGoal: 6000,
            });
        });
        const request = httpMock.expectOne(`${BASE_URL}/clients/client-1/goals`);
        request.flush({
            id: 'client-1',
            dailyCalorieTarget: 2100,
            proteinTarget: 0,
            waterGoal: 1800,
            hydrationGoal: 2600,
            stepGoal: 6000,
            email: 'private@example.test',
            birthDate: '2001-01-01',
            firstName: 'Private',
            heightCm: 182,
            aiConsentAcceptedAt: '2026-10-01T00:00:00Z',
            hasPassword: true,
        });
    });

    it('gets relationship and current-user invitation', () => {
        service.getRelationship().subscribe();
        const relationshipReq = httpMock.expectOne(`${BASE_URL}/relationship`);
        expect(relationshipReq.request.method).toBe('GET');
        relationshipReq.flush(null);

        service.getInvitationForCurrentUser(entityId<'dietologist-invitation'>('inv-1')).subscribe();
        const invitationReq = httpMock.expectOne(`${BASE_URL}/invitations/inv-1/current-user`);
        expect(invitationReq.request.method).toBe('GET');
        invitationReq.flush({
            invitationId: entityId<'dietologist-invitation'>('inv-1'),
            clientUserId: entityId<'user'>('client-1'),
            clientEmail: 'client@example.com',
            clientFirstName: null,
            clientLastName: null,
            status: 'Pending',
            createdAtUtc: utcInstant('2026-04-15T00:00:00Z'),
            expiresAtUtc: utcInstant('2026-04-22T00:00:00Z'),
        });
    });
});

describe('DietologistService saved relationship details', () => {
    const invitationId = '02439194-331f-4def-89ef-d16e60d561c4';
    const dietologistId = '526285f4-eb42-4f93-a7b0-9ec3a8ed049a';
    const createdAt = '2026-05-12T20:05:00Z';
    const expiresAt = '2026-05-19T20:05:00Z';
    const acceptedAt = '2026-05-13T00:15:00+04:00';
    const pendingRelationship = {
        invitationId,
        email: 'dietologist@example.test',
        createdAtUtc: createdAt,
        expiresAtUtc: expiresAt,
        status: 'Pending',
        permissions: PERMISSIONS,
    } satisfies DietologistRelationshipHttpResponse;

    it('keeps the pending invitation email and leaves the accepted user and timestamp absent', async () => {
        const pending = firstValueFrom(service.getRelationship());
        httpMock.expectOne({ method: 'GET', url: `${BASE_URL}/relationship` }).flush(pendingRelationship);

        await expect(pending).resolves.toEqual({
            ...pendingRelationship,
            invitationId: entityId<'dietologist-invitation'>(invitationId),
            dietologistUserId: null,
            acceptedAtUtc: null,
            firstName: null,
            lastName: null,
        });
    });

    it('preserves acceptance history and the explicit set of granted permissions', async () => {
        const relationship = {
            ...pendingRelationship,
            status: 'Accepted',
            dietologistUserId: dietologistId,
            acceptedAtUtc: acceptedAt,
            email: 'dietologist@example.test',
            firstName: 'Ada',
            lastName: 'Lovelace',
            permissions: { ...PERMISSIONS, shareProfile: false, shareFasting: true },
        } satisfies DietologistRelationshipHttpResponse;
        const pending = firstValueFrom(service.getRelationship());
        httpMock.expectOne({ method: 'GET', url: `${BASE_URL}/relationship` }).flush(relationship);

        await expect(pending).resolves.toEqual({
            ...relationship,
            invitationId: entityId<'dietologist-invitation'>(invitationId),
            dietologistUserId: entityId<'user'>(dietologistId),
            createdAtUtc: utcInstant(createdAt),
            expiresAtUtc: utcInstant(expiresAt),
            acceptedAtUtc: utcInstant(acceptedAt),
        });
    });

    it('rejects a relationship response with an incomplete permission record', async () => {
        const pending = firstValueFrom(service.getRelationship());
        httpMock.expectOne({ method: 'GET', url: `${BASE_URL}/relationship` }).flush({
            ...pendingRelationship,
            permissions: { ...PERMISSIONS, shareFasting: undefined },
        });

        await expect(pending).rejects.toThrow('shareFasting');
    });
});

describe('DietologistService client workspace', () => {
    it('loads client dashboard, goals and recommendations', () => {
        service
            .getClientDashboard(entityId<'user'>('client-1'), {
                dateFrom: new Date(LOCAL_DATE_YEAR, MAY_MONTH_INDEX, PERIOD_START_DAY),
                dateTo: new Date(LOCAL_DATE_YEAR, MAY_MONTH_INDEX, PERIOD_END_DAY),
                locale: 'en',
            })
            .subscribe();
        const dashboardReq = httpMock.expectOne(
            `${BASE_URL}/clients/client-1/dashboard?dateFrom=2026-05-17&dateTo=2026-05-23&page=1&limit=5&locale=en&trendDays=14`,
        );
        expect(dashboardReq.request.method).toBe('GET');
        dashboardReq.flush(NO_MEALS_DASHBOARD_RESPONSE);

        service.getClientGoals(entityId<'user'>('client-1')).subscribe();
        const goalsReq = httpMock.expectOne(`${BASE_URL}/clients/client-1/goals`);
        expect(goalsReq.request.method).toBe('GET');
        goalsReq.flush({ id: 'client-1', email: 'client@example.com' });

        service.getRecommendationsForClient(entityId<'user'>('client-1')).subscribe();
        const recommendationsReq = httpMock.expectOne(`${BASE_URL}/clients/client-1/recommendations?page=1&limit=100`);
        expect(recommendationsReq.request.method).toBe('GET');
        recommendationsReq.flush([]);
    });

    it.each([undefined, '   '])('requests a single local day with an absent or blank locale (%s)', async locale => {
        const clientId = entityId<'user'>('client-1');
        const dateFrom = new Date(LOCAL_DATE_YEAR, MAY_MONTH_INDEX, PERIOD_START_DAY);
        const dashboard = firstValueFrom(service.getClientDashboard(clientId, { dateFrom, locale }));
        const request = httpMock.expectOne(
            `${BASE_URL}/clients/${clientId}/dashboard?dateFrom=2026-05-17&dateTo=2026-05-17&page=1&limit=5&trendDays=14`,
        );
        expect(request.request.method).toBe('GET');
        request.flush({ ...NO_MEALS_DASHBOARD_RESPONSE, dateTo: '2026-05-17' });

        expect(await dashboard).toMatchObject({ date: '2026-05-17', dateTo: '2026-05-17' });
    });

    it('creates recommendations and disconnects client', () => {
        service.createRecommendation(entityId<'user'>('client-1'), { text: 'Please add more protein.' }).subscribe();
        const createReq = httpMock.expectOne(`${BASE_URL}/clients/client-1/recommendations`);
        expect(createReq.request.method).toBe('POST');
        expect(createReq.request.body).toEqual({ text: 'Please add more protein.' });
        createReq.flush({
            id: entityId<'recommendation'>('recommendation-1'),
            dietologistUserId: entityId<'user'>('dietologist-1'),
            text: 'Please add more protein.',
            isRead: false,
            createdAtUtc: utcInstant('2026-05-17T10:00:00Z'),
        });

        service.disconnectClient(entityId<'user'>('client-1')).subscribe();
        const disconnectReq = httpMock.expectOne(`${BASE_URL}/clients/client-1`);
        expect(disconnectReq.request.method).toBe('DELETE');
        disconnectReq.flush(null);
    });
});

describe('DietologistService client list and attention', () => {
    it('loads connected clients without inventing withheld profile fields', async () => {
        const response = { userId: 'client-1', permissions: PERMISSIONS, acceptedAtUtc: '2026-05-17T10:00:00Z' };
        const clients = firstValueFrom(service.getMyClients());
        const request = httpMock.expectOne(`${BASE_URL}/clients?page=1&limit=100`);
        expect(request.request.method).toBe('GET');
        request.flush([response]);

        expect(await clients).toEqual([
            {
                ...response,
                email: null,
                firstName: null,
                lastName: null,
                profileImage: null,
                birthDate: null,
                gender: null,
                heightCm: null,
                activityLevel: null,
            },
        ]);
    });

    it('loads attention signals with the chosen thresholds and distinguishes acknowledge from snooze', async () => {
        const settings = {
            inactivityDays: 3,
            calorieDeviationPercent: 20,
            sustainedDays: 4,
            weightChangePercent: 5,
            lookbackDays: 30,
        };
        const response = {
            id: 'inactive-client',
            clientUserId: 'client-1',
            type: 'DiaryInactivity',
            severity: 'High',
            reason: 'NoRecentDiaryEntries',
            detectedAtUtc: '2026-05-23T12:00:00Z',
        };
        const signals = firstValueFrom(service.getAttentionSignals(settings));
        const read = httpMock.expectOne(request => request.url === `${BASE_URL}/clients/attention`);
        expect(read.request.method).toBe('GET');
        expect(Object.fromEntries(read.request.params.keys().map(key => [key, Number(read.request.params.get(key))]))).toEqual(settings);
        read.flush([response]);
        const [signal] = await signals;
        expect(signal).toEqual({ ...response, clientDisplayName: null, snoozedUntilUtc: null });

        const ack = firstValueFrom(service.setAttentionSignalState(signal, 'Acknowledge'));
        const command = `${BASE_URL}/clients/attention/${signal.id}/state`;
        const acknowledge = httpMock.expectOne({ method: 'PUT', url: command });
        expect(acknowledge.request.body).toEqual({ clientUserId: signal.clientUserId, action: 'Acknowledge', snoozedUntilUtc: null });
        acknowledge.flush(null);
        await ack;

        const snoozedUntilUtc = '2026-05-26T12:00:00Z';
        const snooze = firstValueFrom(service.setAttentionSignalState(signal, 'Snooze', snoozedUntilUtc));
        const postpone = httpMock.expectOne({ method: 'PUT', url: command });
        expect(postpone.request.body).toEqual({ clientUserId: signal.clientUserId, action: 'Snooze', snoozedUntilUtc });
        postpone.flush(null);
        await snooze;
    });
});

describe('DietologistService tasks and templates', () => {
    it('loads all task pages and preserves empty optional task fields', async () => {
        const task = makeTaskResponse();
        const tasks = firstValueFrom(service.getTasksForClient(entityId<'user'>(task.clientUserId)));
        const firstPage = httpMock.expectOne(`${BASE_URL}/clients/${task.clientUserId}/tasks?page=1&limit=100`);
        expect(firstPage.request.method).toBe('GET');
        firstPage.flush(Array.from({ length: COLLECTION_PAGE_LIMIT }, (_, index) => ({ ...task, id: `task-${index}` })));
        const secondPage = httpMock.expectOne(`${BASE_URL}/clients/${task.clientUserId}/tasks?page=2&limit=100`);
        secondPage.flush([task]);

        const result = await tasks;
        expect(result).toHaveLength(COLLECTION_PAGE_LIMIT + 1);
        expect(result.at(-1)).toEqual({ ...task, details: null, dueAtUtc: null, statusChangedAtUtc: null });
    });

    it('uses server task state when creating and cancelling a task', async () => {
        const task = makeTaskResponse();
        const taskDraft = { title: task.title, details: 'Compare the meals', dueAtUtc: '2026-05-24T20:00:00Z' };
        const created = firstValueFrom(service.createTask(entityId<'user'>(task.clientUserId), taskDraft));
        const create = httpMock.expectOne({ method: 'POST', url: `${BASE_URL}/clients/${task.clientUserId}/tasks` });
        expect(create.request.body).toEqual(taskDraft);
        create.flush({ ...task, ...taskDraft });
        expect(await created).toEqual({ ...task, ...taskDraft, statusChangedAtUtc: null });

        const cancelled = firstValueFrom(service.cancelTask(entityId<'client-task'>(task.id)));
        const cancel = httpMock.expectOne({ method: 'PUT', url: `${BASE_URL}/clients/tasks/${task.id}/cancel` });
        const statusChangedAtUtc = '2026-05-23T12:00:00Z';
        cancel.flush({ ...task, ...taskDraft, status: 'Cancelled', statusChangedAtUtc });
        expect(await cancelled).toEqual({ ...task, ...taskDraft, status: 'Cancelled', statusChangedAtUtc });
    });

    it('searches active and archived recommendation templates using the selected filter', async () => {
        const active = firstValueFrom(service.searchRecommendationTemplates());
        const activeRequest = httpMock.expectOne(request => request.url === `${BASE_URL}/recommendation-templates`);
        expect(activeRequest.request.method).toBe('GET');
        expect(activeRequest.request.params.get('includeArchived')).toBe('false');
        expect(activeRequest.request.params.get('search')).toBe('');
        activeRequest.flush([]);
        expect(await active).toEqual([]);

        const archived = firstValueFrom(service.searchRecommendationTemplates('protein', true));
        const request = httpMock.expectOne(candidate => candidate.url === `${BASE_URL}/recommendation-templates`);
        expect(request.request.params.get('includeArchived')).toBe('true');
        expect(request.request.params.get('search')).toBe('protein');
        const template = { ...makeTemplateResponse(), isArchived: true };
        request.flush([template]);
        expect(await archived).toEqual([{ ...template, modifiedAtUtc: null }]);
    });

    it('creates, updates, and archives a recommendation template with its returned identity', async () => {
        const template = makeTemplateResponse();
        const draft = { name: template.name, text: template.text };
        const creation = firstValueFrom(service.createRecommendationTemplate(draft));
        const path = `${BASE_URL}/recommendation-templates`;
        const create = httpMock.expectOne({ method: 'POST', url: path });
        expect(create.request.body).toEqual(draft);
        create.flush(template);
        const created = await creation;
        expect(created).toEqual({ ...template, modifiedAtUtc: null });

        const updateDraft = { ...draft, text: 'Add beans to lunch and keep portions consistent.' };
        const updated = firstValueFrom(service.updateRecommendationTemplate(created.id, updateDraft));
        const resource = `${path}/${created.id}`;
        const update = httpMock.expectOne({ method: 'PUT', url: resource });
        expect(update.request.body).toEqual(updateDraft);
        const modifiedAtUtc = '2026-05-23T12:00:00Z';
        update.flush({ ...template, ...updateDraft, modifiedAtUtc });
        expect(await updated).toEqual({ ...template, ...updateDraft, modifiedAtUtc });

        const archived = firstValueFrom(service.archiveRecommendationTemplate(created.id));
        httpMock.expectOne({ method: 'DELETE', url: resource }).flush(null);
        await archived;
    });
});

describe('DietologistService bulk recommendation delivery', () => {
    it('keeps the idempotency key and maps successful and unsuccessful recipients independently', async () => {
        const clientUserIds = [entityId<'user'>('client-1'), entityId<'user'>('client-2')];
        const text = 'Keep lunch portions consistent this week.';
        const idempotencyKey = '9fb17194-40aa-4e60-bf08-ca40d0af5581';
        const result = firstValueFrom(service.bulkCreateRecommendations(clientUserIds, text, idempotencyKey));
        const request = httpMock.expectOne({ method: 'POST', url: `${BASE_URL}/recommendations/bulk` });
        expect(request.request.body).toEqual({ clientUserIds, text, idempotencyKey });
        const success = { clientUserId: clientUserIds[0], succeeded: true, wasAlreadyProcessed: true, recommendationId: 'rec-1' };
        const failure = { clientUserId: clientUserIds[1], succeeded: false, wasAlreadyProcessed: false, errorCode: 'ClientNotFound' };
        request.flush({ idempotencyKey, recipients: [success, failure] });

        expect(await result).toEqual({
            idempotencyKey,
            recipients: [
                { ...success, errorCode: null },
                { ...failure, recommendationId: null },
            ],
        });
    });
});

function makeTaskResponse(): {
    id: string;
    dietologistUserId: string;
    clientUserId: string;
    title: string;
    status: 'Open';
    createdAtUtc: string;
    isOverdue: boolean;
} {
    return {
        id: 'walk-task',
        dietologistUserId: 'diet-1',
        clientUserId: 'client-1',
        title: 'Log today’s walk',
        status: 'Open',
        createdAtUtc: '2026-05-23T09:00:00Z',
        isOverdue: false,
    };
}

function makeTemplateResponse(): { id: string; name: string; text: string; isArchived: boolean; createdAtUtc: string } {
    return {
        id: 'protein-template',
        name: 'Protein guide',
        text: 'Include beans at lunch.',
        isArchived: false,
        createdAtUtc: '2026-05-23T09:00:00Z',
    };
}

describe('DietologistService invitations', () => {
    it('posts invite and current-user accept decline commands', () => {
        service.invite({ dietologistEmail: 'diet@example.com', permissions: INVITE_PERMISSIONS }).subscribe();
        const inviteReq = httpMock.expectOne(`${BASE_URL}/invite`);
        expect(inviteReq.request.method).toBe('POST');
        expect(inviteReq.request.body).toEqual({
            dietologistEmail: 'diet@example.com',
            permissions: INVITE_PERMISSIONS,
        });
        inviteReq.flush(null);

        service.acceptInvitationForCurrentUser(entityId<'dietologist-invitation'>('inv-1')).subscribe();
        const acceptReq = httpMock.expectOne(`${BASE_URL}/invitations/inv-1/accept-current-user`);
        expect(acceptReq.request.method).toBe('POST');
        expect(acceptReq.request.body).toBeNull();
        acceptReq.flush(null);

        service.declineInvitationForCurrentUser(entityId<'dietologist-invitation'>('inv-1')).subscribe();
        const declineReq = httpMock.expectOne(`${BASE_URL}/invitations/inv-1/decline-current-user`);
        expect(declineReq.request.method).toBe('POST');
        expect(declineReq.request.body).toBeNull();
        declineReq.flush(null);
    });
});

describe('DietologistService relationship mutations', () => {
    it('updates permissions and revokes relationship', () => {
        service.updatePermissions(PERMISSIONS).subscribe();
        const permissionsReq = httpMock.expectOne(`${BASE_URL}/permissions`);
        expect(permissionsReq.request.method).toBe('PUT');
        expect(permissionsReq.request.body).toEqual({ permissions: PERMISSIONS });
        permissionsReq.flush(null);

        service.revokeRelationship().subscribe();
        const revokeReq = httpMock.expectOne(`${BASE_URL}/relationship`);
        expect(revokeReq.request.method).toBe('DELETE');
        revokeReq.flush(null);
    });
});

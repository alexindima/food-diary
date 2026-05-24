import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
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
    it('gets relationship and current-user invitation', () => {
        service.getRelationship().subscribe();
        const relationshipReq = httpMock.expectOne(`${BASE_URL}/relationship`);
        expect(relationshipReq.request.method).toBe('GET');
        relationshipReq.flush(null);

        service.getInvitationForCurrentUser('inv-1').subscribe();
        const invitationReq = httpMock.expectOne(`${BASE_URL}/invitations/inv-1/current-user`);
        expect(invitationReq.request.method).toBe('GET');
        invitationReq.flush({
            invitationId: 'inv-1',
            clientUserId: 'client-1',
            clientEmail: 'client@example.com',
            clientFirstName: null,
            clientLastName: null,
            status: 'Pending',
            createdAtUtc: '2026-04-15T00:00:00Z',
            expiresAtUtc: '2026-04-22T00:00:00Z',
        });
    });
});

describe('DietologistService client workspace', () => {
    it('loads client dashboard, goals and recommendations', () => {
        service
            .getClientDashboard('client-1', {
                dateFrom: new Date(LOCAL_DATE_YEAR, MAY_MONTH_INDEX, PERIOD_START_DAY),
                dateTo: new Date(LOCAL_DATE_YEAR, MAY_MONTH_INDEX, PERIOD_END_DAY),
                locale: 'en',
            })
            .subscribe();
        const dashboardReq = httpMock.expectOne(
            `${BASE_URL}/clients/client-1/dashboard?dateFrom=2026-05-17&dateTo=2026-05-23&page=1&pageSize=5&trendDays=14&locale=en`,
        );
        expect(dashboardReq.request.method).toBe('GET');
        dashboardReq.flush({});

        service.getClientGoals('client-1').subscribe();
        const goalsReq = httpMock.expectOne(`${BASE_URL}/clients/client-1/goals`);
        expect(goalsReq.request.method).toBe('GET');
        goalsReq.flush({});

        service.getRecommendationsForClient('client-1').subscribe();
        const recommendationsReq = httpMock.expectOne(`${BASE_URL}/clients/client-1/recommendations`);
        expect(recommendationsReq.request.method).toBe('GET');
        recommendationsReq.flush([]);
    });

    it('creates recommendations and disconnects client', () => {
        service.createRecommendation('client-1', { text: 'Please add more protein.' }).subscribe();
        const createReq = httpMock.expectOne(`${BASE_URL}/clients/client-1/recommendations`);
        expect(createReq.request.method).toBe('POST');
        expect(createReq.request.body).toEqual({ text: 'Please add more protein.' });
        createReq.flush({});

        service.disconnectClient('client-1').subscribe();
        const disconnectReq = httpMock.expectOne(`${BASE_URL}/clients/client-1`);
        expect(disconnectReq.request.method).toBe('DELETE');
        disconnectReq.flush(null);
    });
});

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

        service.acceptInvitationForCurrentUser('inv-1').subscribe();
        const acceptReq = httpMock.expectOne(`${BASE_URL}/invitations/inv-1/accept-current-user`);
        expect(acceptReq.request.method).toBe('POST');
        expect(acceptReq.request.body).toEqual({});
        acceptReq.flush(null);

        service.declineInvitationForCurrentUser('inv-1').subscribe();
        const declineReq = httpMock.expectOne(`${BASE_URL}/invitations/inv-1/decline-current-user`);
        expect(declineReq.request.method).toBe('POST');
        expect(declineReq.request.body).toEqual({});
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

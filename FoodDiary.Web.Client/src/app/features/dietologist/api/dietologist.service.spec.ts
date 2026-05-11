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

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { DietologistService } from './dietologist.service';

describe('DietologistService', () => {
    let service: DietologistService;
    let httpMock: HttpTestingController;

    const baseUrl = environment.apiUrls.dietologist;

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

    it('gets relationship and current-user invitation', () => {
        service.getRelationship().subscribe();
        const relationshipReq = httpMock.expectOne(`${baseUrl}/relationship`);
        expect(relationshipReq.request.method).toBe('GET');
        relationshipReq.flush(null);

        service.getInvitationForCurrentUser('inv-1').subscribe();
        const invitationReq = httpMock.expectOne(`${baseUrl}/invitations/inv-1/current-user`);
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

    it('posts invite and current-user accept decline commands', () => {
        service
            .invite({
                dietologistEmail: 'diet@example.com',
                permissions: {
                    shareMeals: true,
                    shareStatistics: true,
                    shareWeight: false,
                    shareWaist: true,
                    shareGoals: true,
                    shareHydration: false,
                    shareProfile: true,
                    shareFasting: true,
                },
            })
            .subscribe();
        const inviteReq = httpMock.expectOne(`${baseUrl}/invite`);
        expect(inviteReq.request.method).toBe('POST');
        expect(inviteReq.request.body).toEqual({
            dietologistEmail: 'diet@example.com',
            permissions: {
                shareMeals: true,
                shareStatistics: true,
                shareWeight: false,
                shareWaist: true,
                shareGoals: true,
                shareHydration: false,
                shareProfile: true,
                shareFasting: true,
            },
        });
        inviteReq.flush(null);

        service.acceptInvitationForCurrentUser('inv-1').subscribe();
        const acceptReq = httpMock.expectOne(`${baseUrl}/invitations/inv-1/accept-current-user`);
        expect(acceptReq.request.method).toBe('POST');
        expect(acceptReq.request.body).toEqual({});
        acceptReq.flush(null);

        service.declineInvitationForCurrentUser('inv-1').subscribe();
        const declineReq = httpMock.expectOne(`${baseUrl}/invitations/inv-1/decline-current-user`);
        expect(declineReq.request.method).toBe('POST');
        expect(declineReq.request.body).toEqual({});
        declineReq.flush(null);
    });

    it('updates permissions and revokes relationship', () => {
        service
            .updatePermissions({
                shareMeals: false,
                shareStatistics: true,
                shareWeight: true,
                shareWaist: true,
                shareGoals: false,
                shareHydration: true,
                shareProfile: true,
                shareFasting: false,
            })
            .subscribe();
        const permissionsReq = httpMock.expectOne(`${baseUrl}/permissions`);
        expect(permissionsReq.request.method).toBe('PUT');
        expect(permissionsReq.request.body).toEqual({
            permissions: {
                shareMeals: false,
                shareStatistics: true,
                shareWeight: true,
                shareWaist: true,
                shareGoals: false,
                shareHydration: true,
                shareProfile: true,
                shareFasting: false,
            },
        });
        permissionsReq.flush(null);

        service.revokeRelationship().subscribe();
        const revokeReq = httpMock.expectOne(`${baseUrl}/relationship`);
        expect(revokeReq.request.method).toBe('DELETE');
        revokeReq.flush(null);
    });
});

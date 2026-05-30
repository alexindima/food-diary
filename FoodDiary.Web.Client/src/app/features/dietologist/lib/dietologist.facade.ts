import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
    ClientSummary,
    CreateRecommendationRequest,
    DietologistClientGoals,
    DietologistInvitationForCurrentUser,
    DietologistPermissions,
    DietologistRecommendation,
    DietologistRelationship,
    InviteDietologistRequest,
} from '../../../shared/models/dietologist.data';
import type { DashboardSnapshot } from '../../dashboard/models/dashboard.data';
import { type DietologistClientDashboardQuery, DietologistService } from '../api/dietologist.service';

@Injectable({ providedIn: 'root' })
export class DietologistFacade {
    private readonly dietologistService = inject(DietologistService);

    public getRelationship(): Observable<DietologistRelationship | null> {
        return this.dietologistService.getRelationship();
    }

    public getInvitationForCurrentUser(invitationId: string): Observable<DietologistInvitationForCurrentUser> {
        return this.dietologistService.getInvitationForCurrentUser(invitationId);
    }

    public getMyClients(): Observable<ClientSummary[]> {
        return this.dietologistService.getMyClients();
    }

    public getClientDashboard(clientUserId: string, query: DietologistClientDashboardQuery): Observable<DashboardSnapshot> {
        return this.dietologistService.getClientDashboard(clientUserId, query);
    }

    public getClientGoals(clientUserId: string): Observable<DietologistClientGoals> {
        return this.dietologistService.getClientGoals(clientUserId);
    }

    public getRecommendationsForClient(clientUserId: string): Observable<DietologistRecommendation[]> {
        return this.dietologistService.getRecommendationsForClient(clientUserId);
    }

    public disconnectClient(clientUserId: string): Observable<void> {
        return this.dietologistService.disconnectClient(clientUserId);
    }

    public createRecommendation(clientUserId: string, request: CreateRecommendationRequest): Observable<DietologistRecommendation> {
        return this.dietologistService.createRecommendation(clientUserId, request);
    }

    public invite(request: InviteDietologistRequest): Observable<void> {
        return this.dietologistService.invite(request);
    }

    public acceptInvitationForCurrentUser(invitationId: string): Observable<void> {
        return this.dietologistService.acceptInvitationForCurrentUser(invitationId);
    }

    public declineInvitationForCurrentUser(invitationId: string): Observable<void> {
        return this.dietologistService.declineInvitationForCurrentUser(invitationId);
    }

    public updatePermissions(permissions: DietologistPermissions): Observable<void> {
        return this.dietologistService.updatePermissions(permissions);
    }

    public revokeRelationship(): Observable<void> {
        return this.dietologistService.revokeRelationship();
    }
}

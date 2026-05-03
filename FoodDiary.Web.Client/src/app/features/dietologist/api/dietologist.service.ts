import { Injectable } from '@angular/core';
import { type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import {
    type ClientSummary,
    type DietologistInfo,
    type DietologistInvitationForCurrentUser,
    type DietologistPermissions,
    type DietologistRelationship,
    type InviteDietologistRequest,
} from '../models/dietologist.data';

@Injectable({ providedIn: 'root' })
export class DietologistService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.dietologist;

    public getMyDietologist(): Observable<DietologistInfo | null> {
        return this.get<DietologistInfo | null>('my-dietologist');
    }

    public getRelationship(): Observable<DietologistRelationship | null> {
        return this.get<DietologistRelationship | null>('relationship');
    }

    public getInvitationForCurrentUser(invitationId: string): Observable<DietologistInvitationForCurrentUser> {
        return this.get<DietologistInvitationForCurrentUser>(`invitations/${invitationId}/current-user`);
    }

    public getMyClients(): Observable<ClientSummary[]> {
        return this.get<ClientSummary[]>('clients');
    }

    public invite(request: InviteDietologistRequest): Observable<void> {
        return this.post<void>('invite', request);
    }

    public acceptInvitationForCurrentUser(invitationId: string): Observable<void> {
        return this.post<void>(`invitations/${invitationId}/accept-current-user`, {});
    }

    public declineInvitationForCurrentUser(invitationId: string): Observable<void> {
        return this.post<void>(`invitations/${invitationId}/decline-current-user`, {});
    }

    public updatePermissions(permissions: DietologistPermissions): Observable<void> {
        return this.put<void>('permissions', { permissions });
    }

    public revokeRelationship(): Observable<void> {
        return this.delete<void>('relationship');
    }

    public disconnectClient(clientUserId: string): Observable<void> {
        return this.delete<void>(`clients/${clientUserId}`);
    }

    public getClientDashboard(clientUserId: string, date: string): Observable<unknown> {
        return this.get<unknown>(`clients/${clientUserId}/dashboard`, { date });
    }

    public getClientGoals(clientUserId: string): Observable<unknown> {
        return this.get<unknown>(`clients/${clientUserId}/goals`);
    }
}

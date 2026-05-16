import { Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import type {
    ClientSummary,
    DietologistInvitationForCurrentUser,
    DietologistPermissions,
    DietologistRelationship,
    InviteDietologistRequest,
} from '../../../shared/models/dietologist.data';

@Injectable({ providedIn: 'root' })
export class DietologistService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.dietologist;

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
}

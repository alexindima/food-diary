import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../services/api.service';
import { environment } from '../../../../environments/environment';
import { ClientSummary, DietologistInfo, DietologistPermissions, InviteDietologistRequest } from '../models/dietologist.data';

@Injectable({ providedIn: 'root' })
export class DietologistService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.dietologist;

    public getMyDietologist(): Observable<DietologistInfo | null> {
        return this.get<DietologistInfo | null>('my-dietologist');
    }

    public getMyClients(): Observable<ClientSummary[]> {
        return this.get<ClientSummary[]>('clients');
    }

    public invite(request: InviteDietologistRequest): Observable<void> {
        return this.post<void>('invite', request);
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

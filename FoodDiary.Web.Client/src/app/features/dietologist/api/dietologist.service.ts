import { Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
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

const DEFAULT_CLIENT_DASHBOARD_PAGE_SIZE = 5;
const DEFAULT_CLIENT_DASHBOARD_TREND_DAYS = 14;

export type DietologistClientDashboardQuery = {
    date: Date;
    page?: number;
    pageSize?: number;
    locale?: string;
    trendDays?: number;
};

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

    public getClientDashboard(clientUserId: string, query: DietologistClientDashboardQuery): Observable<DashboardSnapshot> {
        const {
            date,
            page = 1,
            pageSize = DEFAULT_CLIENT_DASHBOARD_PAGE_SIZE,
            locale,
            trendDays = DEFAULT_CLIENT_DASHBOARD_TREND_DAYS,
        } = query;
        const params: Record<string, string | number> = {
            date: date.toISOString(),
            page,
            pageSize,
            trendDays,
        };

        if (locale !== undefined && locale.trim().length > 0) {
            params['locale'] = locale;
        }

        return this.get<DashboardSnapshot>(`clients/${clientUserId}/dashboard`, params);
    }

    public getClientGoals(clientUserId: string): Observable<DietologistClientGoals> {
        return this.get<DietologistClientGoals>(`clients/${clientUserId}/goals`);
    }

    public getRecommendationsForClient(clientUserId: string): Observable<DietologistRecommendation[]> {
        return this.get<DietologistRecommendation[]>(`clients/${clientUserId}/recommendations`);
    }

    public disconnectClient(clientUserId: string): Observable<void> {
        return this.delete<void>(`clients/${clientUserId}`);
    }

    public createRecommendation(clientUserId: string, request: CreateRecommendationRequest): Observable<DietologistRecommendation> {
        return this.post<DietologistRecommendation>(`clients/${clientUserId}/recommendations`, request);
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

import { Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { formatDateInputValue } from '../../../shared/lib/local-date.utils';
import type {
    AttentionSignal,
    AttentionSignalSettings,
    BulkRecommendationResult,
    ClientSummary,
    ClientTask,
    CreateClientTaskRequest,
    CreateRecommendationRequest,
    DietologistClientGoals,
    DietologistInvitationForCurrentUser,
    DietologistPermissions,
    DietologistRecommendation,
    DietologistRelationship,
    InviteDietologistRequest,
    RecommendationTemplate,
    RecommendationTemplateRequest,
} from '../../../shared/models/dietologist.data';
import type { DashboardSnapshot } from '../../dashboard/models/dashboard.data';

const DEFAULT_CLIENT_DASHBOARD_PAGE_SIZE = 5;
const DEFAULT_CLIENT_DASHBOARD_TREND_DAYS = 14;

export type DietologistClientDashboardQuery = {
    dateFrom: Date;
    dateTo?: Date;
    page?: number;
    pageSize?: number;
    locale?: string;
    trendDays?: number;
};

@Service()
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

    public getAttentionSignals(settings: AttentionSignalSettings): Observable<AttentionSignal[]> {
        return this.get<AttentionSignal[]>('clients/attention', settings);
    }

    public setAttentionSignalState(
        signal: AttentionSignal,
        action: 'Acknowledge' | 'Snooze',
        snoozedUntilUtc: string | null = null,
    ): Observable<void> {
        return this.put<void>(`clients/attention/${encodeURIComponent(signal.id)}/state`, {
            clientUserId: signal.clientUserId,
            action,
            snoozedUntilUtc,
        });
    }

    public getClientDashboard(clientUserId: string, query: DietologistClientDashboardQuery): Observable<DashboardSnapshot> {
        const {
            dateFrom,
            dateTo,
            page = 1,
            pageSize = DEFAULT_CLIENT_DASHBOARD_PAGE_SIZE,
            locale,
            trendDays = DEFAULT_CLIENT_DASHBOARD_TREND_DAYS,
        } = query;
        const params: Record<string, string | number> = {
            dateFrom: formatDateInputValue(dateFrom),
            dateTo: formatDateInputValue(dateTo ?? dateFrom),
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

    public getTasksForClient(clientUserId: string): Observable<ClientTask[]> {
        return this.get<ClientTask[]>(`clients/${clientUserId}/tasks`);
    }

    public createTask(clientUserId: string, request: CreateClientTaskRequest): Observable<ClientTask> {
        return this.post<ClientTask>(`clients/${clientUserId}/tasks`, request);
    }

    public cancelTask(taskId: string): Observable<ClientTask> {
        return this.put<ClientTask>(`clients/tasks/${taskId}/cancel`, {});
    }

    public searchRecommendationTemplates(search = '', includeArchived = false): Observable<RecommendationTemplate[]> {
        return this.get<RecommendationTemplate[]>('recommendation-templates', { search, includeArchived: String(includeArchived) });
    }

    public createRecommendationTemplate(request: RecommendationTemplateRequest): Observable<RecommendationTemplate> {
        return this.post<RecommendationTemplate>('recommendation-templates', request);
    }

    public updateRecommendationTemplate(templateId: string, request: RecommendationTemplateRequest): Observable<RecommendationTemplate> {
        return this.put<RecommendationTemplate>(`recommendation-templates/${templateId}`, request);
    }

    public archiveRecommendationTemplate(templateId: string): Observable<void> {
        return this.delete<void>(`recommendation-templates/${templateId}`);
    }

    public bulkCreateRecommendations(clientUserIds: string[], text: string, idempotencyKey: string): Observable<BulkRecommendationResult> {
        return this.post<BulkRecommendationResult>('recommendations/bulk', { clientUserIds, text, idempotencyKey });
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

import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { loadPagedCollection } from '../../../shared/api/load-paged-collection';
import { dashboardSnapshotFromSdk } from '../../../shared/api/sdk/dashboard-sdk.mapper';
import {
    attentionSignalFromSdk,
    bulkRecommendationsFromSdk,
    clientGoalsFromSdk,
    clientSummaryFromSdk,
    invitationFromSdk,
    recommendationTemplateFromSdk,
    relationshipFromSdk,
} from '../../../shared/api/sdk/dietologist-sdk.mapper';
import { DietologistSdk } from '../../../shared/api/sdk/generated/api/dietologist.service';
import { clientTaskFromSdk, recommendationFromSdk } from '../../../shared/api/sdk/recommendation-sdk.mapper';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkOptional } from '../../../shared/api/sdk/sdk-response';
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
import type { DietologistDashboardSnapshot } from '../lib/dietologist-dashboard.data';

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
export class DietologistService {
    protected readonly baseUrl = environment.apiUrls.dietologist;
    private readonly sdk = createSdkConnection(DietologistSdk, this.baseUrl, inject(HttpClient));

    public getRelationship(): Observable<DietologistRelationship | null> {
        return this.sdk.client
            .getDietologistRelationship({ version: this.sdk.version })
            .pipe(map(value => sdkOptional(value, relationshipFromSdk)));
    }

    public getInvitationForCurrentUser(invitationId: string): Observable<DietologistInvitationForCurrentUser> {
        return this.sdk.client
            .getDietologistInvitationsByInvitationIdCurrentUser({ version: this.sdk.version, invitationId })
            .pipe(map(invitationFromSdk));
    }

    public getMyClients(): Observable<ClientSummary[]> {
        return loadPagedCollection((page, limit) =>
            this.sdk.client
                .getDietologistClients({ version: this.sdk.version, page, limit })
                .pipe(map(values => values.map(clientSummaryFromSdk))),
        );
    }

    public getAttentionSignals(settings: AttentionSignalSettings): Observable<AttentionSignal[]> {
        return this.sdk.client
            .getDietologistClientsAttention({ version: this.sdk.version, ...settings })
            .pipe(map(values => values.map(attentionSignalFromSdk)));
    }

    public setAttentionSignalState(
        signal: AttentionSignal,
        action: 'Acknowledge' | 'Snooze',
        snoozedUntilUtc: string | null = null,
    ): Observable<void> {
        return this.sdk.client.putDietologistClientsAttentionBySignalIdState({
            version: this.sdk.version,
            signalId: signal.id,
            setAttentionSignalStateHttpRequest: { clientUserId: signal.clientUserId, action, snoozedUntilUtc },
        });
    }

    public getClientDashboard(clientUserId: string, query: DietologistClientDashboardQuery): Observable<DietologistDashboardSnapshot> {
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
            limit: pageSize,
            trendDays,
        };

        if (locale !== undefined && locale.trim().length > 0) {
            params['locale'] = locale;
        }

        return this.sdk.client
            .getDietologistClientsByClientUserIdDashboard({ version: this.sdk.version, clientUserId, ...params })
            .pipe(map(dashboardSnapshotFromSdk));
    }

    public getClientGoals(clientUserId: string): Observable<DietologistClientGoals> {
        return this.sdk.client
            .getDietologistClientsByClientUserIdGoals({ version: this.sdk.version, clientUserId })
            .pipe(map(clientGoalsFromSdk));
    }

    public getRecommendationsForClient(clientUserId: string): Observable<DietologistRecommendation[]> {
        return loadPagedCollection((page, limit) =>
            this.sdk.client
                .getDietologistClientsByClientUserIdRecommendations({ version: this.sdk.version, clientUserId, page, limit })
                .pipe(map(values => values.map(recommendationFromSdk))),
        );
    }

    public disconnectClient(clientUserId: string): Observable<void> {
        return this.sdk.client.deleteDietologistClientsByClientUserId({ version: this.sdk.version, clientUserId });
    }

    public createRecommendation(clientUserId: string, request: CreateRecommendationRequest): Observable<DietologistRecommendation> {
        return this.sdk.client
            .postDietologistClientsByClientUserIdRecommendations({
                version: this.sdk.version,
                clientUserId,
                createRecommendationHttpRequest: request,
            })
            .pipe(map(recommendationFromSdk));
    }

    public getTasksForClient(clientUserId: string): Observable<ClientTask[]> {
        return loadPagedCollection((page, limit) =>
            this.sdk.client
                .getDietologistClientsByClientUserIdTasks({ version: this.sdk.version, clientUserId, page, limit })
                .pipe(map(values => values.map(clientTaskFromSdk))),
        );
    }

    public createTask(clientUserId: string, request: CreateClientTaskRequest): Observable<ClientTask> {
        return this.sdk.client
            .postDietologistClientsByClientUserIdTasks({ version: this.sdk.version, clientUserId, createClientTaskHttpRequest: request })
            .pipe(map(clientTaskFromSdk));
    }

    public cancelTask(taskId: string): Observable<ClientTask> {
        return this.sdk.client.putDietologistClientsTasksByTaskIdCancel({ version: this.sdk.version, taskId }).pipe(map(clientTaskFromSdk));
    }

    public searchRecommendationTemplates(search = '', includeArchived = false): Observable<RecommendationTemplate[]> {
        return loadPagedCollection((page, limit) =>
            this.sdk.client
                .getDietologistRecommendationTemplates({ version: this.sdk.version, search, includeArchived, page, limit })
                .pipe(map(values => values.map(recommendationTemplateFromSdk))),
        );
    }

    public createRecommendationTemplate(request: RecommendationTemplateRequest): Observable<RecommendationTemplate> {
        return this.sdk.client
            .postDietologistRecommendationTemplates({ version: this.sdk.version, recommendationTemplateHttpRequest: request })
            .pipe(map(recommendationTemplateFromSdk));
    }

    public updateRecommendationTemplate(templateId: string, request: RecommendationTemplateRequest): Observable<RecommendationTemplate> {
        return this.sdk.client
            .putDietologistRecommendationTemplatesByTemplateId({
                version: this.sdk.version,
                templateId,
                recommendationTemplateHttpRequest: request,
            })
            .pipe(map(recommendationTemplateFromSdk));
    }

    public archiveRecommendationTemplate(templateId: string): Observable<void> {
        return this.sdk.client.deleteDietologistRecommendationTemplatesByTemplateId({ version: this.sdk.version, templateId });
    }

    public bulkCreateRecommendations(clientUserIds: string[], text: string, idempotencyKey: string): Observable<BulkRecommendationResult> {
        return this.sdk.client
            .postDietologistRecommendationsBulk({
                version: this.sdk.version,
                bulkCreateRecommendationsHttpRequest: { clientUserIds, text, idempotencyKey },
            })
            .pipe(map(bulkRecommendationsFromSdk));
    }

    public invite(request: InviteDietologistRequest): Observable<void> {
        return this.sdk.client.postDietologistInvite({ version: this.sdk.version, inviteDietologistHttpRequest: request });
    }

    public acceptInvitationForCurrentUser(invitationId: string): Observable<void> {
        return this.sdk.client.postDietologistInvitationsByInvitationIdAcceptCurrentUser({ version: this.sdk.version, invitationId });
    }

    public declineInvitationForCurrentUser(invitationId: string): Observable<void> {
        return this.sdk.client.postDietologistInvitationsByInvitationIdDeclineCurrentUser({ version: this.sdk.version, invitationId });
    }

    public updatePermissions(permissions: DietologistPermissions): Observable<void> {
        return this.sdk.client.putDietologistPermissions({
            version: this.sdk.version,
            updateDietologistPermissionsHttpRequest: { permissions },
        });
    }

    public revokeRelationship(): Observable<void> {
        return this.sdk.client.deleteDietologistRelationship({ version: this.sdk.version });
    }
}

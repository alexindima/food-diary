import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

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
import { type DietologistClientDashboardQuery, DietologistService } from '../api/dietologist.service';

@Service()
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

    public getAttentionSignals(settings: AttentionSignalSettings): Observable<AttentionSignal[]> {
        return this.dietologistService.getAttentionSignals(settings);
    }

    public setAttentionSignalState(
        signal: AttentionSignal,
        action: 'Acknowledge' | 'Snooze',
        snoozedUntilUtc: string | null = null,
    ): Observable<void> {
        return this.dietologistService.setAttentionSignalState(signal, action, snoozedUntilUtc);
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

    public getTasksForClient(clientUserId: string): Observable<ClientTask[]> {
        return this.dietologistService.getTasksForClient(clientUserId);
    }

    public createTask(clientUserId: string, request: CreateClientTaskRequest): Observable<ClientTask> {
        return this.dietologistService.createTask(clientUserId, request);
    }

    public cancelTask(taskId: string): Observable<ClientTask> {
        return this.dietologistService.cancelTask(taskId);
    }

    public searchRecommendationTemplates(search = '', includeArchived = false): Observable<RecommendationTemplate[]> {
        return this.dietologistService.searchRecommendationTemplates(search, includeArchived);
    }

    public createRecommendationTemplate(request: RecommendationTemplateRequest): Observable<RecommendationTemplate> {
        return this.dietologistService.createRecommendationTemplate(request);
    }

    public updateRecommendationTemplate(templateId: string, request: RecommendationTemplateRequest): Observable<RecommendationTemplate> {
        return this.dietologistService.updateRecommendationTemplate(templateId, request);
    }

    public archiveRecommendationTemplate(templateId: string): Observable<void> {
        return this.dietologistService.archiveRecommendationTemplate(templateId);
    }

    public bulkCreateRecommendations(clientUserIds: string[], text: string, idempotencyKey: string): Observable<BulkRecommendationResult> {
        return this.dietologistService.bulkCreateRecommendations(clientUserIds, text, idempotencyKey);
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

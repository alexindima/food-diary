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
import type { ClientTaskId, DietologistInvitationId, RecommendationTemplateId, UserId } from '../../../shared/models/semantics/entity-id';
import { type DietologistClientDashboardQuery, DietologistService } from '../api/dietologist.service';
import type { DietologistDashboardSnapshot } from './dietologist-dashboard.data';

@Service()
export class DietologistFacade {
    private readonly dietologistService = inject(DietologistService);

    public getRelationship(): Observable<DietologistRelationship | null> {
        return this.dietologistService.getRelationship();
    }

    public getInvitationForCurrentUser(invitationId: DietologistInvitationId): Observable<DietologistInvitationForCurrentUser> {
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

    public getClientDashboard(clientUserId: UserId, query: DietologistClientDashboardQuery): Observable<DietologistDashboardSnapshot> {
        return this.dietologistService.getClientDashboard(clientUserId, query);
    }

    public getClientGoals(clientUserId: UserId): Observable<DietologistClientGoals> {
        return this.dietologistService.getClientGoals(clientUserId);
    }

    public getRecommendationsForClient(clientUserId: UserId): Observable<DietologistRecommendation[]> {
        return this.dietologistService.getRecommendationsForClient(clientUserId);
    }

    public disconnectClient(clientUserId: UserId): Observable<void> {
        return this.dietologistService.disconnectClient(clientUserId);
    }

    public createRecommendation(clientUserId: UserId, request: CreateRecommendationRequest): Observable<DietologistRecommendation> {
        return this.dietologistService.createRecommendation(clientUserId, request);
    }

    public getTasksForClient(clientUserId: UserId): Observable<ClientTask[]> {
        return this.dietologistService.getTasksForClient(clientUserId);
    }

    public createTask(clientUserId: UserId, request: CreateClientTaskRequest): Observable<ClientTask> {
        return this.dietologistService.createTask(clientUserId, request);
    }

    public cancelTask(taskId: ClientTaskId): Observable<ClientTask> {
        return this.dietologistService.cancelTask(taskId);
    }

    public searchRecommendationTemplates(search = '', includeArchived = false): Observable<RecommendationTemplate[]> {
        return this.dietologistService.searchRecommendationTemplates(search, includeArchived);
    }

    public createRecommendationTemplate(request: RecommendationTemplateRequest): Observable<RecommendationTemplate> {
        return this.dietologistService.createRecommendationTemplate(request);
    }

    public updateRecommendationTemplate(
        templateId: RecommendationTemplateId,
        request: RecommendationTemplateRequest,
    ): Observable<RecommendationTemplate> {
        return this.dietologistService.updateRecommendationTemplate(templateId, request);
    }

    public archiveRecommendationTemplate(templateId: RecommendationTemplateId): Observable<void> {
        return this.dietologistService.archiveRecommendationTemplate(templateId);
    }

    public bulkCreateRecommendations(clientUserIds: UserId[], text: string, idempotencyKey: string): Observable<BulkRecommendationResult> {
        return this.dietologistService.bulkCreateRecommendations(clientUserIds, text, idempotencyKey);
    }

    public invite(request: InviteDietologistRequest): Observable<void> {
        return this.dietologistService.invite(request);
    }

    public acceptInvitationForCurrentUser(invitationId: DietologistInvitationId): Observable<void> {
        return this.dietologistService.acceptInvitationForCurrentUser(invitationId);
    }

    public declineInvitationForCurrentUser(invitationId: DietologistInvitationId): Observable<void> {
        return this.dietologistService.declineInvitationForCurrentUser(invitationId);
    }

    public updatePermissions(permissions: DietologistPermissions): Observable<void> {
        return this.dietologistService.updatePermissions(permissions);
    }

    public revokeRelationship(): Observable<void> {
        return this.dietologistService.revokeRelationship();
    }
}

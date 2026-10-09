import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
    ClientTask,
    ClientTaskStatus,
    CreateRecommendationCommentRequest,
    DietologistRecommendation,
    RecommendationComment,
} from '../../../shared/models/dietologist.data';
import type { ClientTaskId, RecommendationId } from '../../../shared/models/semantics/entity-id';
import { ClientTasksService } from '../api/client-tasks.service';
import { RecommendationsService } from '../api/recommendations.service';

@Service()
export class RecommendationsFacade {
    private readonly recommendationsService = inject(RecommendationsService);
    private readonly clientTasksService = inject(ClientTasksService);

    public getMyRecommendations(): Observable<DietologistRecommendation[]> {
        return this.recommendationsService.getMyRecommendations();
    }

    public markAsRead(recommendationId: RecommendationId): Observable<void> {
        return this.recommendationsService.markAsRead(recommendationId);
    }

    public getComments(recommendationId: RecommendationId): Observable<RecommendationComment[]> {
        return this.recommendationsService.getComments(recommendationId);
    }

    public createComment(
        recommendationId: RecommendationId,
        request: CreateRecommendationCommentRequest,
    ): Observable<RecommendationComment> {
        return this.recommendationsService.createComment(recommendationId, request);
    }

    public getMyTasks(): Observable<ClientTask[]> {
        return this.clientTasksService.getMyTasks();
    }

    public changeTaskStatus(taskId: ClientTaskId, status: Extract<ClientTaskStatus, 'Open' | 'Completed'>): Observable<ClientTask> {
        return this.clientTasksService.changeStatus(taskId, status);
    }
}

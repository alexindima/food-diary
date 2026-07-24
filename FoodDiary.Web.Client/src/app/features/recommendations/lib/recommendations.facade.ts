import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
    ClientTask,
    ClientTaskStatus,
    CreateRecommendationCommentRequest,
    DietologistRecommendation,
    RecommendationComment,
} from '../../../shared/models/dietologist.data';
import { ClientTasksService } from '../api/client-tasks.service';
import { RecommendationsService } from '../api/recommendations.service';

@Service()
export class RecommendationsFacade {
    private readonly recommendationsService = inject(RecommendationsService);
    private readonly clientTasksService = inject(ClientTasksService);

    public getMyRecommendations(): Observable<DietologistRecommendation[]> {
        return this.recommendationsService.getMyRecommendations();
    }

    public markAsRead(recommendationId: string): Observable<void> {
        return this.recommendationsService.markAsRead(recommendationId);
    }

    public getComments(recommendationId: string): Observable<RecommendationComment[]> {
        return this.recommendationsService.getComments(recommendationId);
    }

    public createComment(recommendationId: string, request: CreateRecommendationCommentRequest): Observable<RecommendationComment> {
        return this.recommendationsService.createComment(recommendationId, request);
    }

    public getMyTasks(): Observable<ClientTask[]> {
        return this.clientTasksService.getMyTasks();
    }

    public changeTaskStatus(taskId: string, status: Extract<ClientTaskStatus, 'Open' | 'Completed'>): Observable<ClientTask> {
        return this.clientTasksService.changeStatus(taskId, status);
    }
}

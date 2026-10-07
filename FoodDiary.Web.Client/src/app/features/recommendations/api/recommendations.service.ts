import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { EMPTY, expand, map, type Observable, reduce } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { loadPagedCollection } from '../../../shared/api/load-paged-collection';
import { RecommendationsSdk } from '../../../shared/api/sdk/generated/api/recommendations.service';
import { recommendationCommentFromSdk, recommendationFromSdk } from '../../../shared/api/sdk/recommendation-sdk.mapper';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkPage } from '../../../shared/api/sdk/sdk-response';
import type {
    CreateRecommendationCommentRequest,
    DietologistRecommendation,
    RecommendationComment,
} from '../../../shared/models/dietologist.data';
import type { PageOf } from '../../../shared/models/page-of.data';

@Service()
export class RecommendationsService {
    protected readonly baseUrl = environment.apiUrls.recommendations;
    private readonly sdk = createSdkConnection(RecommendationsSdk, this.baseUrl, inject(HttpClient));

    public getMyRecommendations(): Observable<DietologistRecommendation[]> {
        return loadPagedCollection((page, limit) =>
            this.sdk.client
                .getRecommendations({ version: this.sdk.version, page, limit })
                .pipe(map(values => values.map(recommendationFromSdk))),
        );
    }

    public markAsRead(recommendationId: string): Observable<void> {
        return this.sdk.client.putRecommendationsByRecommendationIdRead({ version: this.sdk.version, recommendationId });
    }

    public getComments(recommendationId: string): Observable<RecommendationComment[]> {
        return this.getCommentPage(recommendationId, 1).pipe(
            expand(response => (response.page < response.totalPages ? this.getCommentPage(recommendationId, response.page + 1) : EMPTY)),
            reduce((comments, response) => [...comments, ...response.data], [] as RecommendationComment[]),
        );
    }

    public createComment(recommendationId: string, request: CreateRecommendationCommentRequest): Observable<RecommendationComment> {
        return this.sdk.client
            .postRecommendationsByRecommendationIdComments({
                version: this.sdk.version,
                recommendationId,
                createRecommendationCommentHttpRequest: request,
            })
            .pipe(map(recommendationCommentFromSdk));
    }

    private getCommentPage(recommendationId: string, page: number): Observable<PageOf<RecommendationComment>> {
        return this.sdk.client
            .getRecommendationsByRecommendationIdComments({ version: this.sdk.version, recommendationId, page, limit: 50 })
            .pipe(map(value => sdkPage(value, recommendationCommentFromSdk)));
    }
}

import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, defer, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { LessonsSdk } from '../../../shared/api/sdk/generated/api/lessons.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkQueryString } from '../../../shared/api/sdk/sdk-query';
import { sdkEnum } from '../../../shared/api/sdk/sdk-response';
import { rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { LessonDetail, LessonPage, LessonQuery } from '../models/lesson.data';
import { lessonDetailFromSdk, lessonPageFromSdk } from './lesson-sdk.mapper';

@Service()
export class LessonService {
    protected readonly baseUrl = environment.apiUrls.lessons;
    private readonly sdk = createSdkConnection(LessonsSdk, this.baseUrl, inject(HttpClient));

    public getAll(query: LessonQuery): Observable<LessonPage> {
        return defer(() => {
            const category = sdkQueryString(query.category?.trim().toLowerCase());
            const difficulty = sdkQueryString(query.difficulty?.trim().toLowerCase());
            return this.sdk.client.getLessons({
                version: this.sdk.version,
                locale: query.locale,
                sort: query.sort,
                page: query.page,
                limit: query.pageSize,
                category:
                    category !== undefined
                        ? sdkEnum(category, [
                              'nutritionbasics',
                              'macronutrients',
                              'micronutrients',
                              'mealtiming',
                              'mindfuleating',
                              'weightmanagement',
                              'hydration',
                              'foodquality',
                              'cookingtips',
                          ] as const)
                        : undefined,
                difficulty: difficulty !== undefined ? sdkEnum(difficulty, ['beginner', 'intermediate', 'advanced'] as const) : undefined,
                search: sdkQueryString(query.search?.trim()),
            });
        }).pipe(
            map(response => lessonPageFromSdk(response, query)),
            catchError((error: unknown) => rethrowApiError('Get lessons error', error)),
        );
    }

    public getById(id: string): Observable<LessonDetail> {
        return this.sdk.client.getLessonsById({ version: this.sdk.version, id }).pipe(
            map(lessonDetailFromSdk),
            catchError((error: unknown) => rethrowApiError('Get lesson error', error)),
        );
    }

    public markRead(id: string): Observable<void> {
        return this.sdk.client
            .postLessonsByIdRead({ version: this.sdk.version, id })
            .pipe(catchError((error: unknown) => rethrowApiError('Mark lesson read error', error)));
    }
}

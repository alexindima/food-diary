import { Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import { addOptionalStringParam, type ApiQueryParams } from '../../../shared/lib/api-query-params.utils';
import type { LessonDetail, LessonPage, LessonQuery, LessonSummary } from '../models/lesson.data';

@Service()
export class LessonService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.lessons;

    public getAll(query: LessonQuery): Observable<LessonPage> {
        const params: ApiQueryParams = { locale: query.locale, sort: query.sort, page: query.page, pageSize: query.pageSize };
        addOptionalStringParam(params, 'category', query.category?.trim());
        addOptionalStringParam(params, 'difficulty', query.difficulty?.trim());
        addOptionalStringParam(params, 'search', query.search?.trim());

        return super.get<LessonPage | LessonSummary[]>('', params).pipe(
            map(response => this.normalizePage(response, query)),
            catchError((error: unknown) => fallbackApiError('Get lessons error', error, this.createEmptyPage(query))),
        );
    }

    public getById(id: string): Observable<LessonDetail> {
        return super.get<LessonDetail>(id).pipe(catchError((error: unknown) => rethrowApiError('Get lesson error', error)));
    }

    public markRead(id: string): Observable<void> {
        return super.post<void>(`${id}/read`, {}).pipe(catchError((error: unknown) => rethrowApiError('Mark lesson read error', error)));
    }

    private normalizePage(response: LessonPage | LessonSummary[], query: LessonQuery): LessonPage {
        if (!Array.isArray(response)) {
            return response;
        }

        const start = (query.page - 1) * query.pageSize;
        return {
            items: response.slice(start, start + query.pageSize),
            page: query.page,
            pageSize: query.pageSize,
            totalCount: response.length,
            totalPages: Math.ceil(response.length / query.pageSize),
            totalLessonCount: response.length,
            readLessonCount: response.filter(lesson => lesson.isRead).length,
            availableCategories: [...new Set(response.map(lesson => lesson.category))],
        };
    }

    private createEmptyPage(query: LessonQuery): LessonPage {
        return {
            items: [],
            page: query.page,
            pageSize: query.pageSize,
            totalCount: 0,
            totalPages: 0,
            totalLessonCount: 0,
            readLessonCount: 0,
            availableCategories: [],
        };
    }
}

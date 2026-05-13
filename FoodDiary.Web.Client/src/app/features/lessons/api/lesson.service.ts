import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { LessonDetail, LessonSummary } from '../models/lesson.data';

@Injectable({ providedIn: 'root' })
export class LessonService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.lessons;

    public getAll(locale: string, category?: string): Observable<LessonSummary[]> {
        const params: Record<string, string> = { locale };
        if (category !== undefined && category.trim().length > 0) {
            params['category'] = category;
        }
        return super
            .get<LessonSummary[]>('', params)
            .pipe(catchError((error: unknown) => fallbackApiError('Get lessons error', error, [])));
    }

    public getById(id: string): Observable<LessonDetail> {
        return super.get<LessonDetail>(id).pipe(catchError((error: unknown) => rethrowApiError('Get lesson error', error)));
    }

    public markRead(id: string): Observable<void> {
        return super.post<void>(`${id}/read`, {}).pipe(catchError((error: unknown) => rethrowApiError('Mark lesson read error', error)));
    }
}

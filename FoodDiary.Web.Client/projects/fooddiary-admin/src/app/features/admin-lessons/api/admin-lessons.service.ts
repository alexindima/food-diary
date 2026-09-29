import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { loadPagedCollection } from '../../../shared/api/load-paged-collection';
import type {
    AdminLesson,
    AdminLessonCreateRequest,
    AdminLessonsImportRequest,
    AdminLessonsImportResponse,
    AdminLessonUpdateRequest,
} from '../models/admin-lesson.data';

@Service()
export class AdminLessonsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/lessons`;

    public getAll(): Observable<AdminLesson[]> {
        return loadPagedCollection((page, limit) => this.http.get<AdminLesson[]>(this.baseUrl, { params: { page, limit } }));
    }

    public create(request: AdminLessonCreateRequest): Observable<AdminLesson> {
        return this.http.post<AdminLesson>(this.baseUrl, request);
    }

    public update(id: string, request: AdminLessonUpdateRequest): Observable<AdminLesson> {
        return this.http.put<AdminLesson>(`${this.baseUrl}/${id}`, request);
    }

    public importLessons(request: AdminLessonsImportRequest): Observable<AdminLessonsImportResponse> {
        const headers = new HttpHeaders({ 'Idempotency-Key': crypto.randomUUID() });
        return this.http.post<AdminLessonsImportResponse>(`${this.baseUrl}/import`, request, { headers });
    }

    public delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }
}

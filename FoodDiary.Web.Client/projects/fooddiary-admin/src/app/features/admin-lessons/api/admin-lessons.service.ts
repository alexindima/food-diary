import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
    AdminLesson,
    AdminLessonCreateRequest,
    AdminLessonsImportRequest,
    AdminLessonsImportResponse,
    AdminLessonUpdateRequest,
} from '../models/admin-lesson.data';

@Injectable({ providedIn: 'root' })
export class AdminLessonsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/lessons`;

    public getAll(): Observable<AdminLesson[]> {
        return this.http.get<AdminLesson[]>(this.baseUrl);
    }

    public create(request: AdminLessonCreateRequest): Observable<AdminLesson> {
        return this.http.post<AdminLesson>(this.baseUrl, request);
    }

    public update(id: string, request: AdminLessonUpdateRequest): Observable<AdminLesson> {
        return this.http.put<AdminLesson>(`${this.baseUrl}/${id}`, request);
    }

    public importLessons(request: AdminLessonsImportRequest): Observable<AdminLessonsImportResponse> {
        return this.http.post<AdminLessonsImportResponse>(`${this.baseUrl}/import`, request);
    }

    public delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }
}

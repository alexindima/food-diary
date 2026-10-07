import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { loadPagedCollection } from '../../../shared/api/load-paged-collection';
import { AdminLessonsSdk } from '../../../shared/api/sdk/generated/api/admin-lessons.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import type {
    AdminLesson,
    AdminLessonCreateRequest,
    AdminLessonsImportRequest,
    AdminLessonsImportResponse,
    AdminLessonUpdateRequest,
} from '../models/admin-lesson.data';
import { adminLessonFromSdk, adminLessonsImportFromSdk } from './admin-lessons-sdk.mapper';

@Service()
export class AdminLessonsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/lessons`;
    private readonly sdk = createSdkConnection(AdminLessonsSdk, this.baseUrl, this.http);

    public getAll(): Observable<AdminLesson[]> {
        return loadPagedCollection((page, limit) =>
            this.sdk.client.getAdminLessons({ version: this.sdk.version, page, limit }).pipe(map(items => items.map(adminLessonFromSdk))),
        );
    }

    public create(request: AdminLessonCreateRequest): Observable<AdminLesson> {
        return this.sdk.client
            .postAdminLessons({ version: this.sdk.version, adminLessonCreateHttpRequest: request })
            .pipe(map(adminLessonFromSdk));
    }

    public update(id: string, request: AdminLessonUpdateRequest): Observable<AdminLesson> {
        return this.sdk.client
            .putAdminLessonsById({ version: this.sdk.version, id, adminLessonUpdateHttpRequest: request })
            .pipe(map(adminLessonFromSdk));
    }

    public importLessons(request: AdminLessonsImportRequest): Observable<AdminLessonsImportResponse> {
        const idempotencyKey = crypto.randomUUID();
        return this.sdk.client
            .postAdminLessonsImport({ version: this.sdk.version, idempotencyKey, adminLessonsImportHttpRequest: request })
            .pipe(map(adminLessonsImportFromSdk));
    }

    public delete(id: string): Observable<void> {
        return this.sdk.client.deleteAdminLessonsById({ version: this.sdk.version, id });
    }
}

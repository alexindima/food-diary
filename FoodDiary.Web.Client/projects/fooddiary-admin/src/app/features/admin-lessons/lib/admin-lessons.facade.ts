import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminLessonsService } from '../api/admin-lessons.service';
import type {
    AdminLesson,
    AdminLessonCreateRequest,
    AdminLessonsImportRequest,
    AdminLessonsImportResponse,
    AdminLessonUpdateRequest,
} from '../models/admin-lesson.data';

@Injectable({ providedIn: 'root' })
export class AdminLessonsFacade {
    private readonly lessonsService = inject(AdminLessonsService);

    public getAll(): Observable<AdminLesson[]> {
        return this.lessonsService.getAll();
    }

    public create(request: AdminLessonCreateRequest): Observable<AdminLesson> {
        return this.lessonsService.create(request);
    }

    public update(id: string, request: AdminLessonUpdateRequest): Observable<AdminLesson> {
        return this.lessonsService.update(id, request);
    }

    public importLessons(request: AdminLessonsImportRequest): Observable<AdminLessonsImportResponse> {
        return this.lessonsService.importLessons(request);
    }

    public delete(id: string): Observable<void> {
        return this.lessonsService.delete(id);
    }
}

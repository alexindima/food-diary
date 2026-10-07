import type { AdminLessonHttpResponse } from '../../../shared/api/sdk/generated/model/admin-lesson-http-response';
import type { AdminLessonsImportHttpResponse } from '../../../shared/api/sdk/generated/model/admin-lessons-import-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import type { AdminLesson, AdminLessonsImportResponse } from '../models/admin-lesson.data';

export function adminLessonFromSdk(response: AdminLessonHttpResponse): AdminLesson {
    const value = requireSdkFields(response, [
        'id',
        'title',
        'content',
        'locale',
        'category',
        'difficulty',
        'estimatedReadMinutes',
        'sortOrder',
        'createdOnUtc',
    ]);
    return { ...value, summary: value.summary ?? null, modifiedOnUtc: value.modifiedOnUtc ?? null };
}

export function adminLessonsImportFromSdk(response: AdminLessonsImportHttpResponse): AdminLessonsImportResponse {
    const value = requireSdkFields(response, ['importedCount', 'lessons']);
    return { ...value, lessons: value.lessons.map(item => adminLessonFromSdk(item)) };
}

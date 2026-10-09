import type { LessonDetailHttpResponse } from '../../../shared/api/sdk/generated/model/lesson-detail-http-response';
import type { LessonPageHttpResponse } from '../../../shared/api/sdk/generated/model/lesson-page-http-response';
import type { LessonSummaryHttpResponse } from '../../../shared/api/sdk/generated/model/lesson-summary-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import { entityId } from '../../../shared/models/semantics/entity-id';
import type { LessonDetail, LessonPage, LessonQuery, LessonSummary } from '../models/lesson.data';

export function lessonSummaryFromSdk(value: LessonSummaryHttpResponse): LessonSummary {
    const lesson = requireSdkFields(value, ['id', 'title', 'category', 'difficulty', 'estimatedReadMinutes', 'isRead']);
    return { ...lesson, id: entityId<'lesson'>(lesson.id) };
}

export function lessonDetailFromSdk(value: LessonDetailHttpResponse): LessonDetail {
    const lesson = requireSdkFields(value, ['id', 'title', 'category', 'difficulty', 'estimatedReadMinutes', 'isRead', 'content']);
    return { ...lesson, id: entityId<'lesson'>(lesson.id) };
}

function isLegacyArray(value: LessonPageHttpResponse | LessonSummaryHttpResponse[]): value is LessonSummaryHttpResponse[] {
    return Array.isArray(value);
}

export function lessonPageFromSdk(response: LessonPageHttpResponse | LessonSummaryHttpResponse[], query: LessonQuery): LessonPage {
    if (!isLegacyArray(response)) {
        const value = requireSdkFields(response, [
            'items',
            'page',
            'pageSize',
            'totalCount',
            'totalPages',
            'totalLessonCount',
            'readLessonCount',
            'availableCategories',
        ]);
        return { ...value, items: value.items.map(lessonSummaryFromSdk) };
    }
    const lessons = response.map(lessonSummaryFromSdk);
    const start = (query.page - 1) * query.pageSize;
    return {
        items: lessons.slice(start, start + query.pageSize),
        page: query.page,
        pageSize: query.pageSize,
        totalCount: lessons.length,
        totalPages: Math.ceil(lessons.length / query.pageSize),
        totalLessonCount: lessons.length,
        readLessonCount: lessons.filter(lesson => lesson.isRead).length,
        availableCategories: [...new Set(lessons.map(lesson => lesson.category))],
    };
}

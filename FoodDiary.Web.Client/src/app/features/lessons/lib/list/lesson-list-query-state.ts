import { InjectionToken, type Signal } from '@angular/core';
import type { Observable } from 'rxjs';

import type { LessonListQuery } from './lesson-list-query';
export type LessonListQueryState = {
    readonly initial: LessonListQuery;
    readonly current: Signal<LessonListQuery>;
    readonly changes: Observable<LessonListQuery>;
    writeAsync: (query: LessonListQuery, options?: { replaceUrl?: boolean }) => Promise<boolean>;
    normalizePageAsync: () => Promise<boolean>;
};
export const LESSON_LIST_QUERY_STATE = new InjectionToken<LessonListQueryState>('LessonListQueryState');

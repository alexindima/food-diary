import type { LessonSummary } from '../../models/lesson.data';

export type LessonListItem = {
    categoryLabelKey: string;
    difficultyLabelKey: string;
} & LessonSummary;

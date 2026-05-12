import type { LessonSummary } from '../../models/lesson.data';

export interface LessonListItem extends LessonSummary {
    categoryLabelKey: string;
    difficultyLabelKey: string;
}

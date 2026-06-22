import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const LESSON_DETAIL_TOUR: LocalizedTourConfig = {
    id: 'lesson-detail',
    translationRoot: 'LESSONS.DETAIL_TOUR',
    steps: [
        {
            id: 'content',
            target: 'lesson-detail-content',
            titleKey: 'CONTENT_TITLE',
            descriptionKey: 'CONTENT_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'help',
            target: 'lesson-detail-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};

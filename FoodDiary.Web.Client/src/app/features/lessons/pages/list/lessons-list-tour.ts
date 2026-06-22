import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const LESSONS_LIST_TOUR: LocalizedTourConfig = {
    id: 'lessons-list',
    translationRoot: 'LESSONS.TOUR',
    steps: [
        {
            id: 'progress',
            target: 'lessons-progress',
            titleKey: 'PROGRESS_TITLE',
            descriptionKey: 'PROGRESS_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'filters',
            target: 'lessons-filters',
            titleKey: 'FILTERS_TITLE',
            descriptionKey: 'FILTERS_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'grid',
            target: 'lessons-grid',
            titleKey: 'GRID_TITLE',
            descriptionKey: 'GRID_DESCRIPTION',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'lessons-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};

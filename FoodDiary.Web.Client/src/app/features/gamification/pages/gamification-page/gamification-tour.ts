import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const GAMIFICATION_TOUR: LocalizedTourConfig = {
    id: 'gamification',
    translationRoot: 'GAMIFICATION.TOUR',
    steps: [
        {
            id: 'stats',
            target: 'gamification-stats',
            titleKey: 'STATS_TITLE',
            descriptionKey: 'STATS_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'health-score',
            target: 'gamification-health-score',
            titleKey: 'HEALTH_SCORE_TITLE',
            descriptionKey: 'HEALTH_SCORE_DESCRIPTION',
            placement: 'right',
        },
        {
            id: 'badges',
            target: 'gamification-badges',
            titleKey: 'BADGES_TITLE',
            descriptionKey: 'BADGES_DESCRIPTION',
            placement: 'left',
        },
        {
            id: 'help',
            target: 'gamification-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};

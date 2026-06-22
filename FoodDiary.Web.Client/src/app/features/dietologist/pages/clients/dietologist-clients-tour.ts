import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const DIETOLOGIST_CLIENTS_TOUR: LocalizedTourConfig = {
    id: 'dietologist-clients',
    translationRoot: 'DIETOLOGIST.CLIENTS.TOUR',
    steps: [
        {
            id: 'clients',
            target: 'dietologist-clients-list',
            titleKey: 'CLIENTS_TITLE',
            descriptionKey: 'CLIENTS_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'help',
            target: 'dietologist-clients-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};

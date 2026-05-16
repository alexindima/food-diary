import type { ClientSummary } from '../../../../../shared/models/dietologist.data';

export type ClientCardViewModel = {
    client: ClientSummary;
    title: string;
    initials: string;
    connectedDateLabel: string;
};

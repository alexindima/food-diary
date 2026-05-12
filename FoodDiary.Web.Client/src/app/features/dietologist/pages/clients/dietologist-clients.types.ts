import type { ClientSummary } from '../../../../shared/models/dietologist.data';

export interface ClientCardViewModel {
    client: ClientSummary;
    title: string;
    initials: string;
    connectedDateLabel: string;
}

import type { ClientSummary } from '../../../../../shared/models/dietologist.data';
import { formatDietologistDate } from '../../../lib/dietologist-date.utils';
import type { ClientCardViewModel } from './dietologist-clients.types';

const INITIALS_PART_LIMIT = 2;

export function buildClientCardViewModels(clients: ClientSummary[], language: string): ClientCardViewModel[] {
    return clients.map(client => ({
        client,
        title: getClientTitle(client),
        initials: getClientInitials(client),
        connectedDateLabel: formatDietologistDate(client.acceptedAtUtc, language),
    }));
}

export function getClientTitle(client: ClientSummary): string | null {
    const fullName = `${client.firstName ?? ''} ${client.lastName ?? ''}`.trim();
    return fullName.length > 0 ? fullName : client.email;
}

export function getClientInitials(client: ClientSummary): string {
    const parts = [client.firstName, client.lastName].filter((value): value is string => Boolean(value?.trim()));
    if (parts.length === 0) {
        return client.email?.charAt(0).toUpperCase() ?? '?';
    }

    return parts
        .slice(0, INITIALS_PART_LIMIT)
        .map(value => value.trim().charAt(0).toUpperCase())
        .join('');
}

import { resolveAppLocale } from '../../../../../shared/lib/locale.constants';
import type { ClientSummary } from '../../../../../shared/models/dietologist.data';
import type { ClientCardViewModel } from './dietologist-clients.types';

const INITIALS_PART_LIMIT = 2;

export function buildClientCardViewModels(clients: ClientSummary[], language: string): ClientCardViewModel[] {
    return clients.map(client => ({
        client,
        title: getClientTitle(client),
        initials: getClientInitials(client),
        connectedDateLabel: formatClientConnectedDate(client.acceptedAtUtc, language),
    }));
}

export function getClientTitle(client: ClientSummary): string {
    const fullName = `${client.firstName ?? ''} ${client.lastName ?? ''}`.trim();
    return fullName.length > 0 ? fullName : client.email;
}

export function getClientInitials(client: ClientSummary): string {
    const parts = [client.firstName, client.lastName].filter((value): value is string => Boolean(value?.trim()));
    if (parts.length === 0) {
        return client.email.charAt(0).toUpperCase();
    }

    return parts
        .slice(0, INITIALS_PART_LIMIT)
        .map(value => value.trim().charAt(0).toUpperCase())
        .join('');
}

export function formatClientConnectedDate(value: string, language: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return value;
    }

    return new Intl.DateTimeFormat(resolveAppLocale(language), {
        dateStyle: 'medium',
    }).format(date);
}

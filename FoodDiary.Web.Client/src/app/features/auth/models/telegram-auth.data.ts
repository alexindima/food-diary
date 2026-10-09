import { type TelegramLoginTicket, telegramLoginTicket } from '../../../shared/auth/telegram-oidc-values';

export type TelegramConfiguration = {
    loginEnabled: boolean;
    registrationEnabled: boolean;
    oidcEnabled: boolean;
};

export type TelegramIntent = {
    ticket: TelegramLoginTicket;
    nextAction: 'login' | 'link' | 'onboarding';
    expiresAtUtc: string;
};

const TELEGRAM_TICKET_LENGTH = 43;

export function telegramIntentFromStorage(value: unknown): TelegramIntent | null {
    return isTelegramIntent(value) ? { ...value, ticket: telegramLoginTicket(value.ticket) } : null;
}

export function isTelegramIntent(value: unknown): value is TelegramIntent {
    if (typeof value !== 'object' || value === null) {
        return false;
    }
    return (
        'ticket' in value &&
        typeof value.ticket === 'string' &&
        value.ticket.length === TELEGRAM_TICKET_LENGTH &&
        'nextAction' in value &&
        isIntentAction(value.nextAction) &&
        'expiresAtUtc' in value &&
        typeof value.expiresAtUtc === 'string' &&
        Date.parse(value.expiresAtUtc) > Date.now()
    );
}

function isIntentAction(value: unknown): boolean {
    return value === 'login' || value === 'link' || value === 'onboarding';
}

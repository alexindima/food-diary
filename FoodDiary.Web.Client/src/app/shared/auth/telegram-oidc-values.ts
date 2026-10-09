/* eslint-disable @typescript-eslint/no-unsafe-type-assertion -- boundary constructors attach meanings without changing OAuth or ticket encodings */
import type { SemanticString, UnbrandedString } from '../models/semantics/string-meaning';

export type TelegramOAuthCode = SemanticString<'telegram-oauth-code'>;
export type TelegramOAuthState = SemanticString<'telegram-oauth-state'>;
export type TelegramLoginTicket = SemanticString<'telegram-login-ticket'>;

export function telegramOAuthCode(value: UnbrandedString | TelegramOAuthCode): TelegramOAuthCode {
    return value as TelegramOAuthCode;
}

export function telegramOAuthState(value: UnbrandedString | TelegramOAuthState): TelegramOAuthState {
    return value as TelegramOAuthState;
}

export function telegramLoginTicket(value: UnbrandedString | TelegramLoginTicket): TelegramLoginTicket {
    return value as TelegramLoginTicket;
}

export function telegramOAuthCallbackFromRoute(
    code: UnbrandedString | null,
    state: UnbrandedString | null,
): { code: TelegramOAuthCode | null; state: TelegramOAuthState | null } {
    return {
        code: code === null ? null : telegramOAuthCode(code),
        state: state === null ? null : telegramOAuthState(state),
    };
}

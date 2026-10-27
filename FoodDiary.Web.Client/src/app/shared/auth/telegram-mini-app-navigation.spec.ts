import { describe, expect, it } from 'vitest';

import { telegramMiniAppEntryUrl, telegramMiniAppReturnUrl } from './telegram-mini-app-navigation';

describe('Telegram Mini App navigation', () => {
    it.each(['/', '/ru'])('opens the diary entry without returning to the public landing %s', path => {
        expect(telegramMiniAppEntryUrl(`https://fooddiary.example${path}#tgWebAppData=proof`)).toBe('/auth/telegram#tgWebAppData=proof');
    });

    it('preserves a private destination and the complete Telegram fragment separately', () => {
        expect(
            telegramMiniAppEntryUrl('https://fooddiary.example/profile?tab=settings#tgWebAppData=proof%26hash&tgWebAppVersion=8.0'),
        ).toBe('/auth/telegram?returnUrl=%2Fprofile%3Ftab%3Dsettings#tgWebAppData=proof%26hash&tgWebAppVersion=8.0');
    });

    it.each([
        '/auth/telegram',
        '/auth/telegram/callback?code=proof&state=state',
        '/verify-email?token=proof',
        '/reset-password',
        '/verify-pending',
        '/mobile/login',
        '/change-password-required',
    ])('preserves the explicit authentication route %s', path => {
        expect(telegramMiniAppEntryUrl(`https://fooddiary.example${path}#tgWebAppData=proof`)).toBeNull();
    });

    it.each([
        null,
        '',
        '/',
        '/ru',
        'https://evil.example',
        '//evil.example',
        '/\\evil.example',
        '/profile\n',
        '/auth/telegram',
        '/reset-password?token=proof',
    ])('rejects external, unsafe and looping return destinations %s', value => {
        expect(telegramMiniAppReturnUrl(value)).toBeNull();
    });

    it.each(['/profile', '/meals/123?mode=edit', '/dashboard#today'])('keeps the internal destination %s', value => {
        expect(telegramMiniAppReturnUrl(value)).toBe(value);
    });
});

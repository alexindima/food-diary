import { describe, expect, it } from 'vitest';

import { sanitizeMarketingLandingPath } from './marketing-landing-path';

describe('sanitizeMarketingLandingPath', () => {
    it.each(['/verify-email', '/reset-password', '/auth/telegram', '/auth/telegram/callback', '/mobile/login', '/login', '/ru/login'])(
        'retains only campaign parameters on %s',
        path => {
            const landing = `${path}?token=verification-secret&userId=user&code=oauth-secret&state=oauth-state&utm_source=telegram#token=fragment-secret`;

            expect(sanitizeMarketingLandingPath(landing)).toBe(`${path}?utm_source=telegram`);
        },
    );

    it('drops encoded parameter names and nested return URLs while preserving repeated campaigns', () => {
        expect(
            sanitizeMarketingLandingPath(
                '/?%74oken=secret&%63ode=secret&returnUrl=%2Fverify-email%3Ftoken%3Dnested&utm%5Fsource=telegram&utm_term=x%2By+%26+z&utm_term=second',
            ),
        ).toBe('/?utm_source=telegram&utm_term=x%2By+%26+z&utm_term=second');
    });

    it.each(['javascript:secret', 'http://[invalid', 'data:text/plain,secret'])('uses a safe fallback for invalid input %s', value => {
        expect(sanitizeMarketingLandingPath(value)).toBe('/');
    });
});

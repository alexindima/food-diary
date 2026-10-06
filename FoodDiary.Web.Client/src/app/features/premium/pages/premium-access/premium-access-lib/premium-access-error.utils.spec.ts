import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';

import { resolvePremiumErrorMessage } from './premium-access-error.utils';

describe('resolvePremiumErrorMessage', () => {
    it('uses localized copy instead of an unlocalized provider message', () => {
        const error = new HttpErrorResponse({
            status: 400,
            error: { message: ' Checkout failed ' },
        });

        expect(resolvePremiumErrorMessage(error, 'fallback')).toBe('fallback');
    });

    it('does not expose a raw provider response', () => {
        const error = new HttpErrorResponse({
            status: 400,
            error: ' Portal failed ',
        });

        expect(resolvePremiumErrorMessage(error, 'fallback')).toBe('fallback');
    });

    it('keeps technical errors out of user copy', () => {
        expect(resolvePremiumErrorMessage(new Error('Missing URL'), 'fallback')).toBe('fallback');
        expect(resolvePremiumErrorMessage(new Error(' '), 'fallback')).toBe('fallback');
        expect(resolvePremiumErrorMessage({}, 'fallback')).toBe('fallback');
    });
});

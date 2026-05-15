import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';

import { resolvePremiumErrorMessage } from './premium-access-error.utils';

describe('resolvePremiumErrorMessage', () => {
    it('uses API payload message when available', () => {
        const error = new HttpErrorResponse({
            status: 400,
            error: { message: ' Checkout failed ' },
        });

        expect(resolvePremiumErrorMessage(error, 'fallback')).toBe('Checkout failed');
    });

    it('uses string API payload when available', () => {
        const error = new HttpErrorResponse({
            status: 400,
            error: ' Portal failed ',
        });

        expect(resolvePremiumErrorMessage(error, 'fallback')).toBe('Portal failed');
    });

    it('uses Error message or fallback', () => {
        expect(resolvePremiumErrorMessage(new Error('Missing URL'), 'fallback')).toBe('Missing URL');
        expect(resolvePremiumErrorMessage(new Error(' '), 'fallback')).toBe('fallback');
        expect(resolvePremiumErrorMessage({}, 'fallback')).toBe('fallback');
    });
});

import { firstValueFrom } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { fallbackApiError, rethrowApiError } from './api-error.utils';

const ERROR_MESSAGE = 'Request failed';
const ERROR_VALUE = new Error('Boom');

describe('api error utils', () => {
    const originalGlobalHandlerFlag = environment.enableGlobalErrorHandler;

    afterEach(() => {
        environment.enableGlobalErrorHandler = originalGlobalHandlerFlag;
        vi.restoreAllMocks();
    });

    it('logs and returns fallback value when global handler is disabled', async () => {
        environment.enableGlobalErrorHandler = false;
        const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => undefined);

        await expect(firstValueFrom(fallbackApiError(ERROR_MESSAGE, ERROR_VALUE, 'fallback'))).resolves.toBe('fallback');

        expect(consoleSpy).toHaveBeenCalledWith(ERROR_MESSAGE, ERROR_VALUE);
    });

    it('logs and rethrows source error when global handler is disabled', async () => {
        environment.enableGlobalErrorHandler = false;
        const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => undefined);

        await expect(firstValueFrom(rethrowApiError(ERROR_MESSAGE, ERROR_VALUE))).rejects.toBe(ERROR_VALUE);

        expect(consoleSpy).toHaveBeenCalledWith(ERROR_MESSAGE, ERROR_VALUE);
    });

    it('skips fallback logging when global handler is enabled', async () => {
        environment.enableGlobalErrorHandler = true;
        const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => undefined);

        await expect(firstValueFrom(fallbackApiError(ERROR_MESSAGE, ERROR_VALUE, 'fallback'))).resolves.toBe('fallback');

        expect(consoleSpy).not.toHaveBeenCalled();
    });
});

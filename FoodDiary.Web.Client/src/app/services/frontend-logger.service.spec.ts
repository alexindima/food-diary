import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../environments/environment';
import { FrontendLoggerService } from './frontend-logger.service';

const MESSAGE = 'message';
const ERROR = new Error('Boom');

describe('FrontendLoggerService', () => {
    const originalBuildVersion = environment.buildVersion;

    afterEach(() => {
        environment.buildVersion = originalBuildVersion;
        vi.restoreAllMocks();
    });

    it('writes warning and error messages through controlled console boundary', () => {
        const service = setup();
        const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
        const errorSpy = vi.spyOn(console, 'error').mockImplementation(() => undefined);

        service.warn(MESSAGE, ERROR);
        service.error(MESSAGE, ERROR);

        expect(warnSpy).toHaveBeenCalledWith(MESSAGE, ERROR);
        expect(errorSpy).toHaveBeenCalledWith(MESSAGE, ERROR);
    });

    it('skips dev-only logging outside dev builds', () => {
        environment.buildVersion = 'production';
        const service = setup();
        const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => undefined);

        service.warn(MESSAGE, ERROR, { devOnly: true });

        expect(warnSpy).not.toHaveBeenCalled();
    });
});

function setup(): FrontendLoggerService {
    TestBed.configureTestingModule({ providers: [FrontendLoggerService] });
    return TestBed.inject(FrontendLoggerService);
}

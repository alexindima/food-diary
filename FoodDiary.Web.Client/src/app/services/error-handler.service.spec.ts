import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BrowserWindowService } from '../shared/platform/browser-window.service';
import { GlobalErrorHandler } from './error-handler.service';
import { FrontendObservabilityService } from './frontend-observability.service';

type FrontendObservabilityServiceMock = {
    recordClientError: ReturnType<typeof vi.fn<FrontendObservabilityService['recordClientError']>>;
};

type BrowserWindowServiceMock = {
    getHref: ReturnType<typeof vi.fn<BrowserWindowService['getHref']>>;
};

describe('GlobalErrorHandler', () => {
    let handler: GlobalErrorHandler;
    let browserWindowSpy: BrowserWindowServiceMock;
    let observabilitySpy: FrontendObservabilityServiceMock;

    beforeEach(() => {
        browserWindowSpy = { getHref: vi.fn().mockReturnValue('https://fooddiary.test/dashboard') };
        observabilitySpy = { recordClientError: vi.fn() };

        TestBed.configureTestingModule({
            providers: [
                GlobalErrorHandler,
                { provide: BrowserWindowService, useValue: browserWindowSpy },
                { provide: FrontendObservabilityService, useValue: observabilitySpy },
            ],
        });

        handler = TestBed.inject(GlobalErrorHandler);
    });

    it('should call loggingService.logError with error payload', () => {
        const error = new Error('Test error');
        handler.handleError(error);

        expect(observabilitySpy.recordClientError).toHaveBeenCalledTimes(1);
        const payload = observabilitySpy.recordClientError.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['location']).toBe('https://fooddiary.test/dashboard');
    });

    it('should include error message in payload', () => {
        const error = new Error('Something went wrong');
        handler.handleError(error);

        const payload = observabilitySpy.recordClientError.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['message']).toBe('Something went wrong');
    });

    it("should use 'Unknown error' when no message", () => {
        handler.handleError({});

        const payload = observabilitySpy.recordClientError.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['message']).toBe('Unknown error');
    });

    it('should include stack trace in payload', () => {
        const error = new Error('With stack');
        handler.handleError(error);

        const payload = observabilitySpy.recordClientError.mock.calls.at(-1)?.[0] as Record<string, unknown>;
        expect(payload['stack']).toBeTruthy();
        expect((payload['stack'] as string).length).toBeGreaterThan(0);
    });

    it('should suppress duplicate errors in a short burst', () => {
        const error = new Error('Repeated runtime error');

        handler.handleError(error);
        handler.handleError(error);

        expect(observabilitySpy.recordClientError).toHaveBeenCalledTimes(1);
    });

    it('should handle logging failure gracefully', () => {
        observabilitySpy.recordClientError.mockImplementation(() => {
            throw new Error('Network error');
        });
        vi.spyOn(console, 'error');

        expect(() => {
            handler.handleError(new Error('Test'));
        }).not.toThrow();
        // eslint-disable-next-line no-console -- test verifies fallback logging when backend reporting fails
        expect(console.error).toHaveBeenCalledWith('Failed to send log to backend:', expect.any(Error));
    });
});

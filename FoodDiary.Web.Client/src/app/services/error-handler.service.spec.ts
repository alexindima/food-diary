import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ErrorHandler } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../environments/environment';
import { SKIP_AUTH } from '../constants/http-context.tokens';
import { SKIP_OBSERVABILITY } from '../constants/observability-context.tokens';
import { ClientTelemetrySessionService } from '../shared/observability/client-telemetry-session.service';
import { BrowserWindowService } from '../shared/platform/browser-window.service';
import { GlobalErrorHandler } from './error-handler.service';
import { FrontendObservabilityService } from './frontend-observability.service';

const ERROR_QUIET_INTERVAL_MS = 10_000;
const REPORTING_UNAVAILABLE_STATUS = 503;

type FrontendObservabilityServiceMock = {
    recordClientError: ReturnType<typeof vi.fn<FrontendObservabilityService['recordClientError']>>;
};

type BrowserWindowServiceMock = {
    getHref: ReturnType<typeof vi.fn<BrowserWindowService['getHref']>>;
};

afterEach(() => {
    vi.restoreAllMocks();
});

describe('GlobalErrorHandler', () => {
    let handler: GlobalErrorHandler;
    let observabilitySpy: FrontendObservabilityServiceMock;

    beforeEach(() => {
        ({ handler, observabilitySpy } = setupMockReporting());
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

describe('GlobalErrorHandler diagnostic normalization', () => {
    it('reports a string rejection when the browser location is unavailable', () => {
        const { handler, browserWindowSpy, observabilitySpy } = setupMockReporting();
        browserWindowSpy.getHref.mockReturnValue(null);

        handler.handleError('Renderer unavailable');

        expect(observabilitySpy.recordClientError).toHaveBeenCalledExactlyOnceWith({
            message: 'Renderer unavailable',
            stack: '',
            location: '',
        });
    });

    it('reports a native Error with an unavailable stack trace', () => {
        const { handler, observabilitySpy } = setupMockReporting();
        const error = new Error('Rendering failed');
        delete error.stack;

        handler.handleError(error);

        expect(observabilitySpy.recordClientError).toHaveBeenCalledExactlyOnceWith({
            message: 'Rendering failed',
            stack: '',
            location: 'https://fooddiary.test/dashboard',
        });
    });

    it('keeps a diagnostic stack from a serialized worker error', () => {
        const { handler, observabilitySpy } = setupMockReporting();
        handler.handleError({ message: 'Worker rendering failed', stack: 'at renderCard (worker.js:20:2)' });

        expect(observabilitySpy.recordClientError).toHaveBeenCalledExactlyOnceWith({
            message: 'Worker rendering failed',
            stack: 'at renderCard (worker.js:20:2)',
            location: 'https://fooddiary.test/dashboard',
            details: { kind: 'non-error-object' },
        });
    });

    it('reports a recurring diagnostic again after a full quiet interval', () => {
        const { handler, observabilitySpy } = setupMockReporting();
        const now = vi.spyOn(Date, 'now').mockReturnValue(0);
        const error = new Error('Renderer unavailable');
        handler.handleError(error);

        const recentDuplicateAt = ERROR_QUIET_INTERVAL_MS - 1;
        now.mockReturnValue(recentDuplicateAt);
        handler.handleError(error);
        expect(observabilitySpy.recordClientError).toHaveBeenCalledOnce();

        now.mockReturnValue(recentDuplicateAt + ERROR_QUIET_INTERVAL_MS);
        handler.handleError(error);
        expect(observabilitySpy.recordClientError).toHaveBeenCalledTimes(2);
    });
});

describe('GlobalErrorHandler HTTP reporting', () => {
    it('sends a sanitized diagnostic and can report again after the logging endpoint fails', () => {
        const originalObservability = environment.enableClientObservability;
        environment.enableClientObservability = true;
        try {
            TestBed.configureTestingModule({
                providers: [
                    provideHttpClient(),
                    provideHttpClientTesting(),
                    provideRouter([]),
                    { provide: ErrorHandler, useClass: GlobalErrorHandler },
                    {
                        provide: BrowserWindowService,
                        useValue: { getHref: (): string => 'https://fooddiary.test/diary/42?entry=sample-query#sample-fragment' },
                    },
                    { provide: ClientTelemetrySessionService, useValue: { getSessionId: (): string => 'fd-session-test' } },
                ],
            });
            const handler = TestBed.inject(ErrorHandler);
            const http = TestBed.inject(HttpTestingController);

            handler.handleError('Failed to render person@example.test');
            const request = http.expectOne({ method: 'POST', url: environment.apiUrls.logs });
            expect(request.request.body).toMatchObject({
                category: 'client_error',
                name: 'global-error',
                level: 'error',
                message: 'Failed to render [redacted]',
                stack: '',
                location: '/diary/:id',
                sessionId: 'fd-session-test',
            });
            expect(request.request.context.get(SKIP_AUTH)).toBe(true);
            expect(request.request.context.get(SKIP_OBSERVABILITY)).toBe(true);
            request.flush(null, { status: REPORTING_UNAVAILABLE_STATUS, statusText: 'Service Unavailable' });

            handler.handleError('Could not refresh the diary');
            const nextRequest = http.expectOne({ method: 'POST', url: environment.apiUrls.logs });
            expect(nextRequest.request.body).toMatchObject({ message: 'Could not refresh the diary' });
            nextRequest.flush(null);
            http.verify();
        } finally {
            environment.enableClientObservability = originalObservability;
        }
    });
});

function setupMockReporting(): {
    handler: GlobalErrorHandler;
    browserWindowSpy: BrowserWindowServiceMock;
    observabilitySpy: FrontendObservabilityServiceMock;
} {
    const browserWindowSpy: BrowserWindowServiceMock = { getHref: vi.fn().mockReturnValue('https://fooddiary.test/dashboard') };
    const observabilitySpy: FrontendObservabilityServiceMock = { recordClientError: vi.fn() };

    TestBed.configureTestingModule({
        providers: [
            GlobalErrorHandler,
            { provide: BrowserWindowService, useValue: browserWindowSpy },
            { provide: FrontendObservabilityService, useValue: observabilitySpy },
        ],
    });

    return { handler: TestBed.inject(GlobalErrorHandler), browserWindowSpy, observabilitySpy };
}

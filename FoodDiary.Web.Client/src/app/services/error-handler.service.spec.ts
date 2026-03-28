import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { GlobalErrorHandler } from './error-handler.service';
import { LoggingApiService } from './logging-api.service';

describe('GlobalErrorHandler', () => {
    let handler: GlobalErrorHandler;
    let loggingSpy: any;

    beforeEach(() => {
        loggingSpy = { logError: vi.fn() } as any;
        loggingSpy.logError.mockReturnValue(of(undefined));

        TestBed.configureTestingModule({
            providers: [GlobalErrorHandler, { provide: LoggingApiService, useValue: loggingSpy }],
        });

        handler = TestBed.inject(GlobalErrorHandler);
    });

    it('should call loggingService.logError with error payload', () => {
        const error = new Error('Test error');
        handler.handleError(error);

        expect(loggingSpy.logError).toHaveBeenCalledTimes(1);
        const payload = loggingSpy.logError.mock.calls.at(-1)![0] as Record<string, unknown>;
        expect(payload['level']).toBe('error');
        expect(payload['timestamp']).toBeDefined();
        expect(payload['location']).toBeDefined();
    });

    it('should include error message in payload', () => {
        const error = new Error('Something went wrong');
        handler.handleError(error);

        const payload = loggingSpy.logError.mock.calls.at(-1)![0] as Record<string, unknown>;
        expect(payload['message']).toBe('Something went wrong');
    });

    it('should use \'Unknown error\' when no message', () => {
        handler.handleError({});

        const payload = loggingSpy.logError.mock.calls.at(-1)![0] as Record<string, unknown>;
        expect(payload['message']).toBe('Unknown error');
    });

    it('should include stack trace in payload', () => {
        const error = new Error('With stack');
        handler.handleError(error);

        const payload = loggingSpy.logError.mock.calls.at(-1)![0] as Record<string, unknown>;
        expect(payload['stack']).toBeTruthy();
        expect((payload['stack'] as string).length).toBeGreaterThan(0);
    });

    it('should handle logging failure gracefully', () => {
        loggingSpy.logError.mockReturnValue(throwError(() => new Error('Network error')));
        vi.spyOn(console, 'error');

        expect(() => handler.handleError(new Error('Test'))).not.toThrow();
        expect(console.error).toHaveBeenCalledWith('Failed to send log to backend:', expect.any(Error));
    });
});

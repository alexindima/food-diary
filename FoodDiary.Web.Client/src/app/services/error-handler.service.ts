import { Injectable, ErrorHandler, inject } from '@angular/core';
import { LoggingApiService } from './logging-api.service';

type ClientErrorPayload = {
    message: string;
    level: 'error';
    stack: string;
    timestamp: string;
    location: string;
};

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
    private readonly loggingService = inject(LoggingApiService);

    public handleError(error: unknown): void {
        this.loggingService.logError(this.buildErrorPayload(error)).subscribe({
            error: err => console.error('Failed to send log to backend:', err),
        });
    }

    private buildErrorPayload(error: unknown): ClientErrorPayload {
        const location = typeof window !== 'undefined' ? window.location.href : '';

        if (error instanceof Error) {
            return {
                message: error.message || 'Unknown error',
                level: 'error',
                stack: error.stack || '',
                timestamp: new Date().toISOString(),
                location,
            };
        }

        if (typeof error === 'object' && error !== null) {
            const candidate = error as { message?: unknown; stack?: unknown };
            return {
                message: typeof candidate.message === 'string' && candidate.message ? candidate.message : 'Unknown error',
                level: 'error',
                stack: typeof candidate.stack === 'string' ? candidate.stack : '',
                timestamp: new Date().toISOString(),
                location,
            };
        }

        return {
            message: typeof error === 'string' && error ? error : 'Unknown error',
            level: 'error',
            stack: '',
            timestamp: new Date().toISOString(),
            location,
        };
    }
}

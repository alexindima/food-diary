import { type ErrorHandler, inject, Injectable } from '@angular/core';

import { FrontendLoggerService } from './frontend-logger.service';
import { FrontendObservabilityService } from './frontend-observability.service';

@Injectable({ providedIn: 'root' })
export class GlobalErrorHandler implements ErrorHandler {
    private readonly frontendObservabilityService = inject(FrontendObservabilityService);
    private readonly logger = inject(FrontendLoggerService);

    public handleError(error: unknown): void {
        try {
            this.frontendObservabilityService.recordClientError(this.buildErrorPayload(error));
        } catch (err) {
            this.logger.error('Failed to send log to backend:', err);
        }
    }

    private buildErrorPayload(error: unknown): {
        message: string;
        stack: string;
        location: string;
        details?: Record<string, unknown>;
    } {
        const location = typeof window !== 'undefined' ? window.location.href : '';

        if (error instanceof Error) {
            return {
                message: this.messageOrUnknown(error.message),
                stack: error.stack ?? '',
                location,
            };
        }

        if (typeof error === 'object' && error !== null) {
            const candidate = error as { message?: unknown; stack?: unknown };
            return {
                message: this.messageOrUnknown(candidate.message),
                stack: typeof candidate.stack === 'string' ? candidate.stack : '',
                location,
                details: {
                    kind: 'non-error-object',
                },
            };
        }

        return {
            message: this.messageOrUnknown(error),
            stack: '',
            location,
        };
    }

    private messageOrUnknown(value: unknown): string {
        return typeof value === 'string' && value.length > 0 ? value : 'Unknown error';
    }
}

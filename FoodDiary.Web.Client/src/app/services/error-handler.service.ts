import { type ErrorHandler, inject, Service } from '@angular/core';

import { BrowserWindowService } from '../shared/platform/browser-window.service';
import { FrontendLoggerService } from './frontend-logger.service';
import { FrontendObservabilityService } from './frontend-observability.service';

const DUPLICATE_ERROR_SUPPRESSION_MS = 10_000;

@Service()
export class GlobalErrorHandler implements ErrorHandler {
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly frontendObservabilityService = inject(FrontendObservabilityService);
    private readonly logger = inject(FrontendLoggerService);
    private lastErrorKey: string | null = null;
    private lastErrorReportedAt = 0;

    public handleError(error: unknown): void {
        try {
            const payload = this.buildErrorPayload(error);
            if (this.shouldSuppressDuplicate(payload)) {
                return;
            }

            this.frontendObservabilityService.recordClientError(payload);
        } catch (error_) {
            this.logger.error('Failed to send log to backend:', error_);
        }
    }

    private buildErrorPayload(error: unknown): {
        message: string;
        stack: string;
        location: string;
        details?: Record<string, unknown>;
    } {
        const location = this.browserWindow.getHref() ?? '';

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

    private shouldSuppressDuplicate(error: { message: string; stack: string; location: string }): boolean {
        const key = `${error.message}|${error.stack}|${error.location}`;
        const now = Date.now();
        const shouldSuppress = key === this.lastErrorKey && now - this.lastErrorReportedAt < DUPLICATE_ERROR_SUPPRESSION_MS;

        this.lastErrorKey = key;
        this.lastErrorReportedAt = now;

        return shouldSuppress;
    }
}

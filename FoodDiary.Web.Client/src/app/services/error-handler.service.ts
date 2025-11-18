import { Injectable, ErrorHandler, inject } from '@angular/core';
import { LoggingApiService } from './logging-api.service';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
    private readonly loggingService = inject(LoggingApiService);

    public handleError(error: any): void {
        const errorPayload = {
            message: error.message || 'Unknown error',
            level: 'error',
            stack: error.stack || '',
            timestamp: new Date().toISOString(),
            location: window.location.href,
        };

        this.loggingService.logError(errorPayload).subscribe({
            error: err => console.error('Failed to send log to backend:', err),
        });
    }
}

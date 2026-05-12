import { Injectable } from '@angular/core';

import { environment } from '../../environments/environment';

type FrontendLoggerOptions = {
    devOnly?: boolean;
};

@Injectable({
    providedIn: 'root',
})
export class FrontendLoggerService {
    public warn(message: string, error?: unknown, options?: FrontendLoggerOptions): void {
        if (!this.shouldLog(options)) {
            return;
        }

        // eslint-disable-next-line no-console -- This service is the controlled console logging boundary.
        console.warn(message, error);
    }

    public error(message: string, error?: unknown, options?: FrontendLoggerOptions): void {
        if (!this.shouldLog(options)) {
            return;
        }

        // eslint-disable-next-line no-console -- This service is the controlled console logging boundary.
        console.error(message, error);
    }

    private shouldLog(options?: FrontendLoggerOptions): boolean {
        return options?.devOnly !== true || environment.buildVersion === 'dev';
    }
}

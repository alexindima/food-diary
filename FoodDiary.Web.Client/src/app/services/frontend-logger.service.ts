import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

interface FrontendLoggerOptions {
    devOnly?: boolean;
}

@Injectable({
    providedIn: 'root',
})
export class FrontendLoggerService {
    public warn(message: string, error?: unknown, options?: FrontendLoggerOptions): void {
        if (!this.shouldLog(options)) {
            return;
        }

        console.warn(message, error);
    }

    public error(message: string, error?: unknown, options?: FrontendLoggerOptions): void {
        if (!this.shouldLog(options)) {
            return;
        }

        console.error(message, error);
    }

    private shouldLog(options?: FrontendLoggerOptions): boolean {
        return !options?.devOnly || environment.buildVersion === 'dev';
    }
}

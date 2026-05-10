import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, type Observable, tap } from 'rxjs';

import { rethrowApiError } from '../shared/lib/api-error.utils';
import type { AppConfig } from '../types/app.data';

@Injectable({
    providedIn: 'root',
})
export class AppConfigService {
    private config: AppConfig | null = null;
    private readonly http = inject(HttpClient);

    public loadConfig(): Observable<AppConfig> {
        return this.http.get<AppConfig>('/assets/config/app-config.json').pipe(
            tap((config: AppConfig) => {
                this.config = config;
            }),
            catchError(error => rethrowApiError('Failed to load app config', error)),
        );
    }

    public getConfig(): AppConfig {
        if (this.config === null) {
            throw new Error('App config is not loaded');
        }
        return this.config;
    }
}

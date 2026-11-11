import { inject, Service } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';

import { environment } from '../../../environments/environment';
import { ADMIN_LOADING_URL_TTL_MS } from '../../config/runtime-ui.tokens';
import { AuthService } from '../../services/auth.service';
import { getRecordProperty, getStringProperty } from '../../shared/lib/unknown-value.utils';
import { BrowserWindowService } from '../../shared/platform/browser-window.service';

@Service()
export class AdminPanelLauncherService {
    private readonly authService = inject(AuthService);
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly toastService = inject(FdUiToastService);
    private readonly translateService = inject(TranslateService);
    private readonly loadingUrlTtlMs = inject(ADMIN_LOADING_URL_TTL_MS);
    private readonly router = inject(Router);

    public open(isAdmin: boolean): void {
        const adminAppUrl = environment.adminAppUrl;
        if (!isAdmin || adminAppUrl === undefined || adminAppUrl.length === 0 || !this.browserWindow.isAvailable()) {
            return;
        }

        const loadingUrl = URL.createObjectURL(new Blob([ADMIN_LOADING_DOCUMENT], { type: 'text/html' }));
        const adminWindow = this.browserWindow.open(loadingUrl, '_blank');
        this.browserWindow.setTimeout(() => {
            URL.revokeObjectURL(loadingUrl);
        }, this.loadingUrlTtlMs);

        this.authService.startAdminSso().subscribe({
            next: response => {
                const url = new URL('/', adminAppUrl);
                url.hash = new URLSearchParams({ code: response.code }).toString();
                adminWindow?.location.assign(url.toString());
            },
            error: (error: unknown) => {
                adminWindow?.close();
                if (getStringProperty(getRecordProperty(error, 'error'), 'error') === 'Authentication.ReauthenticationRequired') {
                    void this.router.navigate(['/auth/confirm-admin']);
                    return;
                }
                this.toastService.error(this.translateService.instant('USER_MANAGE.ADMIN_SSO_ERROR'));
            },
        });
    }
}

const ADMIN_LOADING_DOCUMENT = `
    <!doctype html>
    <html lang="en">
        <head>
            <meta charset="utf-8">
            <title>Opening admin panel...</title>
            <style>
                body {
                    margin: 0;
                    min-height: 100vh;
                    display: grid;
                    place-items: center;
                    background: #0f172a;
                    color: #e2e8f0;
                    font: 600 16px/1.5 Inter, Arial, sans-serif;
                }
            </style>
        </head>
        <body>Opening admin panel...</body>
    </html>
`;

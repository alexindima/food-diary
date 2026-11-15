import { DOCUMENT } from '@angular/common';
import { inject, Service } from '@angular/core';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';
import { BrowserWindowService } from '../../shared/platform/browser-window.service';

@Service()
export class AdminPanelLauncherService {
    private readonly authService = inject(AuthService);
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly document = inject(DOCUMENT);

    public open(isAdmin: boolean): void {
        const adminAppUrl = environment.adminAppUrl;
        if (!isAdmin || adminAppUrl === undefined || adminAppUrl.length === 0 || !this.browserWindow.isAvailable()) {
            return;
        }

        const url = new URL(adminAppUrl, this.document.location.origin);
        const userId = this.authService.getUserId();
        if (userId !== null) {
            url.hash = new URLSearchParams({ account: userId }).toString();
        }
        this.browserWindow.open(url.href, '_blank', 'noopener');
    }
}

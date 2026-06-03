import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID, Service } from '@angular/core';

@Service()
export class BrowserNotificationCapabilityService {
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly isBrowser = isPlatformBrowser(this.platformId);

    public getPermission(): NotificationPermission | 'unsupported' {
        const notification = this.document.defaultView?.Notification;
        if (!this.isBrowser || notification === undefined) {
            return 'unsupported';
        }

        return notification.permission;
    }

    public isPermissionDenied(): boolean {
        return this.getPermission() === 'denied';
    }

    public getUserAgent(): string {
        return this.isBrowser ? (this.document.defaultView?.navigator.userAgent ?? '') : '';
    }

    public toAppUrl(url: string): string {
        if (!/^https?:\/\//i.test(url)) {
            return url;
        }

        const view = this.document.defaultView;
        if (!this.isBrowser || view === null) {
            return url;
        }

        try {
            const parsed = new view.URL(url, view.location.origin);
            if (parsed.origin === view.location.origin) {
                return `${parsed.pathname}${parsed.search}${parsed.hash}`;
            }
        } catch {
            return url;
        }

        return url;
    }
}

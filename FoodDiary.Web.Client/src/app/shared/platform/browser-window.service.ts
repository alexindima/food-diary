import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID, Service } from '@angular/core';

type TelegramCapableWindow = Window & {
    Telegram?: {
        WebApp?: {
            initData?: string;
        };
    };
};

@Service()
export class BrowserWindowService {
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly isBrowser = isPlatformBrowser(this.platformId);

    public isAvailable(): boolean {
        return this.getWindow() !== null;
    }

    public getOrigin(): string | undefined {
        return this.getWindow()?.location.origin;
    }

    public getHref(): string | null {
        return this.getWindow()?.location.href ?? null;
    }

    public getSearch(): string | null {
        return this.getWindow()?.location.search ?? null;
    }

    public getPathname(): string | null {
        return this.getWindow()?.location.pathname ?? null;
    }

    public getReferrer(): string | null {
        return this.document.referrer.length > 0 ? this.document.referrer : null;
    }

    public getHostname(): string | null {
        return this.getWindow()?.location.hostname ?? null;
    }

    public replaceCurrentUrl(nextUrl: string): void {
        this.getWindow()?.history.replaceState({}, '', nextUrl);
    }

    public onPageHideOnce(callback: () => void): void {
        this.getWindow()?.addEventListener('pagehide', callback, { once: true });
    }

    public onVisibilityHiddenOnce(callback: () => void): void {
        this.document.addEventListener(
            'visibilitychange',
            () => {
                if (this.document.visibilityState === 'hidden') {
                    callback();
                }
            },
            { once: true },
        );
    }

    public getTelegramInitData(): string | null {
        const windowRef = this.getWindow();
        if (windowRef === null || !isTelegramCapableWindow(windowRef)) {
            return null;
        }

        const telegram = windowRef.Telegram;
        const initData = telegram?.WebApp?.initData;
        if (typeof initData !== 'string' || initData.length === 0) {
            return null;
        }

        const trimmed = initData.trim();
        return trimmed.length > 0 ? trimmed : null;
    }

    private getWindow(): Window | null {
        if (!this.isBrowser) {
            return null;
        }

        return this.document.defaultView;
    }
}

function isTelegramCapableWindow(windowRef: Window): windowRef is TelegramCapableWindow {
    return 'Telegram' in windowRef;
}

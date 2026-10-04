import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID, Service } from '@angular/core';

@Service()
export class BrowserClipboardService {
    private readonly document = inject(DOCUMENT);
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

    public async readTextAsync(): Promise<string> {
        const clipboard = this.getView()?.navigator.clipboard;
        if (clipboard === undefined || !this.isActive()) {
            throw new Error('Clipboard unavailable');
        }
        return clipboard.readText();
    }

    public async canReadWithoutPromptAsync(): Promise<boolean> {
        const permissions = this.getView()?.navigator.permissions;
        if (permissions === undefined || !this.isActive()) {
            return false;
        }
        try {
            // eslint-disable-next-line @typescript-eslint/no-unsafe-type-assertion -- DOM types omit Chromium's clipboard-read descriptor.
            const status = await permissions.query({ name: 'clipboard-read' as PermissionName });
            return status.state === 'granted';
        } catch {
            return false;
        }
    }

    public onReturn(callback: () => void): () => void {
        const view = this.getView();
        const listener = (): void => {
            if (this.isActive()) {
                callback();
            }
        };
        view?.addEventListener('focus', listener);
        this.document.addEventListener('visibilitychange', listener);
        return () => {
            view?.removeEventListener('focus', listener);
            this.document.removeEventListener('visibilitychange', listener);
        };
    }

    private isActive(): boolean {
        return this.document.visibilityState === 'visible' && this.document.hasFocus();
    }

    private getView(): Window | null {
        return this.isBrowser ? this.document.defaultView : null;
    }
}

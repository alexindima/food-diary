import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { computed, DestroyRef, inject, PLATFORM_ID, Service, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { fromEvent } from 'rxjs';

import { BrowserWindowService } from '../platform/browser-window.service';
import { isMobileShellWindow } from '../platform/mobile-shell-runtime';

type InstallPromptEvent = Event & {
    prompt: () => Promise<{ outcome: 'accepted' | 'dismissed'; platform: string }>;
};

export type PwaInstallResult = 'accepted' | 'dismissed' | 'failed' | 'unavailable';

@Service()
export class PwaInstallService {
    private readonly document = inject(DOCUMENT);
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly deferredPrompt = signal<InstallPromptEvent | null>(null);
    private readonly standalone = signal(false);
    private readonly mobileDevice = signal(false);
    private readonly installationAccepted = signal(false);
    private readonly supportsIosInstructions = signal(false);
    private initialized = false;

    public readonly isInstalling = signal(false);
    public readonly installError = signal(false);
    public readonly isStandalone = this.standalone.asReadonly();
    public readonly shouldRememberSession = computed(() => this.standalone() || this.installationAccepted());
    public readonly isMobileDevice = this.mobileDevice.asReadonly();
    public readonly installMode = computed(() => {
        if (this.standalone() || this.installationAccepted()) {
            return null;
        }
        return this.deferredPrompt() !== null ? 'prompt' : this.supportsIosInstructions() ? 'ios' : null;
    });
    public readonly showInstallCard = computed(
        () =>
            !this.standalone() &&
            !this.installationAccepted() &&
            (this.installMode() !== null || this.isInstalling() || this.installError()),
    );

    public initialize(): void {
        if (this.initialized || !this.isBrowser) {
            return;
        }
        const view = this.document.defaultView;
        if (view === null) {
            return;
        }
        if (!view.isSecureContext || isMobileShellWindow(view)) {
            return;
        }
        this.initialized = true;
        const browserNavigator = view.navigator;
        if (this.browserWindow.getTelegramInitData() !== null || browserNavigator.userAgent.includes('Telegram')) {
            return;
        }
        this.mobileDevice.set(browserNavigator.userAgent.includes('Android') || isIosBrowser(browserNavigator));

        // Fullscreen also matches an ordinary browser tab and does not establish installation.
        const displayMode = view.matchMedia(
            '(display-mode: standalone), (display-mode: minimal-ui), (display-mode: window-controls-overlay)',
        );
        const updateDisplayMode = (): void => {
            this.standalone.set(displayMode.matches || ('standalone' in browserNavigator && browserNavigator.standalone === true));
            this.installError.set(false);
        };
        updateDisplayMode();
        fromEvent(displayMode, 'change').pipe(takeUntilDestroyed(this.destroyRef)).subscribe(updateDisplayMode);

        this.supportsIosInstructions.set(supportsManualIosInstallation(browserNavigator));

        fromEvent<Event>(view, 'beforeinstallprompt')
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(event => {
                if (!isInstallPromptEvent(event)) {
                    return;
                }
                event.preventDefault();
                this.installError.set(false);
                this.installationAccepted.set(false);
                this.deferredPrompt.set(event);
            });
        fromEvent(view, 'appinstalled')
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.installationAccepted.set(true);
                this.deferredPrompt.set(null);
                this.installError.set(false);
            });
    }

    public async installAsync(): Promise<PwaInstallResult> {
        const prompt = this.deferredPrompt();
        if (prompt === null || this.isInstalling() || this.installMode() !== 'prompt') {
            return 'unavailable';
        }
        this.isInstalling.set(true);
        this.installError.set(false);
        this.deferredPrompt.set(null);
        try {
            // Keep this call in the user's click stack: browsers require user activation.
            const result = await prompt.prompt();
            this.installationAccepted.set(result.outcome === 'accepted');
            return result.outcome;
        } catch {
            this.installError.set(true);
            return 'failed';
        } finally {
            this.isInstalling.set(false);
        }
    }
}

function isInstallPromptEvent(event: Event): event is InstallPromptEvent {
    return 'prompt' in event && typeof event.prompt === 'function';
}

function supportsManualIosInstallation(browserNavigator: Navigator): boolean {
    return isIosBrowser(browserNavigator) && browserNavigator.userAgent.includes('Safari');
}

function isIosBrowser(browserNavigator: Navigator): boolean {
    return (
        /iPad|iPhone|iPod/.test(browserNavigator.userAgent) ||
        (browserNavigator.platform === 'MacIntel' && browserNavigator.maxTouchPoints > 1)
    );
}

import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { DestroyRef, inject, PLATFORM_ID, RendererFactory2, Service, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

import { environment } from '../../../../../../environments/environment';
import { GoogleIdentityService } from '../../../../../shared/auth/google-identity.service';
import { resolveTranslateLanguage } from '../../../../../shared/i18n/translate-language.utils';
import { isMobileShellWindow } from '../../../../../shared/platform/mobile-shell-runtime';

export type AuthGoogleMode = 'login' | 'register';

type SocialLoginGoogleResult = {
    provider: 'google';
    result?: {
        idToken?: string | null;
    } | null;
};

type SocialLoginPlugin = {
    initialize: (options: { google: { webClientId: string } }) => Promise<void>;
    login: (options: { provider: 'google' }) => Promise<SocialLoginGoogleResult>;
};

type CapacitorPlugins = {
    SocialLogin?: SocialLoginPlugin;
};

type CapacitorWindow = Window & {
    Capacitor?: {
        Plugins?: CapacitorPlugins;
    };
};

@Service()
export class AuthGoogleManager {
    private readonly googleIdentityService = inject(GoogleIdentityService);
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly renderer = inject(RendererFactory2).createRenderer(null, null);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly isBrowser = isPlatformBrowser(this.platformId);

    public readonly ready = signal<boolean>(false);
    private readonly nativePlugin = signal<SocialLoginPlugin | null>(null);
    private callback: ((credential: string) => void) | null = null;
    private nativeClickUnlisten: (() => void) | null = null;
    private nativeLoginInProgress = false;

    public constructor() {
        this.destroyRef.onDestroy(() => {
            this.callback = null;
            this.nativeClickUnlisten?.();
        });
    }

    public async initializeAsync(callback: (credential: string) => void): Promise<void> {
        this.callback = callback;
        const clientId = environment.googleClientId ?? '';
        if (clientId.length === 0) {
            return;
        }

        const socialLogin = await this.tryInitializeNativeAsync(clientId);
        if (this.destroyRef.destroyed) {
            return;
        }
        if (socialLogin !== null) {
            this.nativePlugin.set(socialLogin);
            this.ready.set(true);
            return;
        }
        await this.initializeBrowserAsync(clientId);
    }

    public renderButton(mode: AuthGoogleMode, loginButton: HTMLElement | undefined, registerButton: HTMLElement | undefined): void {
        if (!this.ready()) {
            return;
        }

        [loginButton, registerButton].forEach(element => {
            if (element !== undefined) {
                this.renderer.setProperty(element, 'innerHTML', '');
            }
        });

        const target = mode === 'login' ? loginButton : registerButton;
        if (target === undefined) {
            return;
        }

        const socialLogin = this.nativePlugin();
        if (socialLogin !== null) {
            this.renderNativeButton(mode, target, socialLogin);
            return;
        }

        this.googleIdentityService.renderButton(target, 'filled_blue', this.getGoogleLocale());
    }

    private async initializeBrowserAsync(clientId: string): Promise<void> {
        try {
            await this.googleIdentityService.initializeAsync({
                clientId,
                callback: credential => {
                    this.callback?.(credential);
                },
            });
            if (this.destroyRef.destroyed) {
                return;
            }
            this.ready.set(true);
            this.googleIdentityService.prompt();
        } catch {
            this.ready.set(false);
        }
    }

    private getGoogleLocale(): string {
        return resolveTranslateLanguage(this.translateService) === 'ru' ? 'ru' : 'en';
    }

    private async tryInitializeNativeAsync(clientId: string): Promise<SocialLoginPlugin | null> {
        const socialLogin = this.getSocialLoginPlugin();
        if (socialLogin === null) {
            return null;
        }

        try {
            await socialLogin.initialize({
                google: {
                    webClientId: clientId,
                },
            });
            return socialLogin;
        } catch {
            return null;
        }
    }

    private renderNativeButton(mode: AuthGoogleMode, target: HTMLElement, socialLogin: SocialLoginPlugin): void {
        this.nativeClickUnlisten?.();
        this.nativeClickUnlisten = null;

        const button = this.document.createElement('button');
        this.renderer.setAttribute(button, 'type', 'button');
        this.renderer.addClass(button, 'auth__native-google-button');
        this.renderer.setProperty(button, 'textContent', this.getNativeButtonText(mode));
        this.renderer.appendChild(target, button);
        this.nativeClickUnlisten = this.renderer.listen(button, 'click', () => {
            void this.loginNativeAsync(socialLogin);
        });
    }

    private getNativeButtonText(mode: AuthGoogleMode): string {
        const key = mode === 'login' ? 'AUTH.GOOGLE.LOGIN_BUTTON' : 'AUTH.GOOGLE.REGISTER_BUTTON';

        return this.translateService.instant(key);
    }

    private async loginNativeAsync(socialLogin: SocialLoginPlugin): Promise<void> {
        if (this.nativeLoginInProgress) {
            return;
        }

        this.nativeLoginInProgress = true;
        try {
            const response = await socialLogin.login({
                provider: 'google',
            });
            const idToken = response.result?.idToken ?? '';
            if (idToken.length > 0) {
                this.callback?.(idToken);
            }
        } catch {
            this.ready.set(true);
        } finally {
            this.nativeLoginInProgress = false;
        }
    }

    private getSocialLoginPlugin(): SocialLoginPlugin | null {
        if (!this.isBrowser) {
            return null;
        }

        const windowRef = this.document.defaultView as CapacitorWindow | null;
        if (windowRef === null || !isMobileShellWindow(windowRef)) {
            return null;
        }

        return windowRef.Capacitor?.Plugins?.SocialLogin ?? null;
    }
}

import { inject, Injectable, RendererFactory2, signal } from '@angular/core';

import { environment } from '../../../../../../environments/environment';
import { GoogleIdentityService } from '../../../lib/google-identity.service';

export type AuthGoogleMode = 'login' | 'register';

@Injectable({ providedIn: 'root' })
export class AuthGoogleManager {
    private readonly googleIdentityService = inject(GoogleIdentityService);
    private readonly renderer = inject(RendererFactory2).createRenderer(null, null);

    public readonly ready = signal<boolean>(false);

    public async initializeAsync(callback: (credential: string) => void): Promise<void> {
        const clientId = environment.googleClientId ?? '';
        if (clientId.length === 0) {
            return;
        }

        try {
            await this.googleIdentityService.initializeAsync({
                clientId,
                callback,
            });
            this.ready.set(true);
            this.googleIdentityService.prompt();
        } catch {
            this.ready.set(false);
        }
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
        if (target !== undefined) {
            this.googleIdentityService.renderButton(target, 'filled_blue');
        }
    }
}

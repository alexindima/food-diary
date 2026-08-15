import { ChangeDetectionStrategy, Component, effect, type ElementRef, inject, input, output, signal, viewChild } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { environment } from '../../../../../../environments/environment';
import { GoogleIdentityService } from '../../../../../shared/auth/google-identity.service';
import { resolveTranslateLanguage } from '../../../../../shared/i18n/translate-language.utils';
import type { PasswordActionState } from '../../user-manage/user-manage-lib/user-manage.types';

@Component({
    selector: 'fd-user-manage-security-card',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiIconComponent],
    templateUrl: './user-manage-security-card.html',
    styleUrl: '../../user-manage/user-manage.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageSecurityCardComponent {
    private readonly googleIdentityService = inject(GoogleIdentityService);
    private readonly translateService = inject(TranslateService);
    private readonly googleButton = viewChild<ElementRef<HTMLDivElement>>('googleButton');
    private initializationStarted = false;

    public readonly email = input.required<string>();
    public readonly hasGoogleIdentity = input.required<boolean>();
    public readonly isLinkingGoogle = input.required<boolean>();
    public readonly passwordActionState = input.required<PasswordActionState>();
    public readonly googleCredential = output<string>();
    public readonly passwordChange = output();
    protected readonly isGoogleReady = signal(false);
    protected readonly isGoogleUnavailable = signal(false);

    public constructor() {
        effect(() => {
            const target = this.googleButton()?.nativeElement;
            if (target === undefined || this.hasGoogleIdentity() || this.initializationStarted) {
                return;
            }

            this.initializationStarted = true;
            void this.initializeGoogleAsync(target);
        });
    }

    private async initializeGoogleAsync(target: HTMLElement): Promise<void> {
        const clientId = environment.googleClientId ?? '';
        if (clientId.length === 0) {
            this.isGoogleUnavailable.set(true);
            return;
        }

        try {
            await this.googleIdentityService.initializeAsync({
                clientId,
                callback: credential => {
                    this.googleCredential.emit(credential);
                },
            });
            this.googleIdentityService.renderButton(target, 'filled_blue', this.getGoogleLocale());
            this.isGoogleReady.set(true);
        } catch {
            this.isGoogleUnavailable.set(true);
        }
    }

    private getGoogleLocale(): string {
        return resolveTranslateLanguage(this.translateService) === 'ru' ? 'ru' : 'en';
    }
}

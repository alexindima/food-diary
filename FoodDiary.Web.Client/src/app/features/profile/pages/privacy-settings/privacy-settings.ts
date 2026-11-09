import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent } from 'fd-ui-kit';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom } from 'rxjs';

import { AiConsentDialogComponent } from '../../../../components/shared/ai-consent-dialog/ai-consent-dialog';
import { localizedPublicPath } from '../../../../config/public-site-paths';
import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { PrivacyConsentFacade } from '../../../../shared/privacy/privacy-consent.facade';
import { AccountDeletionOfferComponent } from '../../components/account-deletion-offer/account-deletion-offer';
import { PrivacyMarketingSettingsComponent } from '../../components/privacy-marketing-settings/privacy-marketing-settings';

@Component({
    selector: 'fd-privacy-settings',
    imports: [
        RouterLink,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiCardComponent,
        AccountDeletionOfferComponent,
        PrivacyMarketingSettingsComponent,
    ],
    templateUrl: './privacy-settings.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrivacySettingsComponent {
    protected readonly privacy = inject(PrivacyConsentFacade);
    private readonly dialogs = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly language = injectCurrentLanguage();
    protected readonly policyPath = computed(() => localizedPublicPath('/privacy-policy', this.language() === 'ru' ? 'ru' : 'en'));

    public constructor() {
        void this.privacy.loadAsync().catch(() => {
            this.privacy.failed.set(true);
        });
    }

    protected allowHealth(): void {
        void this.privacy.ensureHealthConsentAsync(true);
    }
    protected withdrawHealth(): void {
        void this.privacy.chooseAsync(1, false);
    }

    protected async allowAiAsync(): Promise<void> {
        const accepted = await firstValueFrom(
            this.dialogs
                .open<AiConsentDialogComponent, never, boolean>(AiConsentDialogComponent, { size: 'lg' })
                .afterClosed()
                .pipe(takeUntilDestroyed(this.destroyRef)),
            { defaultValue: false },
        );
        if (accepted === true && !this.destroyRef.destroyed) {
            await this.privacy.chooseAsync(2, true);
        }
    }

    protected withdrawAi(): void {
        void this.privacy.chooseAsync(2, false);
    }
}

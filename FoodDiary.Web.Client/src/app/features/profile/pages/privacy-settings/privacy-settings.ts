import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent } from 'fd-ui-kit';

import { localizedPublicPath } from '../../../../config/public-site-paths';
import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { MarketingConsentService } from '../../../../shared/marketing/marketing-consent.service';
import { PrivacyConsentFacade } from '../../../../shared/privacy/privacy-consent.facade';
import { AccountDeletionOfferComponent } from '../../components/account-deletion-offer/account-deletion-offer';

@Component({
    selector: 'fd-privacy-settings',
    imports: [RouterLink, TranslatePipe, FdUiButtonComponent, FdUiCardComponent, AccountDeletionOfferComponent],
    templateUrl: './privacy-settings.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrivacySettingsComponent {
    protected readonly privacy = inject(PrivacyConsentFacade);
    protected readonly marketing = inject(MarketingConsentService);
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
    protected withdrawAi(): void {
        void this.privacy.chooseAsync(2, false);
    }
    protected withdrawMarketing(): void {
        this.marketing.decide(false);
    }
}

import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent } from 'fd-ui-kit';

import { MARKETING_ATTRIBUTION_ENABLED } from '../../../../shared/marketing/marketing-attribution-enabled';
import { MarketingConsentService } from '../../../../shared/marketing/marketing-consent.service';
import { PrivacyConsentFacade } from '../../../../shared/privacy/privacy-consent.facade';

@Component({
    selector: 'fd-privacy-marketing-settings',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCardComponent],
    templateUrl: './privacy-marketing-settings.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrivacyMarketingSettingsComponent {
    protected readonly privacy = inject(PrivacyConsentFacade);
    protected readonly marketing = inject(MarketingConsentService);
    protected readonly marketingAttributionEnabled = inject(MARKETING_ATTRIBUTION_ENABLED);
    protected readonly marketingStateKey = computed(() => {
        const decision = this.marketing.currentDecision();
        return decision === null
            ? 'MARKETING_CONSENT.DEVICE_UNDECIDED'
            : decision.allowed
              ? 'MARKETING_CONSENT.DEVICE_ALLOWED'
              : 'MARKETING_CONSENT.DEVICE_DECLINED';
    });

    protected chooseMarketing(allowed: boolean): void {
        this.marketing.decide(allowed);
    }
}

import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent } from 'fd-ui-kit';

import { PrivacyConsentFacade } from '../../../../shared/privacy/privacy-consent.facade';
import { AccountDeletionFacade } from '../../lib/account-deletion.facade';

@Component({
    selector: 'fd-account-deletion-offer',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCardComponent],
    providers: [AccountDeletionFacade],
    templateUrl: './account-deletion-offer.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountDeletionOfferComponent {
    protected readonly privacy = inject(PrivacyConsentFacade);
    protected readonly deletion = inject(AccountDeletionFacade);
}

import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { localizedPublicPath } from '../../../../../config/public-site-paths';
import { injectCurrentLanguage } from '../../../../../shared/i18n/inject-current-language';

@Component({
    selector: 'fd-user-manage-privacy-card',
    imports: [TranslatePipe, FdUiButtonComponent],
    templateUrl: './user-manage-privacy-card.html',
    styleUrl: '../../user-manage/user-manage.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManagePrivacyCardComponent {
    private readonly language = injectCurrentLanguage();
    protected readonly privacyPolicyPath = computed(() => localizedPublicPath('/privacy-policy', this.language() === 'ru' ? 'ru' : 'en'));

    public readonly hasAiConsent = input.required<boolean>();
    public readonly isRevokingAiConsent = input.required<boolean>();
    public readonly isDeleting = input.required<boolean>();
    public readonly isAccountActionBusy = input.required<boolean>();

    public readonly aiConsentRevoke = output();
    public readonly accountDelete = output();
}

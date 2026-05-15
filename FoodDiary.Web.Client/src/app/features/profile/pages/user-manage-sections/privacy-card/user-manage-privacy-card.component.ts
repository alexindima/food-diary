import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

@Component({
    selector: 'fd-user-manage-privacy-card',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCardComponent],
    templateUrl: './user-manage-privacy-card.component.html',
    styleUrl: '../../user-manage/user-manage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManagePrivacyCardComponent {
    public readonly hasAiConsent = input.required<boolean>();
    public readonly isRevokingAiConsent = input.required<boolean>();
    public readonly isDeleting = input.required<boolean>();

    public readonly aiConsentRevoke = output();
    public readonly accountDelete = output();
}

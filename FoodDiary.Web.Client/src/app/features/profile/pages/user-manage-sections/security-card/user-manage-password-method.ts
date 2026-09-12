import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import type { PasswordActionState } from '../../user-manage/user-manage-lib/user-manage.types';

@Component({
    selector: 'fd-user-manage-password-method',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiIconComponent],
    templateUrl: './user-manage-password-method.html',
    styleUrl: '../../user-manage/user-manage.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManagePasswordMethodComponent {
    public readonly available = input.required<boolean>();
    public readonly actionState = input.required<PasswordActionState>();
    public readonly passwordChange = output();
}

import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';

import type { NotificationItem } from '../../../../shared/notifications/notification.service';
import type { NotificationViewModel } from '../notifications-dialog-lib/notifications-dialog.types';

@Component({
    selector: 'fd-notifications-dialog-item',
    imports: [TranslatePipe, FdUiIconComponent],
    templateUrl: './notifications-dialog-item.html',
    styleUrl: '../notifications-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsDialogItemComponent {
    public readonly item = input.required<NotificationViewModel>();

    public readonly notificationOpen = output<NotificationItem>();
}

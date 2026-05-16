import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import type { NotificationItem } from '../../../../services/notification.service';
import { NotificationsDialogItemComponent } from '../notifications-dialog-item/notifications-dialog-item.component';
import type { NotificationViewModel } from '../notifications-dialog-lib/notifications-dialog.types';

@Component({
    selector: 'fd-notifications-dialog-list',
    imports: [NotificationsDialogItemComponent],
    templateUrl: './notifications-dialog-list.component.html',
    styleUrl: '../notifications-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsDialogListComponent {
    public readonly items = input.required<NotificationViewModel[]>();

    public readonly notificationOpen = output<NotificationItem>();
}

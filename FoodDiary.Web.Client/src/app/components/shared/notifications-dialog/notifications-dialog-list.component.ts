import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import type { NotificationItem } from '../../../services/notification.service';
import type { NotificationViewModel } from './notifications-dialog.component';
import { NotificationsDialogItemComponent } from './notifications-dialog-item.component';

@Component({
    selector: 'fd-notifications-dialog-list',
    imports: [NotificationsDialogItemComponent],
    templateUrl: './notifications-dialog-list.component.html',
    styleUrl: './notifications-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsDialogListComponent {
    public readonly items = input.required<NotificationViewModel[]>();

    public readonly notificationOpen = output<NotificationItem>();
}

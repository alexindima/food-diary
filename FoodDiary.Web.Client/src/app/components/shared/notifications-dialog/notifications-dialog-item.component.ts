import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';

import type { NotificationItem } from '../../../services/notification.service';
import type { NotificationViewModel } from './notifications-dialog.component';

@Component({
    selector: 'fd-notifications-dialog-item',
    imports: [TranslatePipe, FdUiIconComponent],
    templateUrl: './notifications-dialog-item.component.html',
    styleUrl: './notifications-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsDialogItemComponent {
    public readonly item = input.required<NotificationViewModel>();

    public readonly notificationOpen = output<NotificationItem>();
}

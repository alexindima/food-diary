import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiSectionStateComponent } from 'fd-ui-kit/section-state/fd-ui-section-state';

import type { WebPushSubscriptionItem } from '../../../../../shared/notifications/notification.service';
import type { ConnectedDevicesSectionState, ConnectedDeviceViewModel } from '../../user-manage/user-manage-lib/user-manage.types';

@Component({
    selector: 'fd-user-manage-connected-devices',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiSectionStateComponent],
    templateUrl: './user-manage-connected-devices.html',
    styleUrl: '../../user-manage/user-manage.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageConnectedDevicesComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly items = input.required<ConnectedDeviceViewModel[]>();
    public readonly pushNotificationsBusy = input.required<boolean>();
    public readonly removingEndpoint = input.required<string | null>();

    protected readonly state = computed<ConnectedDevicesSectionState>(() => {
        if (this.isLoading()) {
            return 'loading';
        }

        return this.items().length === 0 ? 'empty' : 'content';
    });

    public readonly deviceRemove = output<WebPushSubscriptionItem>();
}

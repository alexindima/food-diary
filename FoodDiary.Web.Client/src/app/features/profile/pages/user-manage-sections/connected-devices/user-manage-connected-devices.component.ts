import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiSectionStateComponent } from 'fd-ui-kit/section-state/fd-ui-section-state.component';

import type { WebPushSubscriptionItem } from '../../../../../services/notification.service';
import type { ConnectedDevicesSectionState, ConnectedDeviceViewModel } from '../../user-manage/user-manage.types';

@Component({
    selector: 'fd-user-manage-connected-devices',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiSectionStateComponent],
    templateUrl: './user-manage-connected-devices.component.html',
    styleUrl: '../../user-manage/user-manage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageConnectedDevicesComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly items = input.required<ConnectedDeviceViewModel[]>();
    public readonly pushNotificationsBusy = input.required<boolean>();
    public readonly removingEndpoint = input.required<string | null>();

    public readonly state = computed<ConnectedDevicesSectionState>(() => {
        if (this.isLoading()) {
            return 'loading';
        }

        return this.items().length === 0 ? 'empty' : 'content';
    });

    public readonly deviceRemove = output<WebPushSubscriptionItem>();
}

import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiHintDirective } from 'fd-ui-kit/hint/fd-ui-hint.directive';
import { FdUiSwitchComponent } from 'fd-ui-kit/switch/fd-ui-switch';

import { DashboardNotificationSettingsFacade } from './dashboard-notification-settings.facade';

@Component({
    selector: 'fd-dashboard-notification-settings-dialog',
    templateUrl: './dashboard-notification-settings-dialog.html',
    styleUrl: './dashboard-notification-settings-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiDialogComponent, FdUiSwitchComponent, FdUiButtonComponent, FdUiDialogFooterDirective, FdUiHintDirective],
    providers: [DashboardNotificationSettingsFacade],
})
export class DashboardNotificationSettingsDialogComponent {
    private readonly settings = inject(DashboardNotificationSettingsFacade);

    protected readonly isLoading = this.settings.isLoading;
    protected readonly isUpdating = this.settings.isUpdating;
    protected readonly isOpeningProfile = this.settings.isOpeningProfile;
    protected readonly submitError = this.settings.submitError;
    protected readonly pushNotificationsEnabled = this.settings.pushNotificationsEnabled;
    protected readonly fastingPushNotificationsEnabled = this.settings.fastingPushNotificationsEnabled;
    protected readonly socialPushNotificationsEnabled = this.settings.socialPushNotificationsEnabled;
    protected readonly pushNotificationsSupported = this.settings.pushNotificationsSupported;
    protected readonly pushNotificationsSubscribed = this.settings.pushNotificationsSubscribed;
    protected readonly pushNotificationsBusy = this.settings.pushNotificationsBusy;
    protected readonly pushNotificationsAccountStatusKey = this.settings.pushNotificationsAccountStatusKey;
    protected readonly pushNotificationsDeviceStatusKey = this.settings.pushNotificationsDeviceStatusKey;
    protected readonly pushNotificationsHintKey = this.settings.pushNotificationsHintKey;

    public constructor() {
        this.settings.load();
    }

    protected togglePushNotifications(): void {
        this.settings.togglePushNotifications();
    }

    protected toggleFastingPushNotifications(): void {
        this.settings.toggleFastingPushNotifications();
    }

    protected toggleSocialPushNotifications(): void {
        this.settings.toggleSocialPushNotifications();
    }

    protected async openAdvancedSettingsAsync(): Promise<void> {
        await this.settings.openAdvancedSettingsAsync();
    }
}

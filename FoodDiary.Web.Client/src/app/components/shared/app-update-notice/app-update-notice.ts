import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiInlineAlertComponent } from 'fd-ui-kit';

import { AppUpdateService } from '../../../shared/service-worker/app-update.service';

@Component({
    selector: 'fd-app-update-notice',
    imports: [TranslatePipe, FdUiInlineAlertComponent],
    templateUrl: './app-update-notice.html',
    styleUrl: './app-update-notice.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppUpdateNoticeComponent {
    public readonly visible = input.required<boolean>();
    public readonly confirmReload = input.required<() => Promise<boolean>>();
    protected readonly updates = inject(AppUpdateService);
    protected readonly showNotice = computed(() => (this.visible() || this.updates.recoveryRequired()) && this.updates.updateAvailable());
    protected readonly titleKey = computed(() => (this.updates.recoveryRequired() ? 'PWA.RECOVERY.TITLE' : 'PWA.UPDATE.TITLE'));
    protected readonly buttonKey = computed(() => (this.updates.recoveryRequired() ? 'PWA.RECOVERY.BUTTON' : 'PWA.UPDATE.BUTTON'));
    protected readonly messageKey = computed(() => {
        if (this.updates.needsSave()) {
            return 'PWA.UPDATE.SAVE_FIRST';
        }
        return this.updates.recoveryRequired() ? 'PWA.RECOVERY.DESCRIPTION' : 'PWA.UPDATE.DESCRIPTION';
    });
}

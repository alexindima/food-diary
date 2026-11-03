import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiDialogRef } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';

import { localizedPublicPath } from '../../../config/public-site-paths';
import { injectCurrentLanguage } from '../../../shared/i18n/inject-current-language';

export type HealthConsentDialogData = { acceptLabelKey: string };

@Component({
    selector: 'fd-health-consent-dialog',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective],
    templateUrl: './health-consent-dialog.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HealthConsentDialogComponent {
    private readonly dialog = inject(FdUiDialogRef<HealthConsentDialogComponent, boolean>);
    protected readonly data = inject<HealthConsentDialogData>(FD_UI_DIALOG_DATA, { optional: true });
    protected readonly agreed = signal(false);
    private readonly language = injectCurrentLanguage();
    protected readonly policyPath = computed(() => localizedPublicPath('/privacy-policy', this.language() === 'ru' ? 'ru' : 'en'));

    protected change(event: Event): void {
        if (event.target instanceof HTMLInputElement) {
            this.agreed.set(event.target.checked);
        }
    }

    protected close(accepted: boolean): void {
        if (!accepted || this.agreed()) {
            this.dialog.close(accepted);
        }
    }
}

import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { catchError, firstValueFrom, of } from 'rxjs';

import { AuthService } from '../../../services/auth.service';
import { PREMIUM_TRIAL_ACTIONS } from './premium-trial-actions';

export type PremiumRequiredDialogData = {
    title?: string;
    message?: string;
    note?: string;
    actionLabel?: string;
    cancelLabel?: string;
};

@Component({
    selector: 'fd-premium-required-dialog',
    templateUrl: './premium-required-dialog.html',
    styleUrls: ['./premium-required-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent, TranslatePipe],
})
export class PremiumRequiredDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<PremiumRequiredDialogComponent, boolean>);
    private readonly trial = inject(PREMIUM_TRIAL_ACTIONS, { optional: true });
    private readonly auth = this.trial === null ? null : inject(AuthService);
    private readonly toast = inject(FdUiToastService);
    private readonly translate = inject(TranslateService);
    private readonly trialAvailable = toSignal(this.trial?.canStart().pipe(catchError(() => of(false))) ?? of(false), {
        initialValue: false,
    });
    private readonly trialActivated = signal(false);
    protected readonly data = inject<PremiumRequiredDialogData>(FD_UI_DIALOG_DATA, { optional: true }) ?? {};
    protected readonly showTrial = computed(() => this.trialAvailable() && !this.trialActivated());
    protected readonly trialLoading = signal(false);
    protected readonly trialError = signal<string | null>(null);

    protected async startTrialAsync(): Promise<void> {
        if (this.trial === null || this.auth === null || !this.showTrial() || this.trialLoading()) {
            return;
        }

        this.trialLoading.set(true);
        this.trialError.set(null);
        this.dialogRef.disableClose = true;
        try {
            await firstValueFrom(this.trial.start());
            this.trialActivated.set(true);
            if (await firstValueFrom(this.auth.refreshSession())) {
                this.toast.success(this.translate.instant('PREMIUM_PAGE.BANNERS.TRIAL_STARTED_MESSAGE'));
                this.dialogRef.close(false);
                return;
            }
            this.trialError.set('PREMIUM_PAGE.ERROR_ACCESS_REFRESH');
        } catch {
            this.trialError.set(this.trialActivated() ? 'PREMIUM_PAGE.ERROR_ACCESS_REFRESH' : 'PREMIUM_PAGE.ERROR_TRIAL');
        } finally {
            this.dialogRef.disableClose = false;
            this.trialLoading.set(false);
        }
    }

    protected onConfirm(): void {
        this.dialogRef.close(true);
    }

    protected onCancel(): void {
        this.dialogRef.close(false);
    }
}

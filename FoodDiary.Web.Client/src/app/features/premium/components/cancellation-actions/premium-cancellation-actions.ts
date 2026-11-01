import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../../../services/auth.service';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { formatDateValue } from '../../../../shared/lib/local-date.utils';
import { resolveAppLocale } from '../../../../shared/lib/locale.constants';
import type { BillingOverview } from '../../../../shared/models/billing.models';
import { PremiumCancelDialogComponent, type PremiumCancelDialogData } from '../../dialogs/cancel-subscription/premium-cancel-dialog';
import { PremiumBillingFacade } from '../../lib/premium-billing.facade';
import type { CancellationFeedback, CancellationId } from '../../models/cancellation.models';

@Component({
    selector: 'fd-premium-cancellation-actions',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiButtonComponent],
    templateUrl: './premium-cancellation-actions.html',
})
export class PremiumCancellationActionsComponent {
    public readonly overview = input.required<BillingOverview | null>();
    public readonly overviewChanged = output<BillingOverview>();
    protected readonly loading = signal(false);
    private readonly billing = inject(PremiumBillingFacade);
    private readonly dialog = inject(FdUiDialogService);
    private readonly toast = inject(FdUiToastService);
    private readonly translate = inject(TranslateService);
    private readonly auth = inject(AuthService);
    private readonly destroyRef = inject(DestroyRef);

    protected async cancelAsync(): Promise<void> {
        if (this.loading() || this.overview()?.cancellationAvailable !== true) {
            return;
        }
        this.loading.set(true);
        try {
            const feedback = await firstValueFrom(
                this.dialog
                    .open<PremiumCancelDialogComponent, PremiumCancelDialogData, CancellationFeedback>(PremiumCancelDialogComponent, {
                        preset: 'confirm',
                        data: {
                            periodEnd: formatDateValue(
                                this.overview()?.currentPeriodEndUtc,
                                resolveAppLocale(resolveTranslateLanguage(this.translate)),
                                { dateStyle: 'medium', timeStyle: 'short' },
                            ),
                            immediate:
                                this.overview()?.subscriptionStatus === 'past_due' || this.overview()?.subscriptionStatus === 'paused',
                        },
                    })
                    .afterClosed(),
            );
            if (feedback === undefined) {
                return;
            }
            const confirmation = await firstValueFrom(this.billing.cancelSubscription());
            this.overviewChanged.emit(confirmation.overview);
            this.toast.success(this.translate.instant('PREMIUM_PAGE.CANCELLATION.SUCCESS'));
            this.saveFeedback(confirmation.cancellationId, feedback);
            this.auth
                .refreshSession()
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe({ error: () => {} });
        } catch {
            this.toast.error(this.translate.instant('PREMIUM_PAGE.CANCELLATION.ERROR'));
        } finally {
            this.loading.set(false);
        }
    }

    private saveFeedback(cancellationId: CancellationId, feedback: CancellationFeedback): void {
        if (feedback.reasonCode === null && feedback.comment === null) {
            return;
        }
        this.billing
            .submitCancellationFeedback(cancellationId, feedback)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({ error: () => this.toast.info(this.translate.instant('PREMIUM_PAGE.CANCELLATION.FEEDBACK_ERROR')) });
    }
}

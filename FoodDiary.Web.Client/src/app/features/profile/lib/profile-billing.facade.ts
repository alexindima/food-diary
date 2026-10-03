import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { computed, DestroyRef, inject, PLATFORM_ID, signal } from '@angular/core';
import { Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import type { Subscription } from 'rxjs';
import { finalize } from 'rxjs';

import { RequestStateController } from '../../../shared/lib/request-state';
import type { BillingOverview } from '../../../shared/models/billing.models';
import { BILLING_ACCOUNT_ACTIONS } from '../../premium/contracts/billing-account-actions';
import type { BillingViewModel } from './user-manage.types';
import { buildBillingView } from './user-manage-billing.mapper';

@Injectable()
export class ProfileBillingFacade {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly toastService = inject(FdUiToastService);
    private readonly billingFacade = inject(BILLING_ACCOUNT_ACTIONS);
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly isBrowser = isPlatformBrowser(this.platformId);
    private readonly overviewRequest = new RequestStateController<BillingOverview>();
    private overviewRead: Subscription | undefined;
    public readonly billingOverview = this.overviewRequest.data;

    public readonly isLoadingBilling = this.overviewRequest.isLoading;

    public readonly isOpeningBillingPortal = signal(false);

    private readonly portalError = signal<string | null>(null);
    public readonly billingError = computed(() => this.overviewRequest.error() ?? this.portalError());

    public readonly billingView = computed<BillingViewModel | null>(() => buildBillingView(this.billingOverview()));

    public reloadBillingOverview(): void {
        this.loadBillingOverview();
    }

    public openBillingPortal(): void {
        if (!this.isBrowser || this.isOpeningBillingPortal()) {
            return;
        }

        this.portalError.set(null);
        this.isOpeningBillingPortal.set(true);
        this.billingFacade
            .createPortalSession()
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.isOpeningBillingPortal.set(false);
                }),
            )
            .subscribe({
                next: session => {
                    if (session.url.length === 0) {
                        this.portalError.set('USER_MANAGE.BILLING_PORTAL_ERROR');
                        this.toastService.error(this.translateService.instant('USER_MANAGE.BILLING_PORTAL_ERROR'));
                        return;
                    }

                    this.document.location.href = session.url;
                },
                error: () => {
                    this.portalError.set('USER_MANAGE.BILLING_PORTAL_ERROR');
                    this.toastService.error(this.translateService.instant('USER_MANAGE.BILLING_PORTAL_ERROR'));
                },
            });
    }

    public loadBillingOverview(): void {
        this.overviewRead?.unsubscribe();
        this.portalError.set(null);
        const requestId = this.overviewRequest.begin();
        this.overviewRead = this.billingFacade
            .getOverview()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: overview => this.overviewRequest.succeed(requestId, overview),
                error: () => this.overviewRequest.fail(requestId, 'USER_MANAGE.BILLING_LOAD_ERROR'),
            });
    }
}

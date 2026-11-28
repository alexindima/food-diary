import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, PLATFORM_ID, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom, type Observable, timer } from 'rxjs';

import { environment } from '../../../../../environments/environment';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { AuthService } from '../../../../services/auth.service';
import { TelegramWebAppService } from '../../../../shared/auth/telegram-web-app.service';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { resolveAppLocale } from '../../../../shared/lib/locale.constants';
import type { BillingOverview, BillingPlan, BillingProvider } from '../../../../shared/models/billing.models';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { scrollElementIntoView } from '../../../../shared/ui/scroll/scroll-element-into-view';
import { PaddleCheckoutService } from '../../lib/paddle-checkout.service';
import { PremiumBillingFacade } from '../../lib/premium-billing.facade';
import { PremiumPlanPricingFacade } from '../../lib/premium-plan-pricing.facade';
import { PremiumAccessBannersComponent } from '../premium-access-sections/access-banners/premium-access-banners';
import { PremiumBenefitsCardComponent } from '../premium-access-sections/benefits-card/premium-benefits-card';
import { PremiumOverviewCardComponent } from '../premium-access-sections/overview-card/premium-overview-card';
import { PremiumPlansCardComponent } from '../premium-access-sections/plans-card/premium-plans-card';
import { PremiumSupportCardComponent } from '../premium-access-sections/support-card/premium-support-card';
import type {
    PremiumCheckoutRequest,
    PremiumOverviewCardViewModel,
    PremiumPlanCardViewModel,
} from './premium-access-lib/premium-access.types';
import { formatPremiumMediumDate } from './premium-access-lib/premium-access-date.utils';
import { resolvePremiumErrorMessage } from './premium-access-lib/premium-access-error.utils';
import { buildPremiumOverviewCardViewModel, buildPremiumPlanCards } from './premium-access-lib/premium-access-view.mapper';
import { PREMIUM_ACCESS_TOUR } from './premium-access-tour';

@Component({
    selector: 'fd-premium-access-page',
    templateUrl: './premium-access-page.html',
    styleUrls: ['./premium-access-page.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [PremiumPlanPricingFacade],
    imports: [
        FdPageContainerDirective,
        PageBodyComponent,
        PageHeaderComponent,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        PremiumAccessBannersComponent,
        PremiumOverviewCardComponent,
        PremiumPlansCardComponent,
        PremiumBenefitsCardComponent,
        PremiumSupportCardComponent,
    ],
})
export class PremiumAccessPageComponent {
    private static readonly CheckoutActivationPollAttempts = 6;
    private static readonly CheckoutActivationPollDelayMs = 1500;

    private readonly billingService = inject(PremiumBillingFacade);
    private readonly telegram = inject(TelegramWebAppService);
    private readonly paddleCheckoutService = inject(PaddleCheckoutService);
    protected readonly pricing = inject(PremiumPlanPricingFacade);
    private readonly authService = inject(AuthService);
    private readonly toastService = inject(FdUiToastService);
    private readonly translateService = inject(TranslateService);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly destroyRef = inject(DestroyRef);
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);

    private readonly isBrowser = isPlatformBrowser(this.platformId);

    protected readonly overview = signal<BillingOverview | null>(null);
    protected readonly isLoading = signal(true);
    protected readonly checkoutLoadingPlan = signal<BillingPlan | null>(null);
    protected readonly portalLoading = signal(false);
    protected readonly trialLoading = signal(false);
    private readonly accessActivationBusy = computed(() => this.checkoutLoadingPlan() !== null || this.trialLoading());
    protected readonly errorMessage = signal<string | null>(null);
    protected readonly checkoutReturnState = signal<'success' | 'canceled' | null>(null);
    private readonly languageChange = toSignal(this.translateService.onLangChange);
    private overviewRevision = 0;
    private openingPaddleTransaction = false;
    private openedPaddleTransactionId: string | null = null;

    protected readonly isPremium = computed(() => this.overview()?.isPremium ?? this.authService.isPremium());
    protected readonly availableProviders = computed(() => {
        const overview = this.overview();
        return (
            overview?.availableProviders.filter(
                provider => provider.trim().length > 0 && (!this.telegram.isMiniAppLaunch() || provider.toLowerCase() === 'telegramstars'),
            ) ?? []
        );
    });
    protected readonly checkoutAvailable = computed(() => this.availableProviders().length > 0);
    private readonly hasOngoingSubscription = computed(() => {
        const overview = this.overview();
        return (
            overview?.manageBillingAvailable === true && overview.subscriptionStatus !== null && overview.subscriptionStatus !== 'canceled'
        );
    });
    protected readonly showPlans = computed(
        () =>
            !this.isLoading() &&
            this.overview() !== null &&
            !this.hasOngoingSubscription() &&
            (!this.isPremium() || this.overview()?.premiumTrialActive === true) &&
            this.checkoutAvailable(),
    );
    protected readonly currentPeriodEndLabel = computed(() => {
        this.languageChange();
        return this.formatMediumDate(this.overview()?.currentPeriodEndUtc);
    });
    protected readonly overviewViewModel = computed<PremiumOverviewCardViewModel>(() =>
        buildPremiumOverviewCardViewModel(this.overview(), this.isPremium(), this.checkoutAvailable(), {
            periodEnd: this.currentPeriodEndLabel(),
            periodStart: this.formatMediumDate(this.overview()?.currentPeriodStartUtc),
            nextBillingAttempt: this.formatMediumDate(this.overview()?.nextBillingAttemptUtc),
        }),
    );
    protected readonly planCards = computed<PremiumPlanCardViewModel[]>(() =>
        buildPremiumPlanCards(this.availableProviders(), this.checkoutLoadingPlan()),
    );

    public constructor() {
        void this.initializePageAsync();
    }

    protected async startCheckoutAsync(plan: BillingPlan, provider?: BillingProvider): Promise<void> {
        if (!this.isBrowser || this.accessActivationBusy()) {
            return;
        }
        const selectedProvider = provider ?? this.availableProviders().at(0);
        if (selectedProvider === undefined || !this.availableProviders().includes(selectedProvider)) {
            this.showErrorMessage(this.translateService.instant('PREMIUM_PAGE.ERROR_CHECKOUT'));
            return;
        }

        this.errorMessage.set(null);
        this.checkoutLoadingPlan.set(plan);

        try {
            const session = await this.readBillingResponseAsync(this.billingService.createCheckoutSession(plan, selectedProvider));
            await this.openCheckoutProviderAsync(session.url, selectedProvider);
        } catch (error) {
            this.showErrorMessage(this.getErrorMessage(error, 'PREMIUM_PAGE.ERROR_CHECKOUT'));
        } finally {
            this.checkoutLoadingPlan.set(null);
        }
    }

    private async openCheckoutProviderAsync(url: string, provider: BillingProvider): Promise<void> {
        if (!this.isPageActive()) {
            return;
        }
        if (url.length === 0) {
            this.showErrorMessage(this.translateService.instant('PREMIUM_PAGE.ERROR_CHECKOUT'));
            return;
        }

        if (provider.toLowerCase() === 'telegramstars' && this.telegram.isMiniAppLaunch()) {
            await this.handleStarsInvoiceAsync(url);
            return;
        }
        this.document.location.href = url;
    }

    private async handleStarsInvoiceAsync(url: string): Promise<void> {
        const state = await this.telegram.openInvoiceAsync(url);
        if (!this.isPageActive()) {
            return;
        }
        if (state === 'paid' || state === 'pending') {
            await this.refreshTokenWhenPremiumEntitlementVisibleAsync();
            if (!this.isPageActive()) {
                return;
            }
            const confirmed = this.overview()?.isPremium === true;
            this.checkoutReturnState.set(confirmed ? 'success' : null);
            if (!confirmed) {
                this.toastService.info(this.translateService.instant('PREMIUM_PAGE.STARS.PENDING'));
            }
        } else if (state === 'cancelled') {
            this.checkoutReturnState.set('canceled');
        } else {
            this.errorMessage.set(this.translateService.instant('PREMIUM_PAGE.ERROR_CHECKOUT'));
        }
    }

    protected async startCheckoutFromViewAsync(request: PremiumCheckoutRequest): Promise<void> {
        await this.startCheckoutAsync(request.plan, request.provider);
    }

    protected showRenewalPlans(plans: HTMLElement): void {
        plans.focus({ preventScroll: true });
        scrollElementIntoView(plans);
    }

    protected async startTrialAsync(): Promise<void> {
        if (this.accessActivationBusy()) {
            return;
        }

        this.errorMessage.set(null);
        this.trialLoading.set(true);

        try {
            const overview = await this.readBillingResponseAsync(this.billingService.startPremiumTrial());
            if (!this.isPageActive()) {
                return;
            }
            this.applyBillingChange(overview);
            this.loadPlanPrices(overview);
            if (!(await this.refreshAccountAccessAsync(this.overviewRevision)) || !this.isPageActive()) {
                return;
            }
            this.toastService.success(this.translateService.instant('PREMIUM_PAGE.BANNERS.TRIAL_STARTED_MESSAGE'));
        } catch (error) {
            this.showErrorMessage(this.getErrorMessage(error, 'PREMIUM_PAGE.ERROR_TRIAL'));
        } finally {
            this.trialLoading.set(false);
        }
    }

    protected async openPortalAsync(): Promise<void> {
        if (!this.isBrowser || this.portalLoading()) {
            return;
        }

        this.errorMessage.set(null);
        this.portalLoading.set(true);

        try {
            const session = await this.readBillingResponseAsync(this.billingService.createPortalSession());
            if (!this.isPageActive()) {
                return;
            }
            if (session.url.length === 0) {
                this.showErrorMessage(this.translateService.instant('PREMIUM_PAGE.ERROR_PORTAL'));
                return;
            }

            this.document.location.href = session.url;
        } catch (error) {
            this.showErrorMessage(this.getErrorMessage(error, 'PREMIUM_PAGE.ERROR_PORTAL'));
        } finally {
            this.portalLoading.set(false);
        }
    }

    protected async retryPricesAsync(): Promise<void> {
        const overview = this.overview();
        if (this.pricing.loading() || (overview !== null && this.loadPlanPrices(overview))) {
            return;
        }
        await this.loadOverviewAsync();
    }

    protected async retryAfterErrorAsync(): Promise<void> {
        if (this.isLoading() || !(await this.loadOverviewAsync())) {
            return;
        }
        await this.handlePaddleTransactionCheckoutAsync();
    }

    protected applyBillingChange(overview: BillingOverview): void {
        this.overviewRevision++;
        this.overview.set(overview);
        this.isLoading.set(false);
        this.errorMessage.set(null);
    }

    protected startPremiumTour(force = true): void {
        this.tourService.start(this.localizedTour.build(PREMIUM_ACCESS_TOUR), { force });
    }

    private formatMediumDate(value: string | null | undefined): string | null {
        return formatPremiumMediumDate(value, resolveAppLocale(this.translateService.getCurrentLang()));
    }

    private async initializePageAsync(): Promise<void> {
        await this.handleCheckoutReturnStateAsync();
        if (!this.isPageActive()) {
            return;
        }
        await this.loadOverviewAsync();
        await this.handlePaddleTransactionCheckoutAsync();
    }

    private async handleCheckoutReturnStateAsync(): Promise<void> {
        const checkoutState = this.route.snapshot.queryParamMap.get('checkout');
        if (checkoutState === null || checkoutState.length === 0) {
            return;
        }

        if (checkoutState === 'success') {
            this.checkoutReturnState.set('success');
            await this.refreshTokenWhenPremiumEntitlementVisibleAsync();
            if (!this.isPageActive()) {
                return;
            }
            this.toastService.success(this.translateService.instant('PREMIUM_PAGE.BANNERS.CHECKOUT_SUCCESS_MESSAGE'));
        } else if (checkoutState === 'canceled') {
            this.checkoutReturnState.set('canceled');
            this.toastService.info(this.translateService.instant('PREMIUM_PAGE.BANNERS.CHECKOUT_CANCELED_MESSAGE'));
        }

        if (!this.isBrowser) {
            return;
        }

        await this.router.navigate([], {
            relativeTo: this.route,
            queryParams: {},
            replaceUrl: true,
        });
    }

    private async loadOverviewAsync(): Promise<boolean> {
        const revision = ++this.overviewRevision;
        this.isLoading.set(true);
        this.errorMessage.set(null);

        let overview: BillingOverview;
        try {
            overview = await this.readBillingResponseAsync(this.billingService.getOverview());
            if (!this.isCurrentOverview(revision)) {
                return false;
            }
            this.overview.set(overview);
            if (this.isBrowser && overview.isPremium !== this.authService.isPremium()) {
                await this.refreshAccountAccessAsync(revision);
            }
        } catch (error) {
            if (this.isCurrentOverview(revision)) {
                this.errorMessage.set(this.getErrorMessage(error));
            }
            return false;
        } finally {
            if (revision === this.overviewRevision) {
                this.isLoading.set(false);
            }
        }

        const loaded = this.isCurrentOverview(revision) && this.errorMessage() === null;
        if (loaded && this.showPlans()) {
            this.loadPlanPrices(overview);
        }
        return loaded;
    }

    private loadPlanPrices(overview: BillingOverview): boolean {
        return this.pricing.loadPrices({ ...overview, availableProviders: this.availableProviders() }, this.resolveCheckoutLocale());
    }

    private isCurrentOverview(revision: number): boolean {
        return this.isPageActive() && revision === this.overviewRevision;
    }

    private async refreshAccountAccessAsync(revision: number): Promise<boolean> {
        const refreshedSession = await firstValueFrom(this.authService.refreshSession());
        if (!refreshedSession && this.isCurrentOverview(revision)) {
            this.errorMessage.set(this.translateService.instant('PREMIUM_PAGE.ERROR_ACCESS_REFRESH'));
        }
        return refreshedSession;
    }

    private async readBillingResponseAsync<T>(response: Observable<T>): Promise<T> {
        return firstValueFrom(response.pipe(takeUntilDestroyed(this.destroyRef)));
    }

    private isPageActive(): boolean {
        return !this.destroyRef.destroyed;
    }

    private async refreshTokenWhenPremiumEntitlementVisibleAsync(): Promise<void> {
        try {
            for (let attempt = 0; attempt < PremiumAccessPageComponent.CheckoutActivationPollAttempts && this.isPageActive(); attempt++) {
                const overview = await this.readBillingResponseAsync(this.billingService.getOverview());
                if (!this.isPageActive()) {
                    return;
                }
                if (overview.isPremium) {
                    this.applyBillingChange(overview);
                    await firstValueFrom(this.authService.refreshSession());
                    return;
                }
                this.overview.set(overview);

                await this.delayCheckoutActivationPollAsync();
            }
        } catch {
            // An active return screen attempts a final session refresh even if polling fails.
        }

        if (this.isPageActive()) {
            await firstValueFrom(this.authService.refreshSession());
        }
    }

    private async delayCheckoutActivationPollAsync(): Promise<void> {
        await firstValueFrom(timer(PremiumAccessPageComponent.CheckoutActivationPollDelayMs).pipe(takeUntilDestroyed(this.destroyRef)), {
            defaultValue: undefined,
        });
    }

    private async handlePaddleTransactionCheckoutAsync(): Promise<void> {
        if (!this.isBrowser || !this.isPageActive() || this.openingPaddleTransaction) {
            return;
        }

        const transactionId = this.route.snapshot.queryParamMap.get('_ptxn');
        if (transactionId === null || transactionId.length === 0) {
            return;
        }

        this.openingPaddleTransaction = true;
        try {
            if (!(await this.ensurePaddleTransactionOpenAsync(transactionId)) || !this.isPageActive()) {
                return;
            }
            this.openedPaddleTransactionId = transactionId;

            await this.router.navigate([], {
                relativeTo: this.route,
                queryParams: {},
                replaceUrl: true,
            });
        } catch (error) {
            if (this.isPageActive()) {
                this.showErrorMessage(this.getErrorMessage(error, 'PREMIUM_PAGE.ERROR_CHECKOUT'));
            }
        } finally {
            this.openingPaddleTransaction = false;
        }
    }

    private resolvePaddleClientToken(): string | null {
        const token = this.overview()?.paddleClientToken?.trim() ?? environment.paddleClientToken?.trim();
        return token !== undefined && token.length > 0 ? token : null;
    }

    private showPaddleClientTokenMissingError(): void {
        this.showErrorMessage(this.translateService.instant('PREMIUM_PAGE.ERROR_CHECKOUT'));
    }

    private showErrorMessage(message: string): void {
        if (!this.isPageActive()) {
            return;
        }
        this.errorMessage.set(message);
        this.toastService.error(message);
    }

    private async ensurePaddleTransactionOpenAsync(transactionId: string): Promise<boolean> {
        if (this.openedPaddleTransactionId === transactionId) {
            return true;
        }

        const paddleClientToken = this.resolvePaddleClientToken();
        if (paddleClientToken === null) {
            this.showPaddleClientTokenMissingError();
            return false;
        }

        await this.paddleCheckoutService.openTransactionCheckoutAsync(
            transactionId,
            {
                token: paddleClientToken,
                environment: paddleClientToken.startsWith('test_') ? 'sandbox' : 'production',
                locale: this.resolveCheckoutLocale(),
            },
            this.destroyRef,
        );
        return true;
    }

    private getErrorMessage(error: unknown, fallbackKey = 'PREMIUM_PAGE.ERROR_GENERIC'): string {
        return resolvePremiumErrorMessage(error, this.translateService.instant(fallbackKey));
    }

    private resolveCheckoutLocale(): string {
        return resolveTranslateLanguage(this.translateService);
    }
}

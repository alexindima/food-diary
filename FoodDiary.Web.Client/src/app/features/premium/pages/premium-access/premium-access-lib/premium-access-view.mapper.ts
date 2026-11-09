import type { BillingOverview, BillingPlan, BillingProvider } from '../../../../../shared/models/billing.models';
import type {
    PremiumOverviewBadgesViewModel,
    PremiumOverviewCardViewModel,
    PremiumOverviewCopyState,
    PremiumPlanCardViewModel,
    PremiumProviderOptionViewModel,
    PremiumSubscriptionDates,
    PremiumSubscriptionDetail,
} from './premium-access.types';

export function buildPremiumOverviewCardViewModel(
    overview: BillingOverview | null,
    isPremium: boolean,
    checkoutAvailable: boolean,
    dates: PremiumSubscriptionDates,
): PremiumOverviewCardViewModel {
    const showManageBilling = overview?.manageBillingAvailable ?? false;
    const showStartTrial = !showManageBilling && !isPremium && (overview?.canStartPremiumTrial ?? false);
    const accessState = buildPremiumAccessState(overview, isPremium);

    return {
        ...accessState,
        copyState: buildPremiumOverviewCopyState(overview, isPremium),
        badges: buildPremiumOverviewBadges(overview, isPremium),
        currentPeriodEndLabel: dates.periodEnd,
        hintKey: getOverviewHintKey(overview, accessState, { showManageBilling, showStartTrial, checkoutAvailable }),
        showManageBilling,
        showStartTrial,
        details: buildSubscriptionDetails(overview, dates, accessState),
    };
}

function buildPremiumAccessState(
    overview: BillingOverview | null,
    isPremium: boolean,
): Pick<
    PremiumOverviewCardViewModel,
    'titleKey' | 'accessActive' | 'isFreePlan' | 'subscriptionEnded' | 'paymentIssue' | 'statusNeedsAttention' | 'accessPeriodLabelKey'
> {
    const status = overview?.subscriptionStatus ?? null;
    const isFreePlan = !isPremium && status === null;
    const subscriptionEnded = !isPremium && (status === 'canceled' || status === 'active');
    const paymentIssue = status === 'past_due' || status === 'unpaid';
    return {
        titleKey: isFreePlan ? 'PREMIUM_PAGE.OVERVIEW.FREE_TITLE' : 'PREMIUM_PAGE.OVERVIEW.PREMIUM_TITLE',
        accessActive: isPremium,
        isFreePlan,
        subscriptionEnded,
        paymentIssue,
        statusNeedsAttention: paymentIssue || status === 'incomplete',
        accessPeriodLabelKey: getAccessPeriodLabelKey(overview, isPremium, subscriptionEnded),
    };
}

function getAccessPeriodLabelKey(overview: BillingOverview | null, isPremium: boolean, subscriptionEnded: boolean): string {
    if (overview?.premiumTrialActive === true) {
        return 'PREMIUM_PAGE.OVERVIEW.TRIAL_ENDS_ON';
    }
    if (isPremium) {
        return 'PREMIUM_PAGE.OVERVIEW.ACCESS_UNTIL';
    }
    return subscriptionEnded ? 'PREMIUM_PAGE.OVERVIEW.SUBSCRIPTION_ENDED' : 'PREMIUM_PAGE.OVERVIEW.PERIOD_END';
}

export function buildPremiumOverviewBadges(overview: BillingOverview | null, isPremium: boolean): PremiumOverviewBadgesViewModel {
    const plan = overview?.plan ?? null;
    const status = overview?.premiumTrialActive === true ? 'trialing' : (overview?.subscriptionStatus ?? null);
    return {
        planLabelKey: isPremium && plan !== null ? getPremiumPlanLabelKey(plan) : null,
        statusLabelKey: getPremiumAccessStatusLabelKey(status, isPremium),
    };
}

function getPremiumAccessStatusLabelKey(status: string | null, isPremium: boolean): string {
    if (status === 'active' && !isPremium) {
        return 'PREMIUM_PAGE.STATUS.EXPIRED';
    }
    if (status === 'canceled') {
        return isPremium ? 'PREMIUM_PAGE.STATUS.ACCESS_ACTIVE' : 'PREMIUM_PAGE.STATUS.EXPIRED';
    }
    return isPremium && status === null ? 'PREMIUM_PAGE.STATUS.ACCESS_ACTIVE' : getPremiumStatusLabelKey(status);
}

export function buildPremiumOverviewCopyState(overview: BillingOverview | null, isPremium: boolean): PremiumOverviewCopyState {
    const isTrialActive = overview?.premiumTrialActive === true;
    return {
        stateLabelKey: isTrialActive
            ? 'PREMIUM_PAGE.OVERVIEW.TRIAL_STATE'
            : isPremium
              ? 'PREMIUM_PAGE.OVERVIEW.PREMIUM_STATE'
              : 'PREMIUM_PAGE.OVERVIEW.FREE_STATE',
        periodLabelKey: getPeriodLabelKey(overview),
        showRenewalOffNotice: overview?.cancelAtPeriodEnd ?? false,
    };
}

function getAccessHintKey(overview: BillingOverview | null): string {
    if (overview?.subscriptionProvider?.toLowerCase() === 'telegramstars') {
        return 'PREMIUM_PAGE.STARS.ACCESS_HINT';
    }
    if ((overview?.subscriptionStatus ?? null) === null || overview?.premiumTrialActive === true) {
        return 'PREMIUM_PAGE.OVERVIEW.ACCESS_HINT';
    }
    return 'PREMIUM_PAGE.OVERVIEW.SUPPORT_MANAGED';
}

function getPeriodLabelKey(overview: BillingOverview | null): string {
    if (overview?.premiumTrialActive === true) {
        return 'PREMIUM_PAGE.OVERVIEW.TRIAL_ENDS_ON';
    }
    if (overview?.cancelAtPeriodEnd === true || overview?.renewalEnabled === false) {
        return 'PREMIUM_PAGE.OVERVIEW.ENDS_ON';
    }
    return 'PREMIUM_PAGE.OVERVIEW.RENEWS_ON';
}

function buildSubscriptionDetails(
    overview: BillingOverview | null,
    dates: PremiumSubscriptionDates,
    state: ReturnType<typeof buildPremiumAccessState>,
): PremiumSubscriptionDetail[] {
    if (overview === null || state.isFreePlan) {
        return [];
    }

    const details: PremiumSubscriptionDetail[] = [];
    if (overview.plan !== null) {
        details.push({
            labelKey: 'PREMIUM_PAGE.OVERVIEW.PLAN',
            value: getSubscriptionPlanLabelKey(overview.plan, overview.subscriptionProvider),
            translateValue: true,
        });
    }
    const provider = overview.subscriptionProvider?.trim() ?? '';
    if (provider.length > 0) {
        details.push({ labelKey: 'PREMIUM_PAGE.OVERVIEW.PROVIDER', value: getPremiumProviderLabel(provider), translateValue: false });
    }
    return details.concat(buildScheduleDetails(overview, dates, state));
}

function buildScheduleDetails(
    overview: BillingOverview,
    dates: PremiumSubscriptionDates,
    state: ReturnType<typeof buildPremiumAccessState>,
): PremiumSubscriptionDetail[] {
    const details: PremiumSubscriptionDetail[] = [];
    if (!state.paymentIssue && !(state.accessActive && overview.cancelAtPeriodEnd)) {
        details.push({ labelKey: 'PREMIUM_PAGE.OVERVIEW.RENEWAL', value: getRenewalLabelKey(overview), translateValue: true });
    }
    const period = state.accessActive ? null : buildPeriodDetail(dates, state.subscriptionEnded);
    if (period !== null) {
        details.push(period);
    }
    if (dates.nextBillingAttempt !== null && shouldShowNextBillingAttempt(overview)) {
        details.push({ labelKey: 'PREMIUM_PAGE.OVERVIEW.NEXT_CHARGE', value: dates.nextBillingAttempt, translateValue: false });
    }
    return details;
}

function getSubscriptionPlanLabelKey(plan: BillingPlan, provider: string | null): string {
    return provider?.toLowerCase() === 'telegramstars' ? 'PREMIUM_PAGE.STARS.PERIOD' : getPremiumPlanLabelKey(plan);
}

function buildPeriodDetail(dates: PremiumSubscriptionDates, subscriptionEnded: boolean): PremiumSubscriptionDetail | null {
    if (dates.periodStart === null) {
        return null;
    }
    const periodLabelKey = subscriptionEnded ? 'PREMIUM_PAGE.OVERVIEW.PREVIOUS_PERIOD' : 'PREMIUM_PAGE.OVERVIEW.PERIOD';
    return {
        labelKey: dates.periodEnd === null ? 'PREMIUM_PAGE.OVERVIEW.PERIOD_START' : periodLabelKey,
        value: dates.periodEnd === null ? dates.periodStart : `${dates.periodStart} — ${dates.periodEnd}`,
        translateValue: false,
    };
}

function shouldShowNextBillingAttempt(overview: BillingOverview): boolean {
    return overview.renewalEnabled && !overview.cancelAtPeriodEnd && !overview.premiumTrialActive;
}

function getRenewalLabelKey(overview: BillingOverview): string {
    if (overview.cancelAtPeriodEnd) {
        return 'PREMIUM_PAGE.OVERVIEW.RENEWAL_OFF';
    }
    if (overview.premiumTrialActive) {
        return 'PREMIUM_PAGE.OVERVIEW.RENEWAL_TRIAL';
    }
    return overview.renewalEnabled ? 'PREMIUM_PAGE.OVERVIEW.RENEWAL_ON' : 'PREMIUM_PAGE.OVERVIEW.RENEWAL_NONE';
}

export function buildPremiumPlanCards(providers: BillingProvider[], loadingPlan: BillingPlan | null): PremiumPlanCardViewModel[] {
    const providerOptions = providers.map(buildPremiumProviderOption);
    const onlyStars = providers.length === 1 && providers[0].toLowerCase() === 'telegramstars';

    const cards: PremiumPlanCardViewModel[] = [
        {
            plan: 'monthly',
            titleKey: onlyStars ? 'PREMIUM_PAGE.STARS.TITLE' : 'PREMIUM_PAGE.PLANS.MONTHLY.TITLE',
            descriptionKey: 'PREMIUM_PAGE.PLANS.MONTHLY.DESCRIPTION',
            actionKey: onlyStars ? 'PREMIUM_PAGE.STARS.SUBSCRIBE' : 'PREMIUM_PAGE.PLANS.MONTHLY.ACTION',
            isFeatured: false,
            kickerKey: null,
            isLoading: loadingPlan === 'monthly',
            providerOptions,
        },
        {
            plan: 'yearly',
            titleKey: 'PREMIUM_PAGE.PLANS.YEARLY.TITLE',
            descriptionKey: 'PREMIUM_PAGE.PLANS.YEARLY.DESCRIPTION',
            actionKey: 'PREMIUM_PAGE.PLANS.YEARLY.ACTION',
            isFeatured: true,
            kickerKey: 'PREMIUM_PAGE.PLANS.YEARLY.KICKER',
            isLoading: loadingPlan === 'yearly',
            providerOptions: providerOptions.filter(option => option.provider.toLowerCase() !== 'telegramstars'),
        },
    ];
    return cards.filter(card => card.providerOptions.length > 0);
}

export function buildPremiumProviderOption(provider: BillingProvider): PremiumProviderOptionViewModel {
    return {
        provider,
        label: getPremiumProviderLabel(provider),
        actionKey: 'PREMIUM_PAGE.PLANS.PAY_WITH',
    };
}

export function getPremiumPlanLabelKey(plan: BillingPlan): string {
    return plan === 'yearly' ? 'PREMIUM_PAGE.PLANS.YEARLY.TITLE' : 'PREMIUM_PAGE.PLANS.MONTHLY.TITLE';
}

export function getPremiumStatusLabelKey(status: string | null): string {
    switch (status) {
        case 'active': {
            return 'PREMIUM_PAGE.STATUS.ACTIVE';
        }
        case 'trialing': {
            return 'PREMIUM_PAGE.STATUS.TRIALING';
        }
        case 'past_due':
        case 'unpaid': {
            return 'PREMIUM_PAGE.STATUS.PAYMENT_ISSUE';
        }
        case 'canceled': {
            return 'PREMIUM_PAGE.STATUS.CANCELED';
        }
        case 'incomplete': {
            return 'PREMIUM_PAGE.STATUS.INCOMPLETE';
        }
        case null: {
            return 'PREMIUM_PAGE.STATUS.NONE';
        }
        default: {
            return 'PREMIUM_PAGE.STATUS.NONE';
        }
    }
}

export function getPremiumProviderLabel(provider: BillingProvider): string {
    switch (provider.toLowerCase()) {
        case 'telegramstars': {
            return 'Telegram Stars';
        }
        case 'yookassa': {
            return 'YooKassa';
        }
        case 'paddle': {
            return 'Paddle';
        }
        case 'stripe': {
            return 'Stripe';
        }
        default: {
            return provider;
        }
    }
}

function getOverviewHintKey(
    overview: BillingOverview | null,
    state: ReturnType<typeof buildPremiumAccessState>,
    {
        showManageBilling,
        showStartTrial,
        checkoutAvailable,
    }: { showManageBilling: boolean; showStartTrial: boolean; checkoutAvailable: boolean },
): string {
    if (state.paymentIssue) {
        return state.accessActive
            ? 'PREMIUM_PAGE.OVERVIEW.PAYMENT_ISSUE_WITH_ACCESS'
            : 'PREMIUM_PAGE.OVERVIEW.PAYMENT_ISSUE_WITHOUT_ACCESS';
    }
    if (checkoutAvailable && state.subscriptionEnded) {
        return 'PREMIUM_PAGE.OVERVIEW.RESUME_HINT';
    }
    if (!showManageBilling && state.accessActive) {
        return getAccessHintKey(overview);
    }
    if (showManageBilling) {
        return 'PREMIUM_PAGE.OVERVIEW.MANAGE_HINT';
    }

    if (showStartTrial) {
        return 'PREMIUM_PAGE.OVERVIEW.TRIAL_HINT';
    }

    return checkoutAvailable ? 'PREMIUM_PAGE.OVERVIEW.CHECKOUT_HINT' : 'PREMIUM_PAGE.OVERVIEW.CHECKOUT_UNAVAILABLE_HINT';
}

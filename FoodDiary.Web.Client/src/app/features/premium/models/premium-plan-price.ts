import type { BillingPlan } from '../../../shared/models/billing.models';

export type PremiumPlanPrice = {
    formattedTotal: string;
    currencyCode: string;
};

export type PremiumPlanPrices = Partial<Record<BillingPlan, PremiumPlanPrice>>;
export type PaddlePlanPriceIds = Record<BillingPlan, string>;

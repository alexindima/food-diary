import { getNumberProperty, getRecordProperty, getStringProperty } from '../../../shared/lib/unknown-value.utils';
import type { BillingPlan } from '../../../shared/models/billing.models';
import type { PaddlePlanPriceIds, PremiumPlanPrice } from '../models/premium-plan-price';

export function mapPaddlePlanPrices(response: unknown, priceIds: PaddlePlanPriceIds): Record<BillingPlan, PremiumPlanPrice> {
    const data = getRecordProperty(response, 'data');
    const currencyCode = getStringProperty(data, 'currencyCode');
    const items = getRecordProperty(getRecordProperty(data, 'details'), 'lineItems');
    if (currencyCode === undefined || !/^[A-Z]{3}$/.test(currencyCode) || !Array.isArray(items)) {
        throw new Error('Paddle price preview is invalid');
    }

    return {
        monthly: readPlanPrice(items, priceIds.monthly, 'month', currencyCode),
        yearly: readPlanPrice(items, priceIds.yearly, 'year', currencyCode),
    };
}

function readPlanPrice(items: unknown[], priceId: string, interval: string, currencyCode: string): PremiumPlanPrice {
    const item = items.find(value => getStringProperty(getRecordProperty(value, 'price'), 'id') === priceId);
    const cycle = getRecordProperty(getRecordProperty(item, 'price'), 'billingCycle');
    const formattedTotal = getStringProperty(getRecordProperty(item, 'formattedTotals'), 'total')?.trim();
    if (
        formattedTotal === undefined ||
        formattedTotal.length === 0 ||
        getStringProperty(cycle, 'interval') !== interval ||
        getNumberProperty(cycle, 'frequency') !== 1
    ) {
        throw new Error('Paddle plan price is unavailable');
    }

    return { formattedTotal, currencyCode };
}

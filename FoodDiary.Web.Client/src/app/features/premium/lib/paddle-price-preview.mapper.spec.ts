import { describe, expect, it } from 'vitest';

import { mapPaddlePlanPrices } from './paddle-price-preview.mapper';

const PRICE_IDS = { monthly: 'pri_month', yearly: 'pri_year' };

describe('mapPaddlePlanPrices', () => {
    it('matches reordered catalog prices and uses preview currency and tax-inclusive totals', () => {
        const response = createPreview();

        expect(mapPaddlePlanPrices(response, PRICE_IDS)).toEqual({
            monthly: { formattedTotal: '€9.49', currencyCode: 'EUR' },
            yearly: { formattedTotal: '€79.90', currencyCode: 'EUR' },
        });
    });

    it.each(['currency', 'missing-price', 'interval', 'frequency', 'empty-total'])(
        'rejects an incomplete or mismatched %s preview',
        problem => {
            const response = createPreview();
            const monthly = response.data.details.lineItems[1];
            if (problem === 'currency') {
                response.data.currencyCode = '';
            }
            if (problem === 'missing-price') {
                monthly.price.id = 'pri_other';
            }
            if (problem === 'interval') {
                monthly.price.billingCycle.interval = 'year';
            }
            if (problem === 'frequency') {
                monthly.price.billingCycle.frequency = 3;
            }
            if (problem === 'empty-total') {
                monthly.formattedTotals.total = ' ';
            }

            expect(() => mapPaddlePlanPrices(response, PRICE_IDS)).toThrow();
        },
    );
});

function createPreview(): {
    data: {
        currencyCode: string;
        details: {
            lineItems: Array<{
                price: { id: string; unitPrice: { currencyCode: string }; billingCycle: { interval: string; frequency: number } };
                formattedTotals: { total: string; subtotal: string };
            }>;
        };
    };
} {
    return {
        data: {
            currencyCode: 'EUR',
            details: {
                lineItems: [
                    {
                        price: { id: 'pri_year', unitPrice: { currencyCode: 'USD' }, billingCycle: { interval: 'year', frequency: 1 } },
                        formattedTotals: { total: '€79.90', subtotal: '€66.03' },
                    },
                    {
                        price: { id: 'pri_month', unitPrice: { currencyCode: 'USD' }, billingCycle: { interval: 'month', frequency: 1 } },
                        formattedTotals: { total: ' €9.49 ', subtotal: '€7.84' },
                    },
                ],
            },
        },
    };
}

import { buildProductTypeTranslationKey } from '../../../../shared/lib/product-type.utils';
import type { ActiveFilterChip } from '../../../../shared/ui/filters/active-filter-chip';
import type { ProductListQuery } from './product-list-query';

export function productFilterChips(model: ProductListQuery, translate: (key: string) => string, locale: string): ActiveFilterChip[] {
    const chips: ActiveFilterChip[] = [];
    const number = new Intl.NumberFormat(locale);
    if (model.onlyMine) {
        chips.push({ id: 'onlyMine', label: translate('PRODUCT_LIST.FILTER_MY_PRODUCTS') });
    }
    for (const type of model.productTypes) {
        chips.push({ id: `type:${type}`, label: translate(buildProductTypeTranslationKey(type)) });
    }
    for (const [id, key] of [
        ['caloriesFrom', 'PRODUCT_LIST.FILTER_CALORIES_FROM'],
        ['caloriesTo', 'PRODUCT_LIST.FILTER_CALORIES_TO'],
    ] as const) {
        const value = model[id];
        if (value !== null) {
            chips.push({ id, label: `${translate(key)}: ${number.format(value)}` });
        }
    }
    if (model.hasImage !== null) {
        chips.push({
            id: 'hasImage',
            label: translate(model.hasImage ? 'PRODUCT_LIST.FILTER_IMAGE_WITH' : 'PRODUCT_LIST.FILTER_IMAGE_WITHOUT'),
        });
    }
    return chips;
}

export function removeProductFilter(model: ProductListQuery, id: string): ProductListQuery {
    if (id === 'onlyMine') {
        return { ...model, page: 1, onlyMine: false };
    }
    if (id.startsWith('type:')) {
        return { ...model, page: 1, productTypes: model.productTypes.filter(type => `type:${type}` !== id) };
    }
    if (['caloriesFrom', 'caloriesTo', 'hasImage'].includes(id)) {
        return { ...model, page: 1, [id]: null };
    }
    return model;
}

export function resetProductFilters(model: ProductListQuery): ProductListQuery {
    return { ...model, page: 1, onlyMine: false, productTypes: [], caloriesFrom: null, caloriesTo: null, hasImage: null };
}

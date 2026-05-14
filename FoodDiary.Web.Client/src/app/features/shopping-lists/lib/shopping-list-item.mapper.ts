import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';

import { MeasurementUnit } from '../../products/models/product.data';
import type { ShoppingListItem, ShoppingListItemDto } from '../models/shopping-list.data';
import type { ShoppingListItemViewModel } from './shopping-list-form.types';

export type ShoppingListTranslateFn = (key: string) => string;

export function buildShoppingListUnitOptions(translate: ShoppingListTranslateFn): Array<FdUiSelectOption<MeasurementUnit>> {
    return (Object.values(MeasurementUnit) as MeasurementUnit[]).map(unit => ({
        value: unit,
        label: translate(`GENERAL.UNITS.${unit}`),
    }));
}

export function buildShoppingListItemViewModels(
    items: readonly ShoppingListItem[],
    translate: ShoppingListTranslateFn,
): ShoppingListItemViewModel[] {
    return items.map(item => ({
        ...item,
        meta: formatShoppingListItemMeta(item, translate),
    }));
}

export function formatShoppingListItemMeta(item: ShoppingListItem, translate: ShoppingListTranslateFn): string {
    const parts: string[] = [];

    if (item.amount !== null && item.amount !== undefined && !Number.isNaN(item.amount)) {
        const unitLabel = getUnitLabel(item.unit, translate);
        parts.push(unitLabel !== null ? `${item.amount} ${unitLabel}` : `${item.amount}`);
    }

    if (item.category !== null && item.category !== undefined && item.category.length > 0) {
        parts.push(item.category);
    }

    return parts.join(' - ');
}

export function rebuildShoppingListSortOrder(items: readonly ShoppingListItem[]): ShoppingListItem[] {
    return items.map((item, index) => ({
        ...item,
        sortOrder: index + 1,
    }));
}

export function normalizeShoppingListAmount(value: number | null): number | null {
    if (value === null) {
        return null;
    }

    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
}

export function mapShoppingListItemToDto(item: ShoppingListItem, index: number): ShoppingListItemDto {
    return {
        productId: item.productId ?? null,
        name: item.name,
        amount: item.amount ?? null,
        unit: item.unit ?? null,
        category: item.category ?? null,
        isChecked: item.isChecked,
        sortOrder: index + 1,
    };
}

function getUnitLabel(unit: MeasurementUnit | string | null | undefined, translate: ShoppingListTranslateFn): string | null {
    if (unit === null || unit === undefined || unit.length === 0) {
        return null;
    }

    const key = `GENERAL.UNITS.${unit}`;
    const translated = translate(key);
    return translated === key ? unit : translated;
}

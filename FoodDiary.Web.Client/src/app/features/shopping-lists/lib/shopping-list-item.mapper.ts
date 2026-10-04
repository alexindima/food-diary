import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';

import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import { MeasurementUnit } from '../../../shared/models/product.data';
import type { ShoppingListItem, ShoppingListItemDto } from '../../../shared/models/shopping-list.data';
import type { ShoppingListItemViewModel } from './shopping-list-form.types';
import { isTextQuantity } from './shopping-list-merge';

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
    language: string | null | undefined = 'en',
): ShoppingListItemViewModel[] {
    return items.map(item => ({
        ...item,
        meta: formatShoppingListItemMeta(item, translate, language),
        quantity:
            (item.amount === null || item.amount === undefined) && isTextQuantity(item.note)
                ? (item.note?.trim() ?? '')
                : formatShoppingListItemMeta({ ...item, note: null, category: null, sources: [] }, translate, language),
        detail: formatShoppingListItemMeta(
            {
                ...item,
                amount: null,
                note: (item.amount === null || item.amount === undefined) && isTextQuantity(item.note) ? null : item.note,
            },
            translate,
            language,
        ),
    }));
}

export function formatShoppingListItemMeta(
    item: ShoppingListItem,
    translate: ShoppingListTranslateFn,
    language: string | null | undefined = 'en',
): string {
    const parts: string[] = [];

    if (item.amount !== null && item.amount !== undefined && !Number.isNaN(item.amount)) {
        const unitLabel = getUnitLabel(item.unit, translate);
        const amount = new Intl.NumberFormat(resolveAppLocale(language), { maximumFractionDigits: 20 }).format(item.amount);
        parts.push(unitLabel !== null ? `${amount} ${unitLabel}` : amount);
    }

    appendTextPart(parts, item.note);
    appendTextPart(parts, item.category);
    appendTextPart(parts, item.sources?.[0]?.label);

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

export function normalizeShoppingListUnit(unit: MeasurementUnit | string | null | undefined): MeasurementUnit | string | null {
    if (unit === null || unit === undefined) {
        return null;
    }

    return Object.values(MeasurementUnit).find(value => value.toString() === unit.trim().toUpperCase()) ?? unit;
}

export function mapShoppingListItemToDto(item: ShoppingListItem, index: number): ShoppingListItemDto {
    return {
        id: isTemporaryId(item.id) ? null : item.id,
        productId: item.productId ?? null,
        name: item.name,
        amount: item.amount ?? null,
        unit: item.unit ?? null,
        category: item.category ?? null,
        aisle: item.aisle ?? item.category ?? null,
        note: item.note ?? null,
        isChecked: item.isChecked,
        checkedOnUtc: item.checkedOnUtc ?? null,
        sortOrder: index + 1,
    };
}

function getUnitLabel(unit: MeasurementUnit | string | null | undefined, translate: ShoppingListTranslateFn): string | null {
    if (unit === null || unit === undefined || unit.length === 0) {
        return null;
    }

    const normalizedUnit = normalizeShoppingListUnit(unit);
    const key = `GENERAL.UNITS.${normalizedUnit ?? unit}`;
    const translated = translate(key);
    return translated === key ? unit : translated;
}

function appendTextPart(parts: string[], value: string | null | undefined): void {
    if (value !== null && value !== undefined && value.length > 0) {
        parts.push(value);
    }
}

function isTemporaryId(id: string): boolean {
    return id.startsWith('temp-');
}

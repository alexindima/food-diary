import type { ShoppingListItem } from '../../../shared/models/shopping-list.data';
import { isTextQuantity } from './shopping-list-merge';

const MAX_AMOUNT = 1_000_000;

export type ShoppingMergeGroup = { before: ShoppingListItem[]; after: ShoppingListItem };
export type ShoppingMergePlan = { items: ShoppingListItem[]; groups: ShoppingMergeGroup[] };

/** Explicit consolidation never drops source relationships or combines purchased rows. */
export function planShoppingConsolidation(items: readonly ShoppingListItem[]): ShoppingMergePlan {
    const groups: ShoppingMergeGroup[] = [];
    const buckets = new Map<string, ShoppingMergeGroup>();
    const result: ShoppingListItem[] = [];
    for (const item of items) {
        const key = consolidationKey(item);
        const bucket = key === null ? undefined : buckets.get(key);
        if (bucket === undefined || !canCombine(bucket.after, item)) {
            const after = { ...item };
            result.push(after);
            if (key !== null) {
                buckets.set(key, { before: [item], after });
            }
        } else {
            if (bucket.before.length === 1) {
                groups.push(bucket);
            }
            bucket.before.push(item);
            if (typeof item.amount === 'number') {
                bucket.after.amount = (bucket.after.amount ?? 0) + item.amount;
            }
        }
    }
    return { items: result.map((item, index) => ({ ...item, sortOrder: index + 1 })), groups };
}

function canCombine(first: ShoppingListItem, second: ShoppingListItem): boolean {
    const amount = (first.amount ?? 0) + (second.amount ?? 0);
    return Number.isFinite(amount) && amount <= MAX_AMOUNT;
}

function eligible(item: ShoppingListItem): boolean {
    if (item.isChecked || item.id.startsWith('temp-') || (item.sources?.length ?? 0) > 0) {
        return false;
    }
    return typeof item.amount === 'number' ? Number.isFinite(item.amount) && item.amount > 0 : isTextQuantity(item.note);
}

function clean(value: string | null | undefined): string {
    return value?.trim() ?? '';
}

function consolidationKey(item: ShoppingListItem): string | null {
    const name = item.name.trim().toLowerCase();
    if (!eligible(item) || name.length === 0) {
        return null;
    }
    const product = clean(item.productId);
    return JSON.stringify([
        product.length > 0 ? `product:${product}` : `text:${name}`,
        name,
        typeof item.amount === 'number',
        clean(item.unit).toUpperCase(),
        clean(item.note),
        clean(item.category),
        clean(item.aisle),
    ]);
}

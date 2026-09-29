import type { ShoppingListItemDto } from '../models/shopping-list.data';

const MAX_AMOUNT = 1_000_000;

export function isTextQuantity(note: string | null | undefined): boolean {
    return /^(по вкусу|по желанию|to taste|optional)$/iu.test(note?.trim() ?? '');
}

/** Merge only new additions. Existing duplicate rows and purchased items are never consolidated. */
export function appendShoppingItems(
    existing: readonly ShoppingListItemDto[],
    incoming: readonly ShoppingListItemDto[],
): ShoppingListItemDto[] {
    const result = existing.map(item => ({ ...item }));
    for (const addition of incoming) {
        const key = mergeKey(addition);
        const match = key === null ? undefined : result.find(item => mergeKey(item) === key);
        if (match === undefined) {
            result.push({ ...addition, sortOrder: result.length + 1 });
        } else if (typeof match.amount === 'number' && typeof addition.amount === 'number') {
            const amount = match.amount + addition.amount;
            if (Number.isFinite(amount) && amount <= MAX_AMOUNT) {
                match.amount = amount;
            } else {
                result.push({ ...addition, sortOrder: result.length + 1 });
            }
        }
    }
    return result;
}

function mergeKey(item: ShoppingListItemDto): string | null {
    if (item.isChecked === true) {
        return null;
    }
    const identity = mergeIdentity(item);
    if (identity === null) {
        return null;
    }
    return JSON.stringify([
        identity,
        typeof item.amount === 'number' ? 'number' : 'text',
        clean(item.unit).toLowerCase(),
        clean(item.note),
        clean(item.category),
        clean(item.aisle),
    ]);
}

function mergeIdentity(item: ShoppingListItemDto): string | null {
    const product = clean(item.productId);
    if (typeof item.amount === 'number') {
        return product.length > 0 && Number.isFinite(item.amount) && item.amount > 0 ? `product:${product}` : null;
    }
    if (!isTextQuantity(item.note)) {
        return null;
    }
    const name = clean(item.name).toLowerCase();
    return product.length > 0 ? `product:${product}` : `text:${name}`;
}

function clean(value: string | null | undefined): string {
    return value?.trim() ?? '';
}

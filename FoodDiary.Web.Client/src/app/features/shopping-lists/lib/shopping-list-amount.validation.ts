const MAX_SHOPPING_AMOUNT = 1_000_000;

export function isShoppingListAmountValid(amount: number | null): boolean {
    return amount === null || (Number.isFinite(amount) && amount > 0 && amount <= MAX_SHOPPING_AMOUNT);
}

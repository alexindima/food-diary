import type { MeasurementUnit } from './product.data';
import type { UtcInstant } from './semantics/date-value';
import type { ProductId, ShoppingListId, ShoppingListItemId } from './semantics/entity-id';

export type ShoppingList = {
    id: ShoppingListId;
    name: string;
    createdAt: UtcInstant;
    items: ShoppingListItem[];
};

export type ShoppingListItem = {
    id: ShoppingListItemId;
    shoppingListId: ShoppingListId;
    productId?: ProductId | null;
    name: string;
    amount?: number | null;
    unit?: MeasurementUnit | string | null;
    category?: string | null;
    aisle?: string | null;
    note?: string | null;
    isChecked: boolean;
    checkedOnUtc?: UtcInstant | null;
    sortOrder: number;
    sources?: ShoppingListItemSource[];
};

export type ShoppingListItemSource = {
    id: string;
    sourceType: string;
    mealPlanId?: string | null;
    mealPlanMealId?: string | null;
    recipeId?: string | null;
    label: string;
    dayNumber?: number | null;
    mealType?: string | null;
    amount: number;
    unit?: MeasurementUnit | string | null;
};

export type ShoppingListSummary = {
    id: ShoppingListId;
    name: string;
    createdAt: UtcInstant;
    itemsCount: number;
    remainingCount?: number;
    completed?: boolean;
};

export type ShoppingListCreateDto = {
    name: string;
    items?: ShoppingListItemDto[];
};

export type ShoppingListUpdateDto = {
    name?: string | null;
    items?: ShoppingListItemDto[];
};

export type ShoppingListItemDto = {
    id?: string | null;
    productId?: string | null;
    name?: string | null;
    amount?: number | null;
    unit?: MeasurementUnit | string | null;
    category?: string | null;
    aisle?: string | null;
    note?: string | null;
    isChecked?: boolean;
    checkedOnUtc?: string | null;
    sortOrder?: number | null;
};

export type ShoppingListPage = { items: ShoppingListSummary[]; hasMore: boolean; nextPage: number | null };
export type ShoppingListOverview = { selectedList: ShoppingList | null; lists: ShoppingListPage };

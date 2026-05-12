import type { MeasurementUnit } from '../../products/models/product.data';

export type ShoppingList = {
    id: string;
    name: string;
    createdAt: string;
    items: ShoppingListItem[];
};

export type ShoppingListItem = {
    id: string;
    shoppingListId: string;
    productId?: string | null;
    name: string;
    amount?: number | null;
    unit?: MeasurementUnit | string | null;
    category?: string | null;
    isChecked: boolean;
    sortOrder: number;
};

export type ShoppingListSummary = {
    id: string;
    name: string;
    createdAt: string;
    itemsCount: number;
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
    productId?: string | null;
    name?: string | null;
    amount?: number | null;
    unit?: MeasurementUnit | string | null;
    category?: string | null;
    isChecked?: boolean;
    sortOrder?: number | null;
};

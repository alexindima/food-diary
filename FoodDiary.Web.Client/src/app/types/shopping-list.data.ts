import { MeasurementUnit } from './product.data';

export interface ShoppingList {
    id: string;
    name: string;
    createdAt: string;
    items: ShoppingListItem[];
}

export interface ShoppingListItem {
    id: string;
    shoppingListId: string;
    productId?: string | null;
    name: string;
    amount?: number | null;
    unit?: MeasurementUnit | string | null;
    category?: string | null;
    isChecked: boolean;
    sortOrder: number;
}

export interface ShoppingListCreateDto {
    name: string;
    items?: ShoppingListItemDto[];
}

export interface ShoppingListUpdateDto {
    name?: string | null;
    items?: ShoppingListItemDto[];
}

export interface ShoppingListItemDto {
    productId?: string | null;
    name?: string | null;
    amount?: number | null;
    unit?: MeasurementUnit | string | null;
    category?: string | null;
    isChecked?: boolean;
    sortOrder?: number | null;
}

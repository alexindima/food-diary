import type { MeasurementUnit } from '../../../shared/models/product.data';
import type { ShoppingListItem } from '../../../shared/models/shopping-list.data';

export type ShoppingListItemFormModel = {
    name: string;
    amount: number | null;
    unit: MeasurementUnit | null;
    category: string | null;
    note: string | null;
};

export type ShoppingListItemViewModel = {
    meta: string;
    quantity: string;
    detail: string;
} & ShoppingListItem;

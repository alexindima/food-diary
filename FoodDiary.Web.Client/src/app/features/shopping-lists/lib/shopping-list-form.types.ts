import type { MeasurementUnit } from '../../products/models/product.data';
import type { ShoppingListItem } from '../models/shopping-list.data';

export type ShoppingListItemFormModel = {
    name: string;
    amount: number | null;
    unit: MeasurementUnit | null;
    category: string | null;
};

export type ShoppingListItemViewModel = {
    meta: string;
} & ShoppingListItem;

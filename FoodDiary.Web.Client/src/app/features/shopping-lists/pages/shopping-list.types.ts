import type { FormGroupControls } from '../../../shared/lib/common.data';
import type { MeasurementUnit } from '../../products/models/product.data';
import type { ShoppingListItem } from '../models/shopping-list.data';

type ShoppingListItemFormValues = {
    name: string;
    amount: number | null;
    unit: MeasurementUnit | null;
    category: string | null;
};

export type ShoppingListItemViewModel = {
    meta: string;
} & ShoppingListItem;

export type ShoppingListItemFormGroup = FormGroupControls<ShoppingListItemFormValues>;

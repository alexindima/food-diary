import type { FormGroupControls } from '../../../shared/lib/common.data';
import type { MeasurementUnit } from '../../products/models/product.data';
import type { ShoppingListItem } from '../models/shopping-list.data';

interface ShoppingListItemFormValues {
    name: string;
    amount: number | null;
    unit: MeasurementUnit | null;
    category: string | null;
}

export interface ShoppingListItemViewModel extends ShoppingListItem {
    meta: string;
}

export type ShoppingListItemFormGroup = FormGroupControls<ShoppingListItemFormValues>;

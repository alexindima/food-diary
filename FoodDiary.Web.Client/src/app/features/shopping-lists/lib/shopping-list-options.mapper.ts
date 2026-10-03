import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';

import type { ShoppingListSummary } from '../../../shared/models/shopping-list.data';

export function buildShoppingListOptions(lists: readonly ShoppingListSummary[]): Array<FdUiSelectOption<string>> {
    return lists.map(list => ({
        value: list.id,
        label: `${list.name} (${list.itemsCount})`,
    }));
}

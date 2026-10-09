import { utcInstant } from '../../models/semantics/date-value';
import { entityId } from '../../models/semantics/entity-id';
import type { ShoppingList, ShoppingListOverview, ShoppingListSummary } from '../../models/shopping-list.data';
import type { ShoppingListHttpResponse } from './generated/model/shopping-list-http-response';
import type { ShoppingListOverviewHttpResponse } from './generated/model/shopping-list-overview-http-response';
import type { ShoppingListSummaryHttpResponse } from './generated/model/shopping-list-summary-http-response';
import { requireSdkFields, sdkOptional } from './sdk-response';

export function shoppingSummaryFromSdk(value: ShoppingListSummaryHttpResponse): ShoppingListSummary {
    const item = requireSdkFields(value, ['id', 'name', 'createdAt', 'itemsCount']);
    return { ...item, id: entityId<'shopping-list'>(item.id), createdAt: utcInstant(item.createdAt) };
}

export function shoppingListFromSdk(response: ShoppingListHttpResponse): ShoppingList {
    const value = requireSdkFields(response, ['id', 'name', 'createdAt', 'items']);
    return {
        ...value,
        items: value.items.map(itemResponse => {
            const item = requireSdkFields(itemResponse, ['id', 'shoppingListId', 'name', 'isChecked', 'sortOrder']);
            return {
                ...item,
                id: entityId<'shopping-list-item'>(item.id),
                shoppingListId: entityId<'shopping-list'>(item.shoppingListId),
                productId: item.productId === null || item.productId === undefined ? item.productId : entityId<'product'>(item.productId),
                checkedOnUtc:
                    item.checkedOnUtc === null || item.checkedOnUtc === undefined ? item.checkedOnUtc : utcInstant(item.checkedOnUtc),
                sources: item.sources?.map(source => requireSdkFields(source, ['id', 'sourceType', 'label', 'amount'])),
            };
        }),

        id: entityId<'shopping-list'>(value.id),
        createdAt: utcInstant(value.createdAt),
    };
}

export function shoppingOverviewFromSdk(response: ShoppingListOverviewHttpResponse): ShoppingListOverview {
    const value = requireSdkFields(response, ['lists']);
    const lists = requireSdkFields(value.lists, ['items', 'hasMore']);
    return {
        selectedList: sdkOptional(value.selectedList, shoppingListFromSdk),
        lists: { ...lists, items: lists.items.map(shoppingSummaryFromSdk), nextPage: lists.nextPage ?? null },
    };
}

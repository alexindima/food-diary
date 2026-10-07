import { userApiContract } from './api-sdk-contract.mjs';

/** A bot consumes explicitly frozen operations; unrelated server routes stay outside its client. */
export function botApiContract(source, operations) {
    const paths = {};
    const groups = [];
    for (const entry of operations) {
        const method = entry.method.toLowerCase();
        const item = source.paths?.[entry.path];
        if (!item?.[method]) throw new Error(`Bot API operation disappeared: ${entry.method} ${entry.path}`);
        paths[entry.path] = { ...paths[entry.path], [method]: item[method] };
        if (item.parameters) paths[entry.path].parameters = item.parameters;
        groups.push({ name: entry.name, prefix: entry.path, operations: { [`${entry.method} ${entry.path}`]: entry.operationId } });
    }
    const contract = userApiContract({ ...source, paths }, groups, 'FoodDiary Telegram API');
    // The bot already parses these totals as decimal. Preserve that representation
    // without changing the server's number schema or narrowing statistics doubles.
    for (const key of ['calories', 'protein', 'fat', 'carbs']) {
        const property = contract.components.schemas.FoodNutritionHttpResponse?.properties?.[key];
        if (property?.type !== 'number') throw new Error(`Bot nutrition number contract changed: ${key}`);
        property['x-bot-decimal'] = true;
    }
    return contract;
}

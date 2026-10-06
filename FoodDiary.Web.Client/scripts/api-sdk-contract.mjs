const productsPath = '/api/v{version}/products';
const operationNames = {
    [`GET ${productsPath}`]: 'queryProducts',
    [`POST ${productsPath}`]: 'createProduct',
    [`GET ${productsPath}/overview`]: 'getProductsOverview',
    [`GET ${productsPath}/recent`]: 'getRecentProducts',
    [`GET ${productsPath}/suggestions`]: 'getProductSuggestions',
    [`GET ${productsPath}/public/{id}`]: 'getPublicProductById',
    [`GET ${productsPath}/{id}`]: 'getProductById',
    [`PATCH ${productsPath}/{id}`]: 'updateProduct',
    [`DELETE ${productsPath}/{id}`]: 'deleteProduct',
    [`POST ${productsPath}/{id}/duplicate`]: 'duplicateProduct',
};
const httpMethods = new Set(['get', 'post', 'put', 'patch', 'delete', 'head', 'options']);

export function canonicalJson(value) {
    const sort = item => {
        if (Array.isArray(item)) return item.map(sort);
        if (item !== null && typeof item === 'object') {
            return Object.fromEntries(
                Object.keys(item)
                    .sort()
                    .map(key => [key, sort(item[key])]),
            );
        }
        return item;
    };
    return `${JSON.stringify(sort(value), null, 4)}\n`;
}

// Keep the server's wire names, validation, nullability and auth metadata intact.
// Only assign stable client method names and select the pilot's schema closure.
export function productsContract(source) {
    if (!source.openapi?.startsWith('3.0.') || !source.paths || !source.components?.schemas) {
        throw new Error('Expected a complete OpenAPI 3.0 document from the FoodDiary host.');
    }
    const paths = {};
    const foundOperations = new Set();
    for (const [path, sourceItem] of Object.entries(source.paths)) {
        if (path !== productsPath && !path.startsWith(`${productsPath}/`)) continue;
        const item = structuredClone(sourceItem);
        for (const [method, operation] of Object.entries(item)) {
            if (!httpMethods.has(method)) continue;
            const key = `${method.toUpperCase()} ${path}`;
            const operationId = operationNames[key];
            if (!operationId) throw new Error(`Name the new Products SDK operation: ${key}`);
            foundOperations.add(key);
            operation.operationId = operationId;
            operation.tags = ['Products'];
        }
        paths[path] = item;
    }
    for (const key of Object.keys(operationNames)) {
        if (!foundOperations.has(key)) throw new Error(`Products SDK operation disappeared: ${key}`);
    }

    const schemas = {};
    const visit = value => {
        if (value === null || typeof value !== 'object') return;
        if (typeof value.$ref === 'string') {
            const prefix = '#/components/schemas/';
            if (!value.$ref.startsWith(prefix)) throw new Error(`Unsupported SDK reference: ${value.$ref}`);
            const name = value.$ref.slice(prefix.length);
            if (!Object.hasOwn(schemas, name)) {
                const schema = source.components.schemas[name];
                if (!schema) throw new Error(`Missing SDK schema: ${name}`);
                schemas[name] = structuredClone(schema);
                visit(schema);
            }
        }
        for (const child of Object.values(value)) visit(child);
    };
    visit(paths);
    return {
        openapi: source.openapi,
        info: { title: 'FoodDiary Products API', version: '1.0.0' },
        paths,
        components: { schemas, securitySchemes: source.components.securitySchemes ?? {} },
    };
}

import { describe, expect, it } from 'vitest';

import { addOptionalNumberParam, addOptionalStringParam, type ApiQueryParams } from './api-query-params.utils';

const LIMIT = 10;

describe('api query params utils', () => {
    it('adds non-empty string values', () => {
        const params: ApiQueryParams = {};

        addOptionalStringParam(params, 'search', 'apple');

        expect(params).toEqual({ search: 'apple' });
    });

    it('skips empty and undefined string values', () => {
        const params: ApiQueryParams = {};

        addOptionalStringParam(params, 'search', '');
        addOptionalStringParam(params, 'category', void 0);

        expect(params).toEqual({});
    });

    it('adds numeric values and skips undefined', () => {
        const params: ApiQueryParams = {};

        addOptionalNumberParam(params, 'limit', LIMIT);
        addOptionalNumberParam(params, 'offset', void 0);

        expect(params).toEqual({ limit: LIMIT });
    });
});

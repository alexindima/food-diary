import { describe, expect, it } from 'vitest';

import { API_MAX_PAGE_NUMBER, hasInvalidPaginationPage, readPaginationPage, resolvePaginationPage } from './pagination-query.utils';

const THIRD_PAGE = 3;
const FOURTH_PAGE = 4;

describe('URL pagination bounds', () => {
    it.each([null, '', '0', '-1', '1.5', 'NaN', 'Infinity', '999999', '9007199254740992'])(
        'loads the first page for an unsupported bookmark %s',
        value => {
            expect(readPaginationPage(value)).toBe(1);
        },
    );
    it('keeps valid pages within the published API range', () => {
        expect(readPaginationPage('2')).toBe(2);
        expect(readPaginationPage(String(API_MAX_PAGE_NUMBER))).toBe(API_MAX_PAGE_NUMBER);
        expect(hasInvalidPaginationPage('999999')).toBe(true);
        expect(hasInvalidPaginationPage(null)).toBe(false);
        expect(hasInvalidPaginationPage('2')).toBe(false);
    });
    it('recovers a page removed from a result set and represents a true empty result on page one', () => {
        expect(resolvePaginationPage(THIRD_PAGE, 2)).toBe(2);
        expect(resolvePaginationPage(2, 1)).toBe(1);
        expect(resolvePaginationPage(FOURTH_PAGE, 0)).toBe(1);
        expect(resolvePaginationPage(2, THIRD_PAGE)).toBe(2);
    });
});

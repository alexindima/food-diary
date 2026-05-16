import { describe, expect, it } from 'vitest';

import { EXPLORE_PAGE_SIZE } from './explore-page.constants';

const EXPECTED_EXPLORE_PAGE_SIZE = 20;

describe('explore page constants', () => {
    it('keeps the default page size stable', () => {
        expect(EXPLORE_PAGE_SIZE).toBe(EXPECTED_EXPLORE_PAGE_SIZE);
    });
});
